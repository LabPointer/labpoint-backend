import "./constants";
import { Elysia } from "elysia";

import fs from "node:fs/promises";
import path from "node:path";

async function saveClassrooms() {
  await fs.writeFile(
    path.join(import.meta.dir, "../json/classrooms.json"),
    JSON.stringify(classrooms, null, 2)
  );
}

async function saveUsers() {
  await fs.writeFile(
    path.join(import.meta.dir, "../json/users.json"),
    JSON.stringify(users, null, 2)
  );
}

const app = new Elysia()
  .get("/", () => ({ message: "Server is running" }))
  .get("/classrooms", () => classrooms)
  .get("/users", () => users)
  .post("/reservations", async ({ body, set }) => {
    const payload = body as { userId?: string; classroomId?: string; startTimestamp?: number; endTimestamp?: number };
    const { userId, classroomId, startTimestamp, endTimestamp } = payload;
    
    if (!userId || !classroomId || !startTimestamp || !endTimestamp) {
       set.status = 400;
       return { error: "Missing required fields: userId, classroomId, startTimestamp, endTimestamp" };
    }

    const user = users.find(u => u.id === userId);
    const classroom = classrooms.find(c => c.id === classroomId);

    if (!user || !classroom) {
       set.status = 404;
       return { error: "User or Classroom not found" };
    }

    // Initialize arrays if they don't exist
    if (!user.reservedRooms) user.reservedRooms = [];
    if (!classroom.reservations) classroom.reservations = [];

    user.reservedRooms.push({ classroomId, startTimestamp, endTimestamp });
    classroom.reservations.push({ userId, startTimestamp, endTimestamp });

    await saveUsers();
    await saveClassrooms();

    return { success: true, user, classroom };
  })
  .delete("/reservations", async ({ body, set }) => {
    const payload = body as { userId?: string; classroomId?: string; startTimestamp?: number; endTimestamp?: number };
    const { userId, classroomId, startTimestamp, endTimestamp } = payload;
    
    if (!userId || !classroomId || !startTimestamp || !endTimestamp) {
       set.status = 400;
       return { error: "Missing required fields: userId, classroomId, startTimestamp, endTimestamp" };
    }

    const user = users.find(u => u.id === userId);
    const classroom = classrooms.find(c => c.id === classroomId);

    if (!user || !classroom) {
       set.status = 404;
       return { error: "User or Classroom not found" };
    }

    if (user.reservedRooms) {
      user.reservedRooms = user.reservedRooms.filter(
          r => !(r.classroomId === classroomId && r.startTimestamp === startTimestamp && r.endTimestamp === endTimestamp)
      );
    }
    
    if (classroom.reservations) {
      classroom.reservations = classroom.reservations.filter(
          r => !(r.userId === userId && r.startTimestamp === startTimestamp && r.endTimestamp === endTimestamp)
      );
    }

    await saveUsers();
    await saveClassrooms();

    return { success: true, message: "Reservation cancelled successfully", user, classroom };
  })
  .listen(3000);

console.log(
  `🦊 Elysia is running at ${app.server?.hostname}:${app.server?.port}\n`
);
