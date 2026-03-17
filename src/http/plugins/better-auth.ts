
import { Elysia } from "elysia";
import { auth } from "#api";

export const betterAuthPlugins = new Elysia({ name: "better-auth" })
    .mount(auth.handler)
    .macro({
        auth: {
            async resolve({ status, request: { headers } }) {
                const session = await auth.api.getSession({ headers });

                if (!session) {
                    return status(401, { message: "Unauthorized" });
                }

                return session;
            }
        }
    })
