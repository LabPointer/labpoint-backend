import { db } from "#database"
import { Resource } from "./generated/prisma/enums.js"
import spacesJson from "../json/spaces.json" with { type: "json" };
/*
const spaces = [
  { id: "auditorio", name: "Auditorio", resources: [Resource.COMPUTADORES] },
]
  */

async function main() {
  // Create or update data
  for (const space of spacesJson) {
    const s = await db.spaces.upsert({
      where: { id: space.id },
      update: {},
      create: {
        id: space.id,
        name: space.name,
        capacity: space.capacity,
        resources: space.resources as Resource[],
      },
    })
    console.log(s);
  }
}

main()
  .catch((e) => {
    console.error(e)
    process.exit(1)
  })
  .finally(async () => {
    await db.$disconnect()
  })