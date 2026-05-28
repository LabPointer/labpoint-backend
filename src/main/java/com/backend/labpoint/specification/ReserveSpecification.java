package com.backend.labpoint.specification;

import com.backend.labpoint.domain.reserve.Reserve;
import com.backend.labpoint.domain.reserve.SchedulesEnum;
import jakarta.persistence.criteria.Predicate;
import org.springframework.data.jpa.domain.Specification;

import java.time.LocalDate;
import java.time.YearMonth;
import java.util.ArrayList;
import java.util.List;
import java.util.Set;

public class ReserveSpecification {
    public static Specification<Reserve> filters(YearMonth yearMonth, String spaceName, String username, String registration/*, Set<String> resourceName*/) {
        return (root, query, cb) -> {
            List<Predicate> predicates = new ArrayList<>();

            if (yearMonth != null) {
                predicates.add(cb.between(
                        root.get("reservedDate"),
                        yearMonth.atDay(1),
                        yearMonth.atEndOfMonth()
                ));
            }

            if (spaceName != null && !spaceName.isBlank()) {
                predicates.add(cb.like(
                        cb.lower(root.get("space").get("name")),
                        "%" + spaceName.toLowerCase() + "%"
                ));
            }

            if (username != null && !username.isBlank()) {
                predicates.add(cb.equal(
                        root.get("user").get("username"),
                        username
                ));
            }

            if (registration != null && !registration.isBlank()) {
                predicates.add(cb.equal(
                        root.get("user").get("registration"),
                        username
                ));
            }

            /*
            if (resourceName != null && !resourceName.isEmpty()) {
                Subquery<Integer> subquery = query.subquery(Integer.class);
                Root<SpaceResource> spaceResourceRoot = subquery.from(SpaceResource.class);

                subquery.select(spaceResourceRoot.get("space").get("id"))
                        .where(
                                cb.equal(
                                        spaceResourceRoot.get("space").get("id"),
                                        root.get("space").get("id")
                                ),
                                spaceResourceRoot.get("resource").get("name").in(resourceName)
                        );

                predicates.add(cb.exists(subquery));
            }
            */

            query.distinct(true);

            return cb.and(predicates.toArray(new Predicate[0]));
        };
    }

    public static Specification<Reserve> exists(int spaceId, Set<LocalDate> dates, Set<SchedulesEnum> schedules) {
        return (root, query, cb) -> {
            List<Predicate> predicates = new ArrayList<>();

            predicates.add(cb.equal(root.get("space").get("id"), spaceId));

            if (dates != null && !dates.isEmpty()) {
                predicates.add(root.get("reservedDate").in(dates));
            }

            if (schedules != null && !schedules.isEmpty()) {
                predicates.add(root.get("schedule").in(schedules));
            }

            return cb.and(predicates.toArray(new Predicate[0]));
        };
    }
}
