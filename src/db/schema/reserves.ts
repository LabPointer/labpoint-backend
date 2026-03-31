import { pgTable, serial, timestamp, text, date, pgEnum } from "drizzle-orm/pg-core";
import { spaces } from "./spaces.js";
import { relations } from "drizzle-orm/relations";

export const horarioEnum = pgEnum('horario', [
  'M-Aula1', 'M-Aula2', 'M-Aula3', 'M-Aula4', 'M-Aula5',
  'V-Aula1', 'V-Aula2', 'V-Aula3', 'V-Aula4', 'V-Aula5',
  'N-Aula1', 'N-Aula2', 'N-Aula3', 'N-Aula4'
]);

export const reserves = pgTable("reserves", {
  id: serial("id").primaryKey(),
  createdAt: timestamp("created_at").defaultNow().notNull(),
  date: date("date", { mode: "string" }).notNull(),
  horarios: horarioEnum("horarios").array().notNull(),
  spaceName: text("space_name")
    .notNull()
    .references(() => spaces.name, { onDelete: "cascade" }),
});

export const reservesRelations = relations(reserves, ({ one }) => ({
  spaces: one(spaces, {
    fields: [reserves.spaceName],
    references: [spaces.name],
  }),
}));