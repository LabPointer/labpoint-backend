package com.backend.labpoint.utils;

import java.time.Instant;
import java.time.LocalDate;
import java.time.LocalDateTime;
import java.time.ZoneId;
import java.util.UUID;

public class UUIDExtractor {
    public static LocalDate getLocalDateFromUuidV7(UUID uuid) {
        // O timestamp ocupa os 48 bits superiores do "Most Significant Bits"
        long timestamp = uuid.getMostSignificantBits() >>> 16;
        
        // Converte o timestamp (ms) para LocalDate usando o fuso horário do sistema
        return Instant.ofEpochMilli(timestamp)
                .atZone(ZoneId.systemDefault())
                .toLocalDate();
    }

    public static LocalDateTime getLocalDateTimeFromUuidV7(UUID uuid) {
        // O timestamp ocupa os 48 bits superiores do "Most Significant Bits"
        long timestamp = uuid.getMostSignificantBits() >>> 16;
        
        // Converte o timestamp (ms) para LocalDate usando o fuso horário do sistema
        return Instant.ofEpochMilli(timestamp)
                .atZone(ZoneId.systemDefault())
                .toLocalDateTime();
    }
}
