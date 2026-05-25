package com.backend.labpoint.domain.space;

import java.util.List;

import jakarta.validation.constraints.NotBlank;
import jakarta.validation.constraints.NotNull;
import lombok.AllArgsConstructor;
import lombok.Getter;
import lombok.NoArgsConstructor;
import lombok.Setter;

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
