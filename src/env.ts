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
    // TOKEN: z.string("Token is required").min(1)
}));