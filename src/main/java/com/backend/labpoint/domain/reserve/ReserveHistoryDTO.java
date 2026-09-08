package com.backend.labpoint.domain.reserve;

import java.util.List;

public record ReserveHistoryDTO(List<ReserveScheduleDTO> next, List<ReserveScheduleDTO> concluded,
                                List<ReserveScheduleDTO> canceled) {
}
