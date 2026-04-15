# Bangboo API

Bangboo é uma API é o backend do bot do discord.

> [!WARNING]
> Versão recomendada do [Nodejs](https://nodejs.org/pt-br) é 24 LTS ou superior

> [!WARNING]
> Versão recomendada do [Docker](https://www.docker.com/) é 4 ou superior

# Iniciando

```bash
npm install
npm run dev
```

## Scripts

- `npm run auth:generate` - Gera o modelo para o uso do better auth
- `npm run db:migrate` - Migra o modelo gerado pelo drizzle para o postgres
- `npm run db:upsert` - Insere dados basicos no db
- `npm run dev` - Inicia o servidor de desenvolvimento
- `npm run watch` - Inicia o servidor de desenvolvimento com autoreload
- `npm run start` - Inicia o servidor de produção
- `npm run build` - Compila o projeto
- `npm run check` - Verifica se o projeto está correto

Abra http://localhost:3000/ no seu navegador para ver o resultado.

Abra http://localhost:3000/docs no seu navegador para ver a documentação das rotas.

## Packages

- [Fastify](https://fastify.dev/)
- [Better-auth](https://better-auth.com/)

## Dotenv setup

- BETTER_AUTH_URL: "http://localhost:3000" ou a url de produção
- BETTER_AUTH_SECRET: Use o [site da documentação](https://better-auth.com/docs/installation#set-environment-variables) do better auth para gerar uma chave
- DATABASE_URL: postgresql://labpoint:labpoint@localhost:5432/labpoint
