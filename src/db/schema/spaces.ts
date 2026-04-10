import { pgTable, text, numeric, pgEnum, serial } from "drizzle-orm/pg-core";

export const resourceEnum = pgEnum("resource", [
  "computadores",
  "telão",
  "tubos de ensaio",
]);

export const spaces = pgTable("spaces", {
  id: serial("id").primaryKey(),
  name: text("name").unique().notNull(),
  capacity: numeric("capacity").notNull(),
  resources: resourceEnum().array(),
});
