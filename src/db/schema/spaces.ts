import { pgTable, text, numeric, uuid, pgEnum } from "drizzle-orm/pg-core";
import { relations } from "drizzle-orm/relations";
import { reserves } from "./reserves.js";

export const resourceEnum = pgEnum("resource", ["computadores", "telão", "tubos de ensaio"]);

export const spaces = pgTable("spaces", {
    name: text("name").primaryKey().unique().notNull(),
    capacity: numeric("capacity").notNull(),
    resources: resourceEnum().array()
});