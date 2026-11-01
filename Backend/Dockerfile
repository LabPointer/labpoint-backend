# Build: contexto = raiz do repositório (onde ficam Backend/, Config/, DTOs/ e Models/).
#   docker build -f Backend/Dockerfile -t labpoint-backend .

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

# Copia só os .csproj primeiro para o restore ficar em cache enquanto as dependências não mudarem.
# O Backend referencia Config, DTOs e Models, então os quatro projetos precisam estar presentes.
COPY Backend/Backend.csproj Backend/
COPY Config/Config.csproj Config/
COPY DTOs/DTOs.csproj DTOs/
COPY Models/Models.csproj Models/
RUN dotnet restore Backend/Backend.csproj

COPY . .
# O Backend.csproj referencia ..\.dockerignore, que o Docker não copia para dentro da imagem de build.
RUN touch /src/.dockerignore
RUN dotnet publish Backend/Backend.csproj \
    -c $BUILD_CONFIGURATION \
    -o /app/publish \
    --no-restore \
    /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app

ENV ASPNETCORE_ENVIRONMENT=Production

# O Render injeta a variável PORT (padrão 10000) e só encaminha tráfego se o app escutar nessa porta em 0.0.0.0.
# O valor abaixo vale apenas para rodar a imagem fora do Render.
ENV PORT=10000
EXPOSE 10000

COPY --from=build /app/publish .

USER $APP_UID

# Usa shell para expandir ${PORT} em tempo de execução; "exec" mantém o dotnet como PID 1 (recebe o SIGTERM).
ENTRYPOINT ["sh", "-c", "ASPNETCORE_URLS=http://0.0.0.0:${PORT} exec dotnet Backend.dll"]
