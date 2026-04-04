import { env } from "#env";
import { drizzle as drizzlePg } from "drizzle-orm/node-postgres";
import { schema } from "./schema/index.js";

export const db = drizzlePg(env.DATABASE_URL, {
  schema,
});
