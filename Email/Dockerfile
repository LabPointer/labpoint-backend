# Build: contexto = raiz do repositório (onde ficam Email/, Core/, Models/ e Config/).
#   docker build -f Email/Dockerfile -t labpoint-email .

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

# Só os .csproj primeiro, para o restore ficar em cache enquanto as dependências não mudarem.
COPY Email/Email.csproj Email/
COPY Config/Config.csproj Config/
COPY Core/Core.csproj Core/
COPY Models/Models.csproj Models/
RUN dotnet restore Email/Email.csproj

COPY Email/ Email/
COPY Config/ Config/
COPY Core/ Core/
COPY Models/ Models/
RUN dotnet publish Email/Email.csproj \
    -c $BUILD_CONFIGURATION \
    -o /app/publish \
    --no-restore \
    /p:UseAppHost=false

# aspnet (e não runtime): o Core usa o Identity, que faz parte do ASP.NET Core.
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app

ENV DOTNET_ENVIRONMENT=Production

COPY --from=build /app/publish .

USER $APP_UID

ENTRYPOINT ["dotnet", "Email.dll"]
