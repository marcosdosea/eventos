package com.itatechjr.bipou.dto;

import jakarta.validation.constraints.NotBlank;
import jakarta.validation.constraints.Pattern;
import jakarta.validation.constraints.Positive;

public record ConsultaParticipanteCpfDTO(
        @NotBlank(message = "O CPF é obrigatório")
        @Pattern(regexp = "\\d{11}", message = "O CPF deve conter exatamente 11 dígitos")
        String cpf,

        @Positive(message = "O identificador do evento deve ser positivo")
        Long eventoId
) {
    public ConsultaParticipanteCpfDTO(String cpf) {
        this(cpf, null);
    }
}
