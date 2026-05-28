package com.backend.labpoint.domain.reserve;

import java.time.LocalDate;

public record UpdateReserveRequestDTO(LocalDate reservedDate, SchedulesEnum schedule, Boolean lock,
                                      String userRegistration, Integer spaceId) {
}
