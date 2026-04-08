import { pgTable, text, numeric, pgEnum } from "drizzle-orm/pg-core";

export const resourceEnum = pgEnum("resource", [
  "computadores",
  "telão",
  "tubos de ensaio",
]);

export const spaces = pgTable("spaces", {
  name: text("name").primaryKey().unique().notNull(),
  capacity: numeric("capacity").notNull(),
  resources: resourceEnum().array(),
});
