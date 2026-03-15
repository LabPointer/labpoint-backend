/*
import { betterAuth } from "better-auth";
import { prismaAdapter } from "better-auth/adapters/prisma";
import { db } from "#database";
import { password } from "bun";

export const auth = betterAuth({
    database: prismaAdapter(db, {
        provider: "postgresql",
        usePlural: true
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
*/