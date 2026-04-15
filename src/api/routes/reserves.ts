import { db, reserves, spaces } from "#db";
import type { FastifyTypedInstance } from "#types/fastify.js";
import { and, arrayOverlaps, eq, sql } from "drizzle-orm";
import { StatusCodes } from "http-status-codes";
import z from "zod";

export function reservesRoute(app: FastifyTypedInstance) {
    app.get("/reserves/:spaceName/:date",
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
                    z
                      .object({
                        id: z.string(),
                        createdAt: z.iso.datetime(),
                        date: z.iso.date(),
                        horarios: z.array(z.string()),
                        spaceName: z.string(),
                      })
                      .loose(),
                  ),
                }),
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
            const { spaceName, date } = req.params;
    
            const foundReserves = await db
              .select()
              .from(reserves)
              .where(
                and(eq(reserves.spaceName, spaceName), eq(reserves.date, date)),
              );
    
            return res.status(StatusCodes.OK).send({
              status: "success",
              data: {
                foundReserves: foundReserves.map((r) => ({
                  ...r,
                  id: String(r.id),
                  createdAt: r.createdAt.toISOString(),
                })),
              },
            });
          } catch (error) {
            console.error(error);
            return res.status(StatusCodes.INTERNAL_SERVER_ERROR).send({
              status: "error",
              message: "Erro ao buscar as reservas no banco de dados.",
            });
          }
        },
      );
    
      app.post("/reserve/create/:spaceName",
        {
          schema: {
            summary: "Registra uma reserva para um espaço.",
            tags: ["reserves"],
            params: z.object({
              spaceName: z.string(),
            }),
            body: z.object({
              date: z.iso.date(),
              horarios: z
                .array(z.string())
                .min(1, "Precisa ter pelo menos 1 horario"),
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
        async (req, res) => {
          try {
            const { spaceName } = req.params;
            const { date, horarios } = req.body;
    
            const conflict = await db.query.reserves.findFirst({
              where: (table, { and, eq }) =>
                and(
                  eq(table.spaceName, spaceName),
                  eq(table.date, date),
                  arrayOverlaps(
                    table.horarios,
                    horarios as typeof reserves.$inferInsert.horarios,
                  ),
                ),
            });
    
            if (conflict) {
              return res.status(StatusCodes.CONFLICT).send({
                status: "error",
                message:
                  "Reserva indisponível: Horário já está ocupado por outra reserva.",
              });
            }
    
            const [reserve] = await db
              .insert(reserves)
              .values({
                spaceName,
                date,
                horarios: horarios as typeof reserves.$inferInsert.horarios,
              })
              .returning();
    
            return res.status(StatusCodes.OK).send({
              status: "success",
              message: "Reserva realizada com sucesso!",
              data: reserve,
            });
          } catch (error) {
            console.error(error);
            const message = error instanceof Error ? error.message : String(error);
            return res.status(StatusCodes.INTERNAL_SERVER_ERROR).send({
              status: "error",
              message: "Erro interno: " + message,
            });
          }
        },
      );
    
      app.delete("/reserve/cancel/:id",
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
        async (req, res) => {
          try {
            const { id } = req.params;
            const [deleted] = await db
              .delete(reserves)
              .where(eq(reserves.id, Number(id)))
              .returning();
    
            if (!deleted) {
              return res.status(StatusCodes.NOT_FOUND).send({
                status: "error",
                message:
                  "Reserva não encontrada: não foi possível realizar o cancelamento.",
              });
            }
    
            return res.status(StatusCodes.OK).send({
              status: "success",
              message: "Reserva cancelada com sucesso!",
              data: deleted,
            });
          } catch (error) {
            console.error(error);
            return res.status(StatusCodes.INTERNAL_SERVER_ERROR).send({
              status: "error",
              message: "Erro interno ao tentar cancelar a reserva.",
            });
          }
        },
      );
}