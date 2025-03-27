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
using Identity.API.Services;
using System.Web;

namespace Identity.API.Controllers
{
    [Route("api/auth")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly ApplicationDbContext _context;
        private readonly IConfiguration _configuration;
        private readonly IEmailService _emailService;

        public AuthController(
             UserManager<ApplicationUser> userManager,
            RoleManager<IdentityRole> roleManager,
            ApplicationDbContext context,
            IConfiguration configuration,
            IEmailService emailService)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _context = context;
            _configuration = configuration;
            _emailService = emailService;
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

        private async Task<ApplicationUser> GetCurrentUserAsync()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
                return null;
            return await _userManager.FindByIdAsync(userId);
        }

        [HttpGet("profile")]
        [Authorize]
        public async Task<IActionResult> GetProfile()
        {
            var user = await GetCurrentUserAsync();
            if (user == null)
                return Unauthorized();
            var userProfile = new UserProfileDto
            {
                Id = user.Id,
                Email = user.Email,
                UserName = user.UserName,
                FirstName = user.FirstName,
                LastName = user.LastName,
                PhoneNumber = user.PhoneNumber,
                PhoneNumberConfirmed = user.PhoneNumberConfirmed,
                EmailConfirmed = user.EmailConfirmed,
                AvatarUrl = user.AvatarUrl,
                TenantId = user.TenantId.ToString()
            };
            return Ok(userProfile);
        }

        [HttpPut("profile")]
        [Authorize]
        public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileDto model)
        {
            var user = await GetCurrentUserAsync();
            if (user == null)
                return Unauthorized();
            user.FirstName = model.FirstName ?? user.FirstName;
            user.LastName = model.LastName ?? user.LastName;
            user.PhoneNumber = model.PhoneNumber ?? user.PhoneNumber;
            var result = await _userManager.UpdateAsync(user);
            if (!result.Succeeded)
                return BadRequest(result.Errors);
            return Ok(new { message = "Profile updated successfully" });
        }

        [HttpPut("change-password")]
        [Authorize]
        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordDto model)
        {
            var user = await GetCurrentUserAsync();
            if (user == null)
                return Unauthorized();
            if (model.NewPassword != model.ConfirmNewPassword)
                return BadRequest("New password and confirmation password do not match");
            var result = await _userManager.ChangePasswordAsync(user, model.CurrentPassword, model.NewPassword);
            if (!result.Succeeded)
                return BadRequest(result.Errors);
            await _userManager.UpdateSecurityStampAsync(user);
            return Ok(new { message = "Password changed successfully" });
        }

        [HttpPost("reset-password")]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordDto model)
        {
            if (model.NewPassword != model.ConfirmNewPassword)
                return BadRequest("New password and confirmation password do not match");
            var user = await _userManager.FindByEmailAsync(model.Email);
            if (user == null)
                return Ok(new { message = "Password has been reset. You can now login with your new password." });
            var result = await _userManager.ResetPasswordAsync(user, model.Token, model.NewPassword);
            if (!result.Succeeded)
                return BadRequest(result.Errors);
            return Ok(new { message = "Password has been reset. You can now login with your new password." });
        }

        [HttpPost("forgot-password")]
        public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordDto model)
        {
            var user = await _userManager.FindByEmailAsync(model.Email);
            if (user == null || !(await _userManager.IsEmailConfirmedAsync(user)))
                return Ok(new { message = "If your email is registered, you will receive a password reset link." });
            var token = await _userManager.GeneratePasswordResetTokenAsync(user);
            var encodedToken = HttpUtility.UrlEncode(token);
            var callbackUrl = $"{_configuration["ApplicationSettings:ClientUrl"]}/reset-password?email={model.Email}&token={encodedToken}";
            var emailSubject = "Reset Your Password";
            var emailBody = $@"
                <p>Hello {user.UserName},</p>
                <p>Please reset your password by clicking <a href='{callbackUrl}'>here</a>.</p>
                <p>If you didn't request this, please ignore this email.</p>
            ";
            await _emailService.SendEmailAsync(model.Email, emailSubject, emailBody);
            return Ok(new { message = "If your email is registered, you will receive a password reset link." });
        }

        [HttpGet("confirm-email")]
        public async Task<IActionResult> ConfirmEmail([FromQuery] string userId, [FromQuery] string token)
        {
            if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(token))
                return BadRequest("User ID and token are required");
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
                return NotFound("User not found");
            var decodedToken = HttpUtility.UrlDecode(token);
            var result = await _userManager.ConfirmEmailAsync(user, decodedToken);
            if (!result.Succeeded)
                return BadRequest("Failed to confirm email");
            return Redirect($"{_configuration["ApplicationSettings:ClientUrl"]}/email-confirmed");
        }

        [HttpPost("profile/avatar")]
        [Authorize]
        public async Task<IActionResult> UploadAvatar(IFormFile file)
        {
            var user = await GetCurrentUserAsync();
            if (user == null)
                return Unauthorized();
            if (file == null || file.Length == 0)
                return BadRequest("No file uploaded");
            if (file.Length > 2 * 1024 * 1024)
                return BadRequest("File size should not exceed 2MB");
            var allowedTypes = new[] { "image/jpeg", "image/png", "image/gif" };
            if (!allowedTypes.Contains(file.ContentType))
                return BadRequest("Only JPEG, PNG and GIF images are allowed");
            var fileName = $"{Guid.NewGuid()}_{Path.GetFileName(file.FileName)}";
            var filePath = Path.Combine("wwwroot/avatars", fileName);
            Directory.CreateDirectory(Path.GetDirectoryName(filePath));
            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }
            var avatarUrl = $"/avatars/{fileName}";
            user.AvatarUrl = avatarUrl;
            var result = await _userManager.UpdateAsync(user);
            if (!result.Succeeded)
                return BadRequest(result.Errors);
            return Ok(new { avatarUrl });
        }

        [HttpGet("get-user-id")]
        public async Task<IActionResult> GetUserIdFromUsername(string username)
        {
            var user = await _userManager.FindByNameAsync(username);
            if (user == null)
                return NotFound();

            return Ok(new { UserId = user.Id });
        }

        [HttpGet("validate-user/{userId}")]
        [Authorize]
        public async Task<IActionResult> ValidateUser(string userId)
        {
            // JWT token'dan gelen kullanıcının ID'si
            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            // Sadece kendi bilgilerini kontrol etmesine izin ver
            if (currentUserId != userId)
                return Unauthorized();

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null || !user.IsActive)
                return NotFound();

            return Ok();
        }

    }
}
