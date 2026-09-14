package com.backend.labpoint.dto.resource;

import jakarta.validation.constraints.Min;

public record ResourceRequestDTO(String name, @Min(10) Integer limit, @Min(0) Integer offset) {

}
