package com.itatechjr.bipou.dto;

import java.util.List;
import java.util.UUID;

public record ParticipanteResponseDTO(
        UUID id,
        String nome,
        String cpf,
        Long idPessoaBorala,
        Long eventoId,
        List<Long> subeventoIds
) {
    public ParticipanteResponseDTO {
        subeventoIds = subeventoIds == null ? List.of() : List.copyOf(subeventoIds);
    }
}
