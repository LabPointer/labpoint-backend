import { relations } from "drizzle-orm";
import { pgTable, text, timestamp, index, uuid } from "drizzle-orm/pg-core";
import { users } from "./users.js";
import { uuidv7 } from "uuidv7";

export const sessions = pgTable(
  "sessions",
  {
    id: uuid("id")
      .primaryKey()
      .$defaultFn(() => uuidv7()),
    expiresAt: timestamp("expires_at").notNull(),
    token: text("token").notNull().unique(),
    createdAt: timestamp("created_at").notNull(),
    updatedAt: timestamp("updated_at")
      .$onUpdate(() => new Date())
      .notNull(),
    ipAddress: text("ip_address"),
    userAgent: text("user_agent"),
    userId: uuid("user_id")
      .notNull()
      .references(() => users.id, { onDelete: "cascade" }),
  },
  (table) => [index("sessions_userId_idx").on(table.userId)],
);

export const sessionsRelations = relations(sessions, ({ one }) => ({
  users: one(users, {
    fields: [sessions.userId],
    references: [users.id],
  }),
}));
