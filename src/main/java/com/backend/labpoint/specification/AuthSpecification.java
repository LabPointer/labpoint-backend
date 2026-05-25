package com.backend.labpoint.specification;

import com.backend.labpoint.domain.space.Space;
import com.backend.labpoint.domain.space.SpaceResource;
import com.backend.labpoint.domain.space.SpaceSubject;
import com.backend.labpoint.domain.user.User;
import com.backend.labpoint.domain.user.UserRole;
import jakarta.persistence.criteria.Predicate;
import jakarta.persistence.criteria.Root;
import jakarta.persistence.criteria.Subquery;
import org.springframework.data.jpa.domain.Specification;

import java.util.ArrayList;
import java.util.List;

public class AuthSpecification {
    public static Specification<User> filters(String registration, String username, String email, UserRole role) {
        return (root, query, cb) -> {
            List<Predicate> predicates = new ArrayList<>();

            if (registration != null && !registration.isBlank()) {
                predicates.add(cb.like(root.get("registration"), "%" + registration + "%"));
            }

            if (username != null && !username.isBlank()) {
                predicates.add(cb.like(root.get("username"), "%" + username + "%"));
            }

            if (email != null && !email.isBlank()) {
                predicates.add(cb.like(root.get("email"), "%" + email + "%"));
            }

            if (role != null) {
                predicates.add(cb.equal(root.get("role"), role));
            }

            return cb.and(predicates.toArray(new Predicate[0]));
        };
    }
}
