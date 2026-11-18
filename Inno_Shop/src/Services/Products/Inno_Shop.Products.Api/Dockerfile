FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
USER $APP_UID
ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

COPY ["src/Services/Products/Inno_Shop.Products.Api/Inno_Shop.Products.Api.csproj",                   "src/Services/Products/Inno_Shop.Products.Api/"]
COPY ["src/Services/Products/Inno_Shop.Products.Application/Inno_Shop.Products.Application.csproj",   "src/Services/Products/Inno_Shop.Products.Application/"]
COPY ["src/Services/Products/Inno_Shop.Products.Domain/Inno_Shop.Products.Domain.csproj",             "src/Services/Products/Inno_Shop.Products.Domain/"]
COPY ["src/Services/Products/Inno_Shop.Products.Infrastructure/Inno_Shop.Products.Infrastructure.csproj", "src/Services/Products/Inno_Shop.Products.Infrastructure/"]
COPY ["src/Services/Contracts/Inno_Shop.Contracts/Inno_Shop.Contracts.csproj", "src/Services/Contracts/Inno_Shop.Contracts/"]
RUN dotnet restore "src/Services/Products/Inno_Shop.Products.Api/Inno_Shop.Products.Api.csproj"
COPY src/Services/Products/ src/Services/Products/
COPY src/Services/Contracts/ src/Services/Contracts/

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
WORKDIR /src/src/Services/Products/Inno_Shop.Products.Api
# UseAppHost=false - не создавать лишний нативный запускной файл, нужен только .dll
RUN dotnet publish "Inno_Shop.Products.Api.csproj" -c $BUILD_CONFIGURATION -o /app/publish --no-restore /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Inno_Shop.Products.Api.dll"]
