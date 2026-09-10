package com.backend.labpoint.dto.reserve;

import java.util.List;

public record ReserveHistoryDTO(List<ReserveScheduleDTO> next, List<ReserveScheduleDTO> concluded,
                                List<ReserveScheduleDTO> canceled) {
}
