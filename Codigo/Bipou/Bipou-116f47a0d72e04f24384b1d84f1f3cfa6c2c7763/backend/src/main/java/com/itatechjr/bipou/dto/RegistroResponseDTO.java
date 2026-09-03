package com.itatechjr.bipou.dto;

import com.itatechjr.bipou.model.enums.TipoAcao;

import java.time.OffsetDateTime;
import java.util.UUID;

public record RegistroResponseDTO(
        UUID id,
        UUID leituraId,
        UUID participanteId,
        String participanteNome,
        TipoAcao tipoAcao,
        OffsetDateTime dataHora,
        OffsetDateTime dataHoraLidaNoCelular,
        String dispositivoId
) {
}
