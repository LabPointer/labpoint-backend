import type { FastifyTypedInstance } from "#types/fastify.js";
import { reservesRoute } from "./reserves.js";
import { spacesRoute } from "./spaces.js";

export function registerRoutes(app: FastifyTypedInstance) {
    spacesRoute(app);
    reservesRoute(app);
}