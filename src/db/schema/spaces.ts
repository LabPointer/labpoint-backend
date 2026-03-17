import { randomUUIDv7 } from "bun";
import { pgTable, text, numeric, uuid, pgEnum } from "drizzle-orm/pg-core";
import { relations } from "drizzle-orm/relations";
import { reserves } from "./reserves.js";

export const resourceEnum = pgEnum("resource", ["Computadores", "Telão", "Tubos de Ensaio"]);

export const spaces = pgTable("spaces", {
    id: uuid("id").primaryKey().$defaultFn(() => randomUUIDv7()),
    name: text("name").notNull().unique(),
    capacity: numeric("capacity").notNull(),
    resources: resourceEnum().array()
});

export const spacesRelations = relations(spaces, ({ many }) => ({
  sessions: many(reserves),
}));