package com.backend.labpoint.specification;

import java.util.ArrayList;
import java.util.List;

import org.springframework.data.jpa.domain.Specification;

import com.backend.labpoint.entities.account.Account;
import com.backend.labpoint.entities.account.AccountRole;

import jakarta.persistence.criteria.Predicate;

public class AccountSpecification {
    public static Specification<Account> filters(String registration, String username, String email, AccountRole role) {
        return (root, query, cb) -> {
            List<Predicate> predicates = new ArrayList<>();

            if (registration != null && !registration.isBlank()) {
                predicates.add(cb.like(root.get("registration"), "%" + registration + "%"));
            }

            if (username != null && !username.isBlank()) {
                predicates.add(cb.like(root.get("username"), "%" + username + "%"));
            }

            if (email != null && !email.isBlank()) {
                predicates.add(cb.like(root.get("templates/email"), "%" + email + "%"));
            }

            if (role != null) {
                predicates.add(cb.equal(root.get("role"), role));
            }

            return cb.and(predicates.toArray(new Predicate[0]));
        };
    }
}
