package com.backend.labpoint.dto.reserve;

import com.backend.labpoint.entities.reserve.ScheduleStatusEnum;
import jakarta.validation.constraints.NotBlank;
import jakarta.validation.constraints.NotNull;

import java.time.LocalDate;
import java.time.OffsetDateTime;

public record ReserveDTO(@NotNull Integer id, @NotNull OffsetDateTime createdAt, @NotNull LocalDate reservedDateFrom, @NotNull LocalDate reservedDateTo, @NotNull ScheduleStatusEnum status, @NotBlank String purpose) {
}
