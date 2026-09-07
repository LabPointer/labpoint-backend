package com.backend.labpoint.specification;

import com.backend.labpoint.domain.reserve.Reserve;
import com.backend.labpoint.domain.reserve.ReserveSchedule;
import com.backend.labpoint.domain.reserve.SchedulesEnum;
import jakarta.persistence.criteria.Predicate;
import org.springframework.data.jpa.domain.Specification;
import org.springframework.security.core.userdetails.UserDetails;

import java.time.LocalDate;
import java.time.YearMonth;
import java.util.ArrayList;
import java.util.List;
import java.util.Set;
import java.util.UUID;

public class ReserveSpecification {
    public static Specification<Reserve> filters(YearMonth yearMonth, String spaceName, String username,
            String registration/* , Set<String> resourceName */) {
        return (root, query, cb) -> {
            List<Predicate> predicates = new ArrayList<>();

            if (yearMonth != null) {
                LocalDate startOfMonth = yearMonth.atDay(1);
                LocalDate endOfMonth = yearMonth.atEndOfMonth();

                predicates.add(cb.and(
                        cb.lessThanOrEqualTo(root.get("reservedDateFrom"), endOfMonth),
                        cb.greaterThanOrEqualTo(root.get("reservedDateTo"), startOfMonth)));
            }

            if (spaceName != null && !spaceName.isBlank()) {
                predicates.add(cb.like(
                        cb.lower(root.get("space").get("name")),
                        "%" + spaceName.toLowerCase() + "%"));
            }

            if (username != null && !username.isBlank()) {
                predicates.add(cb.equal(
                        root.get("user").get("username"),
                        username));
            }

            if (registration != null && !registration.isBlank()) {
                predicates.add(cb.equal(
                        root.get("user").get("registration"),
                        username));
            }

            /*
             * if (resourceName != null && !resourceName.isEmpty()) {
             * Subquery<Integer> subquery = query.subquery(Integer.class);
             * Root<SpaceResource> spaceResourceRoot = subquery.from(SpaceResource.class);
             * 
             * subquery.select(spaceResourceRoot.get("space").get("id"))
             * .where(
             * cb.equal(
             * spaceResourceRoot.get("space").get("id"),
             * root.get("space").get("id")
             * ),
             * spaceResourceRoot.get("resource").get("name").in(resourceName)
             * );
             * 
             * predicates.add(cb.exists(subquery));
             * }
             */

            query.distinct(true);

            return cb.and(predicates.toArray(new Predicate[0]));
        };
    }

    public static Specification<Reserve> history(YearMonth yearMonth, UUID uuid) {
        return (root, query, cb) -> {
            List<Predicate> predicates = new ArrayList<>();

            if (yearMonth != null) {
                LocalDate startOfMonth = yearMonth.atDay(1);
                LocalDate endOfMonth = yearMonth.atEndOfMonth();

                predicates.add(cb.and(
                        cb.lessThanOrEqualTo(root.get("reservedDateFrom"), endOfMonth),
                        cb.greaterThanOrEqualTo(root.get("reservedDateTo"), startOfMonth)
                ));
            }

            if (uuid != null) {
                predicates.add(cb.equal(root.get("user").get("id"), uuid));
            }

            return cb.and(predicates.toArray(new Predicate[0]));
        };
    }

    public static Specification<Reserve> exists(int spaceId, LocalDate dateFrom, LocalDate dateTo) {
        return (root, query, cb) -> {
            List<Predicate> predicates = new ArrayList<>();

            predicates.add(cb.equal(root.get("space").get("id"), spaceId));

            if (dateFrom != null && dateTo != null) {
                predicates.add(cb.and(
                        cb.lessThanOrEqualTo(root.get("reservedDateFrom"), dateTo),
                        cb.greaterThanOrEqualTo(root.get("reservedDateTo"), dateFrom)));
            }

            predicates.add(
                    cb.notEqual(root.get("status"), com.backend.labpoint.domain.reserve.ScheduleStatusEnum.CANCELED));

            return cb.and(predicates.toArray(new Predicate[0]));
        };
    }

    public static Specification<ReserveSchedule> scheduleExists(List<Integer> reserveIds,
            Set<SchedulesEnum> schedules) {
        return (root, query, cb) -> {
            List<Predicate> predicates = new ArrayList<>();

            predicates.add(root.get("reserve").get("id").in(reserveIds));

            if (schedules != null && !schedules.isEmpty()) {
                predicates.add(root.get("schedule").in(schedules));
            }

            return cb.and(predicates.toArray(new Predicate[0]));
        };
    }
}
