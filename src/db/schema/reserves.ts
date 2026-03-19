import { randomUUIDv7 } from "bun";
import { pgTable, uuid, timestamp, check } from "drizzle-orm/pg-core";
import { spaces } from "./spaces.js";
import { relations } from "drizzle-orm/relations";
import { sql } from "drizzle-orm/sql";

export const reserves = pgTable("reserves", {
  id: uuid("id").primaryKey().$defaultFn(() => randomUUIDv7()),
  startFrom: timestamp("start_from").defaultNow().notNull(),
  endFrom: timestamp("end_at").defaultNow().notNull(),
  spaceId: uuid("space_id")
    .notNull()
    .references(() => spaces.id, { onDelete: "cascade" }),
}, (table) => ({
  checkEndAfterStart: check("check_end_after_start", sql`${table.endFrom} > ${table.startFrom}`),
}));

export const reservesRelations = relations(reserves, ({ one }) => ({
  spaces: one(spaces, {
    fields: [reserves.spaceId],
    references: [spaces.id],
  }),
}));