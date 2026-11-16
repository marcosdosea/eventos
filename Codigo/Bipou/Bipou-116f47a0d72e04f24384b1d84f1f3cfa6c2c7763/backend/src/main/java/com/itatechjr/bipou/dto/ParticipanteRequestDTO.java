package com.itatechjr.bipou.dto;

import jakarta.validation.constraints.NotBlank;
import jakarta.validation.constraints.NotNull;
import jakarta.validation.constraints.Pattern;
import jakarta.validation.constraints.Positive;
import jakarta.validation.constraints.Size;

import java.util.ArrayList;
import java.util.Collections;
import java.util.List;
import java.util.UUID;

public record ParticipanteRequestDTO(
        @NotBlank(message = "O nome é obrigatório")
        @Size(max = 150, message = "O nome deve ter no máximo 150 caracteres")
        String nome,

        @NotBlank(message = "O CPF é obrigatório")
        @Pattern(regexp = "\\d{11}", message = "O CPF deve conter exatamente 11 dígitos")
        String cpf,

        UUID cadastroId,

        @Positive(message = "O identificador da pessoa no Boralá deve ser positivo")
        Long idPessoaBorala,

        @NotNull(message = "O identificador do evento é obrigatório")
        @Positive(message = "O identificador do evento deve ser positivo")
        Long eventoId,

        List<@NotNull(message = "O identificador do subevento não pode ser nulo")
                @Positive(message = "O identificador do subevento deve ser positivo") Long> subeventoIds
) {

    public ParticipanteRequestDTO {
        subeventoIds = subeventoIds == null
                ? List.of()
                : Collections.unmodifiableList(new ArrayList<>(subeventoIds));
    }

    public ParticipanteRequestDTO(String nome, String cpf, Long eventoId) {
        this(nome, cpf, null, null, eventoId, List.of());
    }

    public ParticipanteRequestDTO(String nome, String cpf, UUID cadastroId, Long eventoId) {
        this(nome, cpf, cadastroId, null, eventoId, List.of());
    }
}
