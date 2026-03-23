import { accounts } from "./accounts.js";
import { reserves } from "./reserves.js";
import { spaces } from "./spaces.js";
import { sessions } from "./sessions.js";
import { users } from "./users.js";
import { verifications } from "./verifications.js";

export { accounts, reserves, spaces, sessions, users, verifications };

export const schema = {
    accounts,
    sessions,
    users,
    verifications,
    spaces,
    reserves
}