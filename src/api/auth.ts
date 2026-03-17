
import { betterAuth } from "better-auth";
import { db } from "#db";
import { drizzleAdapter } from "@better-auth/drizzle-adapter";

export const auth = betterAuth({
    database: drizzleAdapter(db, {
        provider: "pg",
        usePlural: true,
        camelCase: false
    }),
    emailAndPassword: {
        enabled: true,
        autoSignIn: true,
        password: {
            hash: (password: string) => Bun.password.hash(password),
            verify: ({ password, hash }) => Bun.password.verify(password, hash)
        }
    },
    session: {
        expiresIn: 60 * 60 * 24 * 7,
        cookieCache: {
            enabled: true,
            maxAge: 60 * 10
        }
    }
});
