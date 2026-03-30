import { pgTable, serial, timestamp, check, text, date, time } from "drizzle-orm/pg-core";
import { spaces } from "./spaces.js";
import { relations } from "drizzle-orm/relations";
import { sql } from "drizzle-orm/sql";

export const reserves = pgTable("reserves", {
  id: serial("id").primaryKey(),
  createdAt: timestamp("created_at").defaultNow().notNull(),
  date: date("date", { mode: "string" }).notNull(),
  startAt: time("start_at", { precision: 0 }).notNull(),
  endAt: time("end_at", { precision: 0 }).notNull(),
  spaceName: text("space_name")
    .notNull()
    .references(() => spaces.name, { onDelete: "cascade" }),
}, (table) => [
  check("check_end_after_start", sql`${table.endAt} > ${table.startAt}`),
]);

export const reservesRelations = relations(reserves, ({ one }) => ({
  spaces: one(spaces, {
    fields: [reserves.spaceName],
    references: [spaces.name],
  }),
}));