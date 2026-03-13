import ck from "chalk";
import { z, ZodObject, ZodRawShape } from "zod";
import chalk from "chalk";

const x = chalk.red("✖︎");
const w = chalk.yellow("▲");

function validateEnv<T extends ZodRawShape>(schema: ZodObject<T>){
    const result = schema.loose().safeParse(process.env);
    if (!result.success){
        const u = ck.underline;
        for(const error of result.error.issues){
            const { path, message } = error;
            console.error(`${x} ENV VAR → ${u.bold(path)} ${message}`);
            if (error.code == "invalid_type")
                console.log(ck.dim(
                    `└ "Expected: ${u.green(error.expected)} | Received: ${u.red(error.input)}`
                ));
        }
        process.exit(1);
    }
    console.log(ck.green(`${ck.magenta("☰ Environment variables")} loaded ✓`));

    return result.data as Record<string, string> & z.infer<typeof schema>;
}

export const env = validateEnv(z.object({
    BETTER_AUTH_URL: z.string("Better auth url is required").min(1),
    BETTER_AUTH_SECRET: z.string("Better auth secret is required").min(1),
    DATABASE_URL: z.string("Database url is required").min(1),
    POSTGRES_URL: z.string("Postgres url is required").min(1),
    PRISMA_DATABASE_URL: z.string("Prisma database url is required").min(1)
}));