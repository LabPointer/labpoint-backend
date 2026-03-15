import "./constants";
import { Elysia } from "elysia";
import { openapi } from "@elysiajs/openapi";
import { z } from "zod";
import { db } from "#database";
import { Resource } from "./generated/prisma/enums.js";
//import { auth } from "./api/auth.js";
//import { betterAuthPlugins } from "./http/plugins/better-auth.js";

const app = new Elysia()
  .use(openapi())
  .get("/", () => "Hello Elysia")
  .get("/spaces", async ({ query }) => {
    const labs = await db.spaces.findMany({
      where: {
        id: query.id,
        name: query.name ? { contains: query.name, mode: "insensitive" } : undefined,
        capacity: query.capacity ? { gte: Number(query.capacity) } : undefined,
        resources: query.resources ? { hasSome: query.resources.split(",") as Resource[] } : undefined,
      }
    });

    return labs;
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
      resources: z.string().optional(),
    }),
  })

  .get("/reserves", async ({ params }) => {
    const reserves = await db.reserves.findMany({
      where: {
        spacesId: params.spaceId
      }
    });

    return reserves;
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
    const reserve = await db.reserves.create({
      data: {
        spacesId: params.id,
        startFrom: new Date(body.startFrom),
        endUntil: new Date(body.endUntil),
      }
    })

    return reserve;
  }, {
    auth: false,

    detail: {
      summary: "Cria uma reserva para um espaço.",
      tags: ["reserves"]
    },

    params: z.object({
      id: z.string()
    }),

    body: z.object({
      startFrom: z.iso.datetime(),
      endUntil: z.iso.datetime(),
    }),
  })
  .delete("/reserve/cancel/:id", async ({ params }) => {
    const reserves = await db.reserves.delete({ where: { id: Number(params.id) } })

    return reserves;
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
  .listen(3000);

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