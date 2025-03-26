FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY ["src/Authorization/Authorization.API/Authorization.API.csproj", "src/Authorization/Authorization.API/"]
COPY ["src/Shared/Shared.Core/Shared.Core.csproj", "src/Shared/Shared.Core/"]
RUN dotnet restore "src/Authorization/Authorization.API/Authorization.API.csproj"
COPY . .
WORKDIR "/src/src/Authorization/Authorization.API"
RUN dotnet build "Authorization.API.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Authorization.API.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Authorization.API.dll"]
