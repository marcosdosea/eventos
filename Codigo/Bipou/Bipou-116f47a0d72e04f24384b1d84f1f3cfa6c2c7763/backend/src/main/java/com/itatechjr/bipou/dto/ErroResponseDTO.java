package com.itatechjr.bipou.dto;

import java.time.OffsetDateTime;
import java.util.Map;

public record ErroResponseDTO(
        OffsetDateTime dataHora,
        int status,
        String erro,
        String mensagem,
        String caminho,
        Map<String, String> campos
) {
    public ErroResponseDTO {
        campos = campos == null ? Map.of() : Map.copyOf(campos);
    }
}
