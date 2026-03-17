import { env } from "#env";
import { drizzle } from "drizzle-orm/neon-http";
import { drizzle as drizzlePg } from "drizzle-orm/node-postgres";
import { neon } from "@neondatabase/serverless";
import { schema } from './schema/index.js';

export const db = (() => {
    if (env.DATABASE_URL === "postgresql://labpoint:labpoint@localhost:5432/labpoint") {
        return drizzlePg(env.DATABASE_URL, { 
            schema
        });
    }
    const sql = neon(env.DATABASE_URL);
    return drizzle({ 
        client: sql,
        schema
    });
})()