package com.itatechjr.bipou.dto;

import java.util.List;
import java.util.UUID;

public record ParticipanteResponseDTO(
        UUID id,
        String nome,
        String cpf,
        Long idPessoaBorala,
        Long eventoId,
        List<Long> subeventoIds,
        String email
) {
    public ParticipanteResponseDTO {
        subeventoIds = subeventoIds == null ? List.of() : List.copyOf(subeventoIds);
    }

    public ParticipanteResponseDTO(
            UUID id,
            String nome,
            String cpf,
            Long idPessoaBorala,
            Long eventoId,
            List<Long> subeventoIds
    ) {
        this(id, nome, cpf, idPessoaBorala, eventoId, subeventoIds, null);
    }
}
