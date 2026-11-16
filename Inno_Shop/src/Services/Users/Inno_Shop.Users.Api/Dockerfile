FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
USER $APP_UID
ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

COPY ["src/Services/Users/Inno_Shop.Users.Api/Inno_Shop.Users.Api.csproj",                       "src/Services/Users/Inno_Shop.Users.Api/"]
COPY ["src/Services/Users/Inno_Shop.Users.Application/Inno_Shop.Users.Application.csproj",       "src/Services/Users/Inno_Shop.Users.Application/"]
COPY ["src/Services/Users/Inno_Shop.Users.Domain/Inno_Shop.Users.Domain.csproj",                 "src/Services/Users/Inno_Shop.Users.Domain/"]
COPY ["src/Services/Users/Inno_Shop.Users.Infrastructure/Inno_Shop.Users.Infrastructure.csproj", "src/Services/Users/Inno_Shop.Users.Infrastructure/"]
COPY ["src/Services/Contracts/Inno_Shop.Contracts/Inno_Shop.Contracts.csproj", "src/Services/Contracts/Inno_Shop.Contracts/"]
RUN dotnet restore "src/Services/Users/Inno_Shop.Users.Api/Inno_Shop.Users.Api.csproj"
COPY src/Services/Users/ src/Services/Users/
COPY src/Services/Contracts/ src/Services/Contracts/

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
WORKDIR /src/src/Services/Users/Inno_Shop.Users.Api
# UseAppHost=false - не создавать лишний нативный запускной файл, нужен только .dll
RUN dotnet publish "Inno_Shop.Users.Api.csproj" -c $BUILD_CONFIGURATION -o /app/publish --no-restore /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Inno_Shop.Users.Api.dll"]