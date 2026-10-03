# Labpoint API

Backend para do Labpoint, sistema de reservas de laboratórios.

> [!WARNING]
> Versão recomendada do [.NET](https://dotnet.microsoft.com/pt-br/download) é 10 LTS ou superior

> [!WARNING]
> Para o banco de dados, recomendo baixar o [Docker](https://www.docker.com/) ou [Postgres](https://www.postgresql.org/)
>
> Caso esteja usando o postgre nativo, nao se esqueça de configurar as credencias de acesso em `./src/main/resources/application.json`

# Iniciando

```bash
dotnet restore
dotnet run
```

## Scripts

- `dotnet restore` - Limpa o projeto e instala as dependências
- `dotnet run` - Inicia o servidor de desenvolvimento
- `dotnet build` - Cria o arquivo dll do projeto
- `dotnet test` - Executa os testes do projeto
- `dotnet run --environment=Production` - Inicia o servidor de desenvolvimento com o perfil para prod
- `dotnet build --environment=Production` - Cria o arquivo dll do projeto com o perfil para prod
- `dotnet run --environment=Development` - Inicia o servidor de desenvolvimento com o perfil para dev
- `dotnet build --environment=Development` - Cria o arquivo dll do projeto com o perfil para dev

Abra http://localhost:8080/scalar no seu navegador para ver a documentação das rotas. 

Abra http://localhost:8080/openapi/v1.json no seu navegador para ver a documentação das rotas em formato JSON. 

Abra http://localhost:8080/swagger no seu navegador para ver a documentação das rotas em formato Swagger UI.

## Migrations

- `dotnet ef migrations add [nome da migration]` - Cria uma nova migration
- `dotnet ef database update` - Atualiza o banco de dados com as migrations pendentes
- `dotnet ef migrations remove` - Remove a última migration criada
- `dotnet ef migrations list` - Lista todas as migrations do projeto
- `dotnet ef migrations script` - Gera um script SQL com todas as migrations pendentes
- `dotnet ef migrations script [nome da migration]` - Gera um script SQL com a migration especificada
- `dotnet ef database drop --force` - Deleta o banco de dados

## Packages

- [.NET](https://dotnet.microsoft.com/pt-br/download)

## appsettings.json

```json
{
  "ConnectionStrings": {
    "DbConnection": "Host=localhost;Port=5432;Database=pei;Username=pei;Password=pei"
  },
  "Frontend": {
    "Url": "http://localhost/3000"
  },
  "Smtp": {
    "Host": "smtp.gmail.com",
    "Port": 587,
    "User": "seu email",
    "Password": "senha do aplicativo do email",
    "FromName": "Labpoint",
    "FromAddress": "seu email"
  },
  "Cors": {
    "AllowedOrigins": [
      "http://localhost:3000"
    ]
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "AllowedHosts": "*"
}

```
