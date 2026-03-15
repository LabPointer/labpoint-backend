import constantsJson from "../json/constants.json" with { type: "json" };
import spacesJson from "../json/spaces.json" with { type: "json" };

declare global {
  var constants: typeof constantsJson;
  var spaces: typeof spacesJson;
}
Object.assign(globalThis, Object.freeze({
    constants: constantsJson,
    spaces: spacesJson,
}));
//globalThis.constants = JSON.parse(JSON.stringify(constantsJson));
//globalThis.spaces = JSON.parse(JSON.stringify(spacesJson));
//globalThis.users = JSON.parse(JSON.stringify(usersJson));