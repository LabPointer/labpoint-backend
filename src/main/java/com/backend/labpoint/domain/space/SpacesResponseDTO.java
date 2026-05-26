package com.backend.labpoint.domain.space;

import lombok.AllArgsConstructor;
import lombok.Getter;
import lombok.NoArgsConstructor;
import lombok.Setter;

import java.util.List;

@Getter
@Setter
@AllArgsConstructor
@NoArgsConstructor
public class SpacesResponseDTO {
    private List<SpaceDTO> spaces;

    private Integer offset;

    private Integer limit;

    private Integer total;
}
