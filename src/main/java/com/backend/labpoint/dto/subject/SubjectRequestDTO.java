package com.backend.labpoint.dto.subject;

import jakarta.validation.constraints.Min;

public record SubjectRequestDTO(String name, @Min(10) Integer limit, @Min(0) Integer offset) {

}
