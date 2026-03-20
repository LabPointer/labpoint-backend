import { db } from "#db"
import laboratoriesJson from "../json/laboratories.json" with { type: "json" };
import { spaces } from "./db/schema/spaces.js";

async function main() {
    // Create or update data
    //await db.delete(spaces);
    for (const lab of laboratoriesJson) {
        await db.insert(spaces).values({
            name: lab.name,
            capacity: `${lab.capacity}`,
            resources: lab.resources as ("computadores" | "telão" | "tubos de ensaio")[],
        }).onConflictDoNothing();
    }
}

main();