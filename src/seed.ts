import { db } from "#db"
import spacesJson from "../json/spaces.json" with { type: "json" };
import { spaces } from "./db/schema/spaces.js";

async function main() {
    // Create or update data
    await db.delete(spaces);
    for (const space of spacesJson) {
        await db.insert(spaces).values({
            name: space.name,
            capacity: `${space.capacity}`,
            resources: space.resources as ("computadores" | "telão" | "tubos de ensaio")[],
        }).onConflictDoNothing();
    }
}

main();