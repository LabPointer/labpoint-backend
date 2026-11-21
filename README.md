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

## appsettings

Backend e Email seguem o mesmo padrão (os arquivos reais são ignorados pelo git; copie dos `.example`):

- `appsettings.json`: base, vale em todos os ambientes (feriados, logs, CORS e URLs de produção, escolha do banco).
- `appsettings.Development.json`: só o que muda no desenvolvimento (banco local, `http://localhost:3000`, SMTP de
  teste no Email). O Backend roda em Development pelo perfil `http`; o Email, pelo `Email/Properties/launchSettings.json`.
- A escolha do banco é `Database:Provider` (`PostgreSql` ou `Sqlite`) e deve ser a mesma no Backend e no Email.
- O SMTP só existe no Email (o Backend não envia e-mails). Senhas ficam nos arquivos reais ou em variáveis de ambiente,
  nunca nos `.example`.

## Banco de dados: PostgreSQL ou SQLite

O provedor é escolhido no `appsettings.json` (do Backend e do Email):

```json
"Database": { "Provider": "PostgreSql" },          // ou "Sqlite"
"ConnectionStrings": {
  "DbConnection": "Host=localhost;Port=5432;Database=pei;Username=pei;Password=pei",
  "SqliteConnection": "Data Source=labpoint.db"
}
```

- Cada provedor tem as próprias migrations: PostgreSQL em `Backend/Migrations` (contexto `AppDbContext`) e SQLite em
  `Backend/Migrations/Sqlite` (contexto `SqliteAppDbContext`). Ao mudar o modelo, gere as duas:
  - `dotnet ef migrations add Nome --context AppDbContext`
  - `dotnet ef migrations add Nome --context SqliteAppDbContext --output-dir Migrations/Sqlite`
- Para criar/atualizar o banco SQLite: `dotnet ef database update --context SqliteAppDbContext`.
- No SQLite não há busca full-text (a busca usa só "contém") nem LISTEN/NOTIFY (o serviço de e-mail verifica a fila a
  cada `OutboxPollSeconds`), e `lower()` não converte letras acentuadas.

## Projeto `Core`

Class library com o que é comum ao Backend e ao serviço de e-mail: `AppDbContext`/`SqliteAppDbContext` e a escolha do
provedor (`AddLabpointDatabase`), `BrazilClock`, `UsageTokens` (links de check-in/check-out), `TextSearch` e a fila de
e-mails (`Core.Email.EmailOutbox`).

## Serviço de e-mail (`Email/`)

O backend não envia e-mails: ele grava cada pedido na tabela `email_outbox` e avisa com `NOTIFY email_outbox`.
O projeto `Email` (um .NET Worker separado) escuta essa fila, monta o HTML a partir de `Email/Templates`, envia por
SMTP (com novas tentativas) e agenda os avisos de check-in e check-out.

- Local: `cd Email && dotnet run` (usa a seção `Smtp` do `appsettings` do próprio projeto ou variáveis de ambiente;
  com `Smtp:Host` vazio, em Development o e-mail só é escrito no log).
- Docker: `docker compose up -d --build email` (credenciais via `SMTP_HOST`, `SMTP_USER`, `SMTP_PASSWORD`,
  `SMTP_FROM_ADDRESS` no ambiente ou num `.env` não versionado).
- Cada aviso de check-in/check-out fica ligado à sessão (`email_outbox.fk_usage_id`) e vence no fim da janela
  (`expires_at`): se o serviço ficar fora do ar até lá, o aviso não é enviado e fica com status 3 (Expirado).
  O Gerenciar check-out mostra a situação dos avisos de cada sessão.
- Configuração em `EmailService`: `OutboxPollSeconds`, `BatchSize`, `MaxAttempts`, `UsageScanSeconds`,
  `CheckInEmailMinutesBeforeWindow` (15) e `CheckOutEmailMinutesBeforeWindow` (0).
