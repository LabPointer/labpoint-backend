import { randomUUIDv7 } from "bun";
import { pgTable, text, timestamp, index, uuid } from "drizzle-orm/pg-core";

export const verifications = pgTable(
  "verifications",
  {
    id: uuid("id").primaryKey().$defaultFn(() => randomUUIDv7()),
    identifier: text("identifier").notNull(),
    value: text("value").notNull(),
    expiresAt: timestamp("expires_at").notNull(),
    createdAt: timestamp("created_at").notNull(),
    updatedAt: timestamp("updated_at")
      .$onUpdate(() => new Date())
      .notNull(),
  },
  (table) => [index("verifications_identifier_idx").on(table.identifier)],
);
