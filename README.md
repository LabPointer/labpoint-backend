# Labpoint Backend

Labpoint é um sistema de agendamento de laboratórios, criado em primeiro momento para a instituição do SENAI com o intuito de organizar melhor a disposição das aulas na unidade e ajudar os professores a organizar e planejar os dias de acordo com a necessidade da disciplina. O backend concentra recursos para o desenvolvimento funcional do sistema.

> [!AVISO]
> Versão recomendada do [Nodejs](https://bun.sh/) é 24 LTS ou superior

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

Abra http://localhost:3000/openapi no seu navegador para ver a documentação das rotas.

## Packages

- [Fastify](https://fastify.dev/)

## Dotenv setup

- BETTER_AUTH_URL: "http://localhost:3000" ou a url de produção
- BETTER_AUTH_SECRET: Use o [site da documentação](https://better-auth.com/docs/installation#set-environment-variables) do better auth para gerar uma chave
- DATABASE_URL: postgresql://labpoint:labpoint@localhost:5432/labpoint
