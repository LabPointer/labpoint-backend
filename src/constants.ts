import constantsJson from "../json/constants.json" with { type: "json" };
import classroomsJson from "../json/classrooms.json" with { type: "json" };
import usersJson from "../json/users.json" with { type: "json" };

declare global {
  var constants: typeof constantsJson;
  var classrooms: typeof classroomsJson;
  var users: typeof usersJson;
}

globalThis.constants = JSON.parse(JSON.stringify(constantsJson));
globalThis.classrooms = JSON.parse(JSON.stringify(classroomsJson));
globalThis.users = JSON.parse(JSON.stringify(usersJson));