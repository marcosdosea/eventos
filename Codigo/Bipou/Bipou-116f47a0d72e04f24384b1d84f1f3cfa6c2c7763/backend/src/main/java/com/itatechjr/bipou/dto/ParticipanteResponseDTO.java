package com.itatechjr.bipou.dto;

import java.util.UUID;

public record ParticipanteResponseDTO(
        UUID id,
        String nome,
        String cpf
) {
}
