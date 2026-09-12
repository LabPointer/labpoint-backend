package com.backend.labpoint.entities.account;

import lombok.Getter;

@Getter
public enum AccountRole {
    OWNER("owner"),
    ADMIN("admin"),
    USER("user");

    private final String role;

    AccountRole(String role) {
        this.role = role;
    }

}
