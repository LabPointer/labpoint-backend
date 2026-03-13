import "./constants";
import { Elysia } from "elysia";
import { openapi } from "@elysiajs/openapi";
import { auth } from "./api/auth.js";

import fs from "node:fs/promises";
import path from "node:path";
import { z } from "zod";
import { betterAuthPlugins } from "./http/plugins/better-auth.js";

async function saveClassrooms() {
  await fs.writeFile(
    path.join(import.meta.dir, "../json/classrooms.json"),
    JSON.stringify(classrooms, null, 2)
  );
}

async function saveUsers() {
  await fs.writeFile(
    path.join(import.meta.dir, "../json/users.json"),
    JSON.stringify(users, null, 2)
  );
}

const app = new Elysia()
  .use(openapi())
  .use(betterAuthPlugins)
  .all("/api/auth/*", async (ctx) => {
    return auth.handler(ctx.request);
  })
  .get("/", () => "Hello Elysia")
  .get("/users/:id", ({ params }) => {


    return { id: "nathan", name: "nathan" };
  }, {

    detail: {
      summary: "Buscar usuario pelo id.",
      tags: ["users"]
    },

    params: z.object({
      id: z.string()
    }),

    response: {
      200: z.object({
        id: z.string(),
        name: z.string()
      })
    }

  })
  .listen(3000);

console.log(
  `🦊 Elysia is running at ${app.server?.hostname}:${app.server?.port}\n`
);
