import "./constants";
import { Elysia } from "elysia";
import { openapi } from "@elysiajs/openapi";
import { z } from "zod";
import { db } from "#db";
import { and, eq, gte, ilike, sql } from "drizzle-orm";
import { spaces } from "./db/schema/spaces.js";
import { reserves } from "./db/schema/reserves.js";

const app = new Elysia()
  .use(openapi())
  .get("/", () => "Hello Elysia")
  .get("/spaces", async ({ query }) => {
    const resourcesRaw =
      Array.isArray(query.resources) ? query.resources.join(",") : query.resources ?? "";

    const resourceList = resourcesRaw
      .split(",")
      .map((r: string) => r.trim())
      .filter(Boolean);

    const resourcesOverlap =
      resourceList.length === 0
        ? undefined
        : sql`${spaces.resources} && ${sql`ARRAY[${sql.join(
            resourceList.map((r: string) => sql`${r}`),
            sql`, `,
          )}]::resource[]`}`;

    const where = and(
      query.id ? eq(spaces.id, query.id) : undefined,
      query.name ? ilike(spaces.name, `%${query.name}%`) : undefined,
      query.capacity ? gte(spaces.capacity, query.capacity) : undefined,
      resourcesOverlap,
    );

    const spacesResult = await db.select().from(spaces).where(where);

    return spacesResult;
  }, {
    auth: false,

    detail: {
      summary: "Lista todos os laboratorios.",
      description: "/spaces?name=lab&capacity=20&resources=TELAO",
      tags: ["laboratorio"]
    },

    query: z.object({
      id: z.string().optional(),
      name: z.string().optional(),
      capacity: z.string().optional(),
      resources: z.union([z.string(), z.array(z.string())]).optional(),
    })
  })

  .get("/reserves", async ({ params }) => {
    const rows = await db
      .select()
      .from(reserves)
      .where(eq(reserves.spaceId, params.spaceId));

    return rows;
  }, {
    auth: false,

    detail: {
      summary: "Lista todas as reservas de um espaço.",
      tags: ["reserves"]
    },

    params: z.object({
      spaceId: z.string()
    }),
  })
  .post("/reserve/create/:id", async ({ params, body }) => {
    const [reserve] = await db
      .insert(reserves)
      .values({
        spaceId: params.spaceId,
        startFrom: new Date(body.startFrom),
        endFrom: new Date(body.endUntil),
      })
      .returning();

    return reserve;
  }, {
    auth: false,

    detail: {
      summary: "Cria uma reserva para um espaço.",
      tags: ["reserves"]
    },

    params: z.object({
      spaceId: z.string()
    }),

    body: z.object({
      startFrom: z.iso.datetime(),
      endUntil: z.iso.datetime(),
    }),
  })
  .delete("/reserve/cancel/:id", async ({ params }) => {
    const [deleted] = await db
      .delete(reserves)
      .where(eq(reserves.id, params.id))
      .returning();

    return deleted;
  }, {
    auth: false,

    detail: {
      summary: "Deleta uma reserva pelo id",
      tags: ["reserves"]
    },

    params: z.object({
      id: z.string()
    }),
  })
  .listen(3001);

console.log(
  `🦊 Elysia is running at ${app.server?.hostname}:${app.server?.port}\n`
);

/*
  .use(betterAuthPlugins)
  .all("/api/auth/*", async (ctx) => {
    return auth.handler(ctx.request);
  })
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
*/