import { db, reserves, spaces } from "#db";
import type { FastifyTypedInstance } from "#types/fastify.js";
import { and, eq, sql } from "drizzle-orm";
import { z } from "zod";

export async function routes(app: FastifyTypedInstance) {
    app.get(
        "/spaces",
        {
            schema: {
                summary: "Lista todos os laboratorios.",
                description: "/spaces?name=lab&capacity=20&resources=TELAO",
                tags: ["laboratorio"],
                querystring: z.object({
                    id: z.string().optional(),
                    name: z.string().optional(),
                    capacity: z.string().optional(),
                    resources: z
                        .union([z.string(), z.array(z.string())])
                        .optional(),
                }),
                response: {
                    200: z.object({
                        status: z.literal("success"),
                        data: z.array(
                            z
                                .object({
                                    id: z.number().or(z.string()).optional(),
                                    name: z.string(),
                                    capacity: z.number().or(z.string()),
                                    resources: z.array(z.string()).nullable(),
                                })
                                .loose(),
                        ),
                    }),
                    500: z.object({
                        status: z.literal("error"),
                        message: z.string(),
                    }),
                },
            },
        },
        async (request, reply) => {
            try {
                const { query } = request;
                const resourcesRaw = Array.isArray(query.resources)
                    ? query.resources.join(",")
                    : (query.resources ?? "");

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
                            query.name
                                ? ilike(fields.name, `%${query.name}%`)
                                : undefined,
                            query.capacity
                                ? gte(fields.capacity, query.capacity)
                                : undefined,
                            resourcesOverlap,
                        );
                    },
                });

                return {
                    status: "success",
                    data: spacesResult,
                } as const;
            } catch (error) {
                console.error(error);
                reply.status(500);
                return {
                    status: "error",
                    message: "Erro ao buscar os espaços no banco de dados.",
                } as const;
            }
        },
    );

    app.get(
        "/reserves/:spaceName/:date",
        {
            schema: {
                summary: "Lista todas as reservas de um espaço.",
                tags: ["reserves"],
                params: z.object({
                    spaceName: z.string(),
                    date: z.iso.date(),
                }),
                response: {
                    200: z.object({
                        status: z.literal("success"),
                        data: z.object({
                            foundReserves: z.array(
                                z.object({
                                        id: z.string(),
                                        createdAt: z.iso.datetime(),
                                        date: z.iso.date(),
                                        startAt: z.iso.time(),
                                        endAt: z.iso.time(),
                                        spaceName: z.string(),
                                    })
                                    .loose(),
                            )
                        }),
                    }),
                    500: z.object({
                        status: z.literal("error"),
                        message: z.string(),
                    }),
                },
            },
        },
        async (request, reply) => {
            try {
                const { spaceName, date } = request.params;

                const foundReserves = await db
                    .select()
                    .from(reserves)
                    .where(
                        and(
                            eq(reserves.spaceName, spaceName),
                            eq(reserves.date, date),
                        ),
                    );

                return {
                    status: "success",
                    data: {
                        foundReserves: foundReserves.map((r) => ({
                            ...r,
                            id: String(r.id),
                            createdAt: r.createdAt.toISOString(),
                            startAt: r.startAt,
                            endAt: r.endAt,
                        }))
                    },
                } as const;
            } catch (error) {
                console.error(error);
                reply.status(500);
                return {
                    status: "error",
                    message: "Erro ao buscar as reservas no banco de dados.",
                } as const;
            }
        },
    );

    app.post(
        "/reserve/create/:spaceName",
        {
            schema: {
                summary: "Registra uma reserva para um espaço.",
                tags: ["reserves"],
                params: z.object({
                    spaceName: z.string(),
                }),
                body: z.object({
                    date: z.iso.date(),
                    startAt: z.iso.time(),
                    endAt: z.iso.time(),
                }),
                response: {
                    200: z.object({
                        status: z.literal("success"),
                        message: z.string(),
                        data: z.any(),
                    }),
                    409: z.object({
                        status: z.literal("error"),
                        message: z.string(),
                    }),
                    500: z.object({
                        status: z.literal("error"),
                        message: z.string(),
                    }),
                },
            },
        },
        async (request, reply) => {
            try {
                const { spaceName } = request.params;
                const { date, startAt, endAt } = request.body;

                const conflict = await db.query.reserves.findFirst({
                    where: (table, { and, eq }) =>
                        and(
                            eq(table.spaceName, spaceName),
                            eq(table.date, date),
                            eq(table.startAt, startAt),
                            eq(table.endAt, endAt),
                        ),
                });

                if (conflict) {
                    reply.status(409);
                    return {
                        status: "error",
                        message:
                            "Reserva indisponível: Horário já está ocupado por outra reserva.",
                    } as const;
                }

                const [reserve] = await db
                    .insert(reserves)
                    .values({
                        spaceName,
                        date,
                        startAt,
                        endAt,
                    })
                    .returning();

                return {
                    status: "success",
                    message: "Reserva realizada com sucesso!",
                    data: reserve,
                } as const;
            } catch (error) {
                console.error(error);
                reply.status(500);
                return {
                    status: "error",
                    message: "Erro interno ao tentar criar a reserva.",
                } as const;
            }
        },
    );

    app.delete(
        "/reserve/cancel/:id",
        {
            schema: {
                summary: "Deleta uma reserva pelo id",
                tags: ["reserves"],
                params: z.object({
                    id: z.string(),
                }),
                response: {
                    200: z.object({
                        status: z.literal("success"),
                        message: z.string(),
                        data: z.any(),
                    }),
                    404: z.object({
                        status: z.literal("error"),
                        message: z.string(),
                    }),
                    500: z.object({
                        status: z.literal("error"),
                        message: z.string(),
                    }),
                },
            },
        },
        async (request, reply) => {
            try {
                const { id } = request.params;
                const [deleted] = await db
                    .delete(reserves)
                    .where(eq(reserves.id, Number(id)))
                    .returning();

                if (!deleted) {
                    reply.status(404);
                    return {
                        status: "error",
                        message:
                            "Reserva não encontrada: não foi possível realizar o cancelamento.",
                    } as const;
                }

                return {
                    status: "success",
                    message: "Reserva cancelada com sucesso!",
                    data: deleted,
                } as const;
            } catch (error) {
                console.error(error);
                reply.status(500);
                return {
                    status: "error",
                    message: "Erro interno ao tentar cancelar a reserva.",
                } as const;
            }
        },
    );
}
