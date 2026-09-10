package com.itatechjr.bipou.dto;

import jakarta.validation.Valid;
import jakarta.validation.constraints.NotEmpty;
import jakarta.validation.constraints.Size;

import java.util.List;

public record ParticipantesLoteRequestDTO(
        @NotEmpty(message = "Informe ao menos um participante")
        @Size(max = 5000, message = "O lote deve conter no máximo 5000 participantes")
        List<@Valid ParticipanteRequestDTO> participantes
) {
}
