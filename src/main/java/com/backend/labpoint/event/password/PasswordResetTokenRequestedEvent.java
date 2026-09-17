package com.backend.labpoint.event.password;

import java.util.UUID;

public record PasswordResetTokenRequestedEvent(UUID passwordResetId) {

}
