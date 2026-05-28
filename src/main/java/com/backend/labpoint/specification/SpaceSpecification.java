package com.backend.labpoint.specification;

import com.backend.labpoint.domain.space.Space;
import com.backend.labpoint.domain.space.SpaceResource;
import com.backend.labpoint.domain.space.SpaceSubject;
import jakarta.persistence.criteria.Predicate;
import jakarta.persistence.criteria.Root;
import jakarta.persistence.criteria.Subquery;
import org.springframework.data.jpa.domain.Specification;

import java.util.ArrayList;
import java.util.List;
import java.util.Set;

public class SpaceSpecification {

    public static Specification<Space> filters(String name, Integer capacity, Set<Integer> resources,
                                               Set<Integer> subjects) {
        return (root, query, cb) -> {
            List<Predicate> predicates = new ArrayList<>();

            if (name != null && !name.trim().isEmpty()) {
                predicates.add(cb.like(cb.lower(root.get("name")), "%" + name.trim().toLowerCase() + "%"));
            }

            if (capacity != null) {
                predicates.add(cb.greaterThanOrEqualTo(root.get("capacity"), capacity));
            }

            if (resources != null && !resources.isEmpty()) {
                Subquery<Integer> resourceSubquery = query.subquery(Integer.class);
                Root<SpaceResource> spaceResourceRoot = resourceSubquery.from(SpaceResource.class);
                resourceSubquery.select(spaceResourceRoot.get("space").get("id"))
                        .where(spaceResourceRoot.get("resource").get("id").in(resources));
                predicates.add(root.get("id").in(resourceSubquery));
            }

            if (subjects != null && !subjects.isEmpty()) {
                Subquery<Integer> subjectSubquery = query.subquery(Integer.class);
                Root<SpaceSubject> spaceSubjectRoot = subjectSubquery.from(SpaceSubject.class);
                subjectSubquery.select(spaceSubjectRoot.get("space").get("id"))
                        .where(spaceSubjectRoot.get("subject").get("id").in(subjects));
                predicates.add(root.get("id").in(subjectSubquery));
            }

            return cb.and(predicates.toArray(new Predicate[0]));
        };
    }

}
