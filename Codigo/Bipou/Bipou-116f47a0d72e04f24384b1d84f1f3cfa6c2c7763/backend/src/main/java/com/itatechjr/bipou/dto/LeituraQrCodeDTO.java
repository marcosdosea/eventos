package com.itatechjr.bipou.dto;

import com.itatechjr.bipou.model.enums.TipoAcao;
import jakarta.validation.constraints.NotBlank;
import jakarta.validation.constraints.NotNull;
import jakarta.validation.constraints.Size;

import java.time.OffsetDateTime;
import java.util.UUID;

public record LeituraQrCodeDTO(
        @NotNull(message = "O identificador da leitura é obrigatório")
        UUID leituraId,

        @NotNull(message = "O identificador do participante é obrigatório")
        UUID participanteId,

        @NotNull(message = "O tipo de ação é obrigatório")
        TipoAcao tipoAcao,

        @NotBlank(message = "O identificador do dispositivo é obrigatório")
        @Size(max = 100, message = "O identificador do dispositivo deve ter no máximo 100 caracteres")
        String dispositivoId,

        @NotNull(message = "A data e hora da leitura no celular são obrigatórias")
        OffsetDateTime dataHoraLidaNoCelular
) {
}
