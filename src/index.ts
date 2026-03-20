import "./constants";
import { Elysia } from "elysia";
import { openapi } from "@elysiajs/openapi";
import { z } from "zod";
import { db } from "#db";
import { and, eq, sql } from "drizzle-orm";
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

    const spacesResult = await db.query.spaces.findMany({
      where(fields, { and, ilike, gte }) {
        return and(
          query.name ? ilike(fields.name, `%${query.name}%`) : undefined,
          query.capacity ? gte(fields.capacity, query.capacity) : undefined,
          resourcesOverlap,
        );
      },
    });

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

  .get("/reserves/:spaceName", async ({ params, query }) => {
    const foundReserves = await db
      .select()
      .from(reserves)
      .where(and(
        eq(reserves.spaceName, params.spaceName),
      ));

    const conflictingReservation = await db.query.reserves.findFirst({
      where: (table, { and, eq, lt, gt }) =>
        and(
          eq(table.spaceName, params.spaceName),
          query.endAt ? lt(table.startAt, new Date(query.endAt)) : undefined,
          query.startAt ? gt(table.endAt, new Date(query.startAt)) : undefined
        ),
    });

    return {
      foundReserves: foundReserves.map((r) => ({
        ...r,
        id: String(r.id),
        createdAt: r.createdAt.toISOString(),
        startAt: r.startAt.toISOString(),
        endAt: r.endAt.toISOString(),
      })),
      conflictingReservation: conflictingReservation
        ? {
          ...conflictingReservation,
          id: String(conflictingReservation.id),
          createdAt: conflictingReservation.createdAt.toISOString(),
          startAt: conflictingReservation.startAt.toISOString(),
          endAt: conflictingReservation.endAt.toISOString(),
        }
        : undefined,
    };
  }, {
    auth: false,

    detail: {
      summary: "Lista todas as reservas de um espaço.",
      tags: ["reserves"]
    },

    params: z.object({
      spaceName: z.string()
    }),

    query: z.object({
      startAt: z.iso.datetime().optional(),
      endAt: z.iso.datetime().optional(),
    }),

    response: {
      200: z.object({
        foundReserves: z.object({
          id: z.string(),
          createdAt: z.iso.datetime(),
          startAt: z.iso.datetime(),
          endAt: z.iso.datetime(),
          spaceName: z.string(),
        }).array(),
        conflictingReservation: z.object({
          id: z.string(),
          createdAt: z.iso.datetime(),
          startAt: z.iso.datetime(),
          endAt: z.iso.datetime(),
          spaceName: z.string(),
        }).optional(),
      })
    }
  })
  .post("/reserve/create/:spaceName", async ({ params, body, set }) => {
    const conflict = await db.query.reserves.findFirst({
      where: (table, { and, eq, lt, gt }) =>
        and(
          eq(table.spaceName, params.spaceName),
          lt(table.startAt, new Date(body.endAt)),
          gt(table.endAt, new Date(body.startAt))
        ),
    });

    if (conflict) {
      set.status = 409;
      return {
        status: "error",
        message: "Reserva indisponível: Horário já está ocupado."
      };
    }

    const [reserve] = await db
      .insert(reserves)
      .values({
        spaceName: params.spaceName,
        startAt: new Date(body.startAt),
        endAt: new Date(body.endAt),
      })
      .returning();

    return {
      status: "success",
      message: "Reserva realizada com sucesso!",
      reserve
    };
  }, {
    auth: false,

    detail: {
      summary: "Registra uma reserva para um espaço.",
      tags: ["reserves"]
    },

    params: z.object({
      spaceName: z.string()
    }),

    body: z.object({
      startAt: z.iso.datetime(),
      endAt: z.iso.datetime(),
    }),
  })
  .delete("/reserve/cancel/:id", async ({ params, set }) => {
    const [deleted] = await db
      .delete(reserves)
      .where(eq(reserves.id, Number(params.id)))
      .returning();

    if (!deleted) {
      set.status = 404;
      return {
        status: "error",
        message: "Reserva não encontrada: não foi possível realizar o cancelamento."
      };
    }

    return {
      status: "success",
      message: "Reserva cancelada com sucesso!",
      deleted
    };
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