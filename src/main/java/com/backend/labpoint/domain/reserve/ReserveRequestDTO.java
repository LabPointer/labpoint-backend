package com.backend.labpoint.domain.reserve;

import jakarta.validation.constraints.NotNull;

import java.time.YearMonth;

public record ReserveRequestDTO(
    @NotNull(message = "É necessario passar o mes e o ano") 
    YearMonth yearMonth,
    String spaceName, 
    String username, 
    String registration) {
}
