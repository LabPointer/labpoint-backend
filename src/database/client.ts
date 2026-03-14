import { PrismaClient } from "@src/generated/prisma/client.js";
import { PrismaPg } from "@prisma/adapter-pg";
import { env } from "#env";

const adapter = new PrismaPg({ connectionString: env.DATABASE_URL });
export const db = new PrismaClient({ adapter });