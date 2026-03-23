//import constantsJson from "../json/constants.json" with { type: "json" };
import constantsJson from "@json/constants.json" with { type: "json" };
import laboratoriesJson from "@json/laboratories.json" with { type: "json" };

declare global {
  var constants: typeof constantsJson;
  var laboratories: typeof laboratoriesJson;
}
Object.assign(globalThis, Object.freeze({
    constants: constantsJson,
    laboratories: laboratoriesJson,
}));
//globalThis.constants = JSON.parse(JSON.stringify(constantsJson));
//globalThis.spaces = JSON.parse(JSON.stringify(spacesJson));
//globalThis.users = JSON.parse(JSON.stringify(usersJson));