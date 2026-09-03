package com.itatechjr.bipou.dto;

import jakarta.validation.constraints.NotBlank;
import jakarta.validation.constraints.Pattern;
import jakarta.validation.constraints.Size;

import java.util.UUID;

public record ParticipanteRequestDTO(
        @NotBlank(message = "O nome é obrigatório")
        @Size(max = 150, message = "O nome deve ter no máximo 150 caracteres")
        String nome,

        @NotBlank(message = "O CPF é obrigatório")
        @Pattern(regexp = "\\d{11}", message = "O CPF deve conter exatamente 11 dígitos")
        String cpf,

        UUID cadastroId
) {

    public ParticipanteRequestDTO(String nome, String cpf) {
        this(nome, cpf, null);
    }
}
