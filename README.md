# Labpoint Backend

Labpoint é um sistema de agendamento de laboratórios, criado em primeiro momento para a instituição do SENAI com o intuito de organizar melhor a disposição das aulas na unidade e ajudar os professores a organizar e planejar os dias de acordo com a necessidade da disciplina. O backend concentra recursos para o desenvolvimento funcional do sistema.

> [!AVISO]
> Versão recomendada do [Bun](https://bun.sh/) é 1.3.x ou superior

# Iniciando

### Bun

```bash
bun install
bun run dev
```

## Scripts

### Bun

- `bun run auth:generate` - Gera o modelo para o uso do better auth
- `bun run prisma:dev` - Inicia o servidor local do prisma
- `bun run prisma:dev:update` - Atualiza o modelo de banco de dados do prisma para desenvolvimento
- `bun run prisma:studio` - Inicia a dashboard do prisma studio
- `bun run dev` - Inicia o servidor de desenvolvimento
- `bun run watch` - Inicia o servidor de desenvolvimento com autoreload
- `bun run start` - Inicia o servidor de produção
- `bun run build` - Compila o projeto
- `bun run check` - Verifica se o projeto está correto

Abra http://localhost:3000/ no seu navegador para ver o resultado.

Abra http://localhost:3000/openapi no seu navegador para ver a documentação das rotas.

## Packages

- [Elysia](https://elysiajs.com/)

## Dotenv setup

- BETTER_AUTH_URL: "http://localhost:3000" ou a url de produção
- BETTER_AUTH_SECRET: Use o [site da documentação](https://better-auth.com/docs/installation#set-environment-variables) do better auth para gerar uma chave
- DATABASE_URL: Variavel de ambiente na vercel
- POSTGRES_URL: Variavel de ambiente na vercel
- PRISMA_DATABASE_URL = Variavel de ambiente na vercel
