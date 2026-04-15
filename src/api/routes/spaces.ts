import { db, spaces } from "#db";
import type { FastifyTypedInstance } from "#types/fastify.js";
import { sql } from "drizzle-orm";
import { StatusCodes } from "http-status-codes";
import z from "zod";

export function spacesRoute(app: FastifyTypedInstance) {
    app.get("/spaces",
        {
            schema: {
                summary: "Lista todos os laboratorios.",
                description: "/spaces?name=lab&capacity=20&resources=TELAO",
                tags: ["laboratorio"],
                querystring: z.object({
                    id: z.string().optional(),
                    name: z.string().optional(),
                    capacity: z.string().optional(),
                    resources: z.union([z.string(), z.array(z.string())]).optional(),
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
        async (req, res) => {
            try {
                const { query } = req;
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
                            query.name ? ilike(fields.name, `%${query.name}%`) : undefined,
                            query.capacity ? gte(fields.capacity, query.capacity) : undefined,
                            resourcesOverlap,
                        );
                    },
                });

                return res.status(StatusCodes.OK).send({
                    status: "success",
                    data: spacesResult,
                });
            } catch (error) {
                console.error(error);
                return res.status(StatusCodes.INTERNAL_SERVER_ERROR).send({
                    status: "error",
                    message: "Erro ao buscar os espaços no banco de dados.",
                });
            }
        },
    );
}