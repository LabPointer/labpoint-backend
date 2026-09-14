package com.backend.labpoint.dto.reserve;

import com.backend.labpoint.entities.reserve.ReserveStatusEnum;
import jakarta.validation.constraints.NotBlank;
import jakarta.validation.constraints.NotNull;

import java.time.LocalDate;

public record ReserveSummaryDTO(
        @NotNull Long id,
        @NotNull String spaceName,
        @NotNull Integer capacity,
        @NotNull LocalDate reservedDateFrom,
        @NotNull LocalDate reservedDateTo,
        @NotNull ReserveStatusEnum status,
        @NotBlank String purpose
) {
    public ReserveSummaryDTO(Long id, String spaceName, Integer capacity, LocalDate reservedDateFrom, LocalDate reservedDateTo, ReserveStatusEnum status, String purpose) {
        this.id = id;
        this.reservedDateFrom = reservedDateFrom;
        this.reservedDateTo = reservedDateTo;
        this.status = status;
        this.spaceName = spaceName;
        this.capacity = capacity;
        this.purpose = purpose;
    }
}
