using Identity.API.Models.DTOs;
using Identity.API.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Identity.API.Models.Requests;
using Identity.API.Data;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using Microsoft.AspNetCore.Authorization;

namespace Identity.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly ApplicationDbContext _context;
        private readonly IConfiguration _configuration;

        public AuthController(
             UserManager<ApplicationUser> userManager,
            RoleManager<IdentityRole> roleManager,
            ApplicationDbContext context,
            IConfiguration configuration)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _context = context;
            _configuration = configuration;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterModel model)
        {
            try
            {
                var tenantExists = await _context.Tenants.AnyAsync(t => t.Id == model.TenantId);
                if (!tenantExists) return BadRequest("Invalid Tenant");

                var user = new ApplicationUser
                {
                    UserName = model.Email,
                    Email = model.Email,
                    TenantId = model.TenantId,
                    FirstName = model.FirstName,
                    LastName = model.LastName,
                    IsActive = true
                };

                var result = await _userManager.CreateAsync(user, model.Password);

                if (!result.Succeeded)
                    return BadRequest(result.Errors);

                await _userManager.AddToRoleAsync(user, "User");
                return Ok(new { Message = "User created!" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal error: {ex.Message}");
            }
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginModel model)
        {
            try
            {
                var user = await _userManager.FindByEmailAsync(model.Username);
                if (user == null || !await _userManager.CheckPasswordAsync(user, model.Password))
                    return Unauthorized();

                if (!user.IsActive)
                    return Unauthorized(new { Message = "This account is disabled." });

                var roles = await _userManager.GetRolesAsync(user);
                var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                    new Claim(ClaimTypes.Name, user.UserName),
                    new Claim(ClaimTypes.GivenName, user.FirstName),
                    new Claim(ClaimTypes.Surname, user.LastName),
                    new Claim("TenantId", user.TenantId.ToString())
                };
                foreach (var role in roles) claims.Add(new Claim(ClaimTypes.Role, role));

                var token = GenerateJwtToken(claims);
                var refreshToken = GenerateRefreshToken();
                await SaveRefreshToken(user.Id, refreshToken);

                return Ok(new
                {
                    token = new JwtSecurityTokenHandler().WriteToken(token),
                    expiration = token.ValidTo,
                    refreshToken = refreshToken.Token
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal error: {ex.Message}");
            }
        }

        [Authorize]
        [HttpGet("user-info")]
        public async Task<IActionResult> GetUserInfo()
        {
            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                var user = await _userManager.FindByIdAsync(userId);
                if (user == null) return NotFound();

                var roles = await _userManager.GetRolesAsync(user);
                return Ok(new
                {
                    user.Email,
                    user.TenantId,
                    user.FirstName,
                    user.LastName,
                    Roles = roles
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal error: {ex.Message}");
            }
        }

        [HttpPost("refresh-token")]
        public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenModel model)
        {
            try
            {
                var principal = GetPrincipalFromExpiredToken(model.AccessToken);
                var userId = principal.FindFirstValue(ClaimTypes.NameIdentifier);
                var user = await _userManager.FindByIdAsync(userId);
                var storedToken = await _context.RefreshTokens.FirstOrDefaultAsync(rt => rt.Token == model.RefreshToken && rt.UserId == userId);

                if (user == null || storedToken == null || storedToken.Expires < DateTime.UtcNow)
                    return BadRequest("Invalid token");

                _context.RefreshTokens.Remove(storedToken);
                var newRefreshToken = GenerateRefreshToken();
                await SaveRefreshToken(user.Id, newRefreshToken);

                var newJwtToken = GenerateJwtToken(principal.Claims.ToList());
                return Ok(new
                {
                    token = new JwtSecurityTokenHandler().WriteToken(newJwtToken),
                    expiration = newJwtToken.ValidTo,
                    refreshToken = newRefreshToken.Token
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal error: {ex.Message}");
            }
        }

        private ClaimsPrincipal GetPrincipalFromExpiredToken(string token)
        {
            var tokenValidationParameters = new TokenValidationParameters
            {
                ValidateAudience = false,
                ValidateIssuer = false,
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Jwt:Secret"])),
                ValidateLifetime = false // Süreyi ignore et
            };

            var tokenHandler = new JwtSecurityTokenHandler();
            var principal = tokenHandler.ValidateToken(token, tokenValidationParameters, out var securityToken);

            // Token algoritmasını kontrol et
            if (securityToken is not JwtSecurityToken jwtSecurityToken ||
                !jwtSecurityToken.Header.Alg.Equals(SecurityAlgorithms.HmacSha256, StringComparison.InvariantCultureIgnoreCase))
                throw new SecurityTokenException("Invalid token");

            return principal;
        }

        private JwtSecurityToken GenerateJwtToken(List<Claim> authClaims)
        {
            var authSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["JWT:Secret"]));
            _ = int.TryParse(_configuration["JWT:TokenValidityInMinutes"], out int tokenValidityInMinutes);

            var token = new JwtSecurityToken(
                issuer: _configuration["JWT:ValidIssuer"],
                audience: _configuration["JWT:ValidAudience"],
                expires: DateTime.Now.AddMinutes(tokenValidityInMinutes),
                claims: authClaims,
                signingCredentials: new SigningCredentials(authSigningKey, SecurityAlgorithms.HmacSha256)
                );

            return token;
        }

        private RefreshToken GenerateRefreshToken()
        {
            return new RefreshToken
            {
                Token = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64)),
                Expires = DateTime.UtcNow.AddDays(7)
            };
        }

        private async Task SaveRefreshToken(string userId, RefreshToken refreshToken)
        {
            refreshToken.UserId = userId;
            await _context.RefreshTokens.AddAsync(refreshToken);
            await _context.SaveChangesAsync();
        }

        [Authorize]
        [HttpPost("logout")]
        public async Task<IActionResult> Logout([FromBody] RefreshTokenModel model)
        {
            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

                // Remove the refresh token from database
                var refreshToken = await _context.RefreshTokens
                    .FirstOrDefaultAsync(rt => rt.Token == model.RefreshToken && rt.UserId == userId);

                if (refreshToken != null)
                {
                    _context.RefreshTokens.Remove(refreshToken);
                    await _context.SaveChangesAsync();
                }

                return Ok(new { Message = "Logged out successfully" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal error: {ex.Message}");
            }
        }

    }
}
