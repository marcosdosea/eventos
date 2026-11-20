package com.itatechjr.bipou.dto;

import com.itatechjr.bipou.model.enums.TipoAcao;
import jakarta.validation.Validation;
import jakarta.validation.Validator;
import org.junit.jupiter.api.BeforeAll;
import org.junit.jupiter.api.Test;

import java.time.OffsetDateTime;
import java.time.ZoneOffset;
import java.util.UUID;
import java.util.List;

import static org.assertj.core.api.Assertions.assertThat;

class DtoValidationTests {

    private static Validator validator;

    @BeforeAll
    static void configurarValidator() {
        validator = Validation.buildDefaultValidatorFactory().getValidator();
    }

    @Test
    void deveAceitarParticipanteValido() {
        ParticipanteRequestDTO participante = new ParticipanteRequestDTO(
                "Participante Teste",
                "52998224725",
                UUID.randomUUID(),
                1042L,
                15L,
                List.of(42L, 43L)
        );

        assertThat(validator.validate(participante)).isEmpty();
    }

    @Test
    void deveRejeitarParticipanteComNomeVazioECpfInvalidoESemEvento() {
        ParticipanteRequestDTO participante = new ParticipanteRequestDTO(" ", "123", null);

        assertThat(validator.validate(participante))
                .extracting(violacao -> violacao.getPropertyPath().toString())
                .contains("nome", "cpf", "eventoId");
    }

    @Test
    void deveRejeitarParticipanteSemCpf() {
        ParticipanteRequestDTO participante = new ParticipanteRequestDTO(
                "Participante Teste",
                null,
                15L
        );

        assertThat(validator.validate(participante))
                .extracting(violacao -> violacao.getPropertyPath().toString())
                .contains("cpf");
    }

    @Test
    void deveRejeitarParticipanteComEventoESubeventoNaoPositivos() {
        ParticipanteRequestDTO participante = new ParticipanteRequestDTO(
                "Participante Teste",
                "52998224725",
                null,
                0L,
                0L,
                List.of(-1L)
        );

        assertThat(validator.validate(participante))
                .extracting(violacao -> violacao.getPropertyPath().toString())
                .contains("idPessoaBorala", "eventoId", "subeventoIds[0].<list element>");
    }

    @Test
    void deveRejeitarParticipanteComSubeventoNuloNaLista() {
        ParticipanteRequestDTO participante = new ParticipanteRequestDTO(
                "Participante Teste",
                "52998224725",
                null,
                1042L,
                15L,
                java.util.Arrays.asList((Long) null)
        );

        assertThat(validator.validate(participante))
                .extracting(violacao -> violacao.getPropertyPath().toString())
                .contains("subeventoIds[0].<list element>");
    }

    @Test
    void deveValidarCadaParticipanteDoLote() {
        ParticipantesLoteRequestDTO lote = new ParticipantesLoteRequestDTO(List.of(
                new ParticipanteRequestDTO("Participante válido", "52998224725", 15L),
                new ParticipanteRequestDTO(" ", "123", 15L)
        ));

        assertThat(validator.validate(lote))
                .extracting(violacao -> violacao.getPropertyPath().toString())
                .contains("participantes[1].nome", "participantes[1].cpf");
    }

    @Test
    void deveAceitarLeituraQrCodeValidaComOffsetEIdempotencia() {
        LeituraQrCodeDTO leitura = new LeituraQrCodeDTO(
                UUID.randomUUID(),
                UUID.randomUUID(),
                15L,
                42L,
                TipoAcao.ENTRADA,
                "celular-01",
                "operador.portaria",
                OffsetDateTime.of(2026, 8, 22, 15, 0, 0, 0, ZoneOffset.ofHours(-3))
        );

        assertThat(validator.validate(leitura)).isEmpty();
        assertThat(leitura.dataHoraLidaNoCelular().getOffset()).isEqualTo(ZoneOffset.ofHours(-3));
    }

    @Test
    void deveRejeitarLeituraQrCodeSemCamposObrigatorios() {
        LeituraQrCodeDTO leitura = new LeituraQrCodeDTO(null, null, null, null, null, " ", null, null);

        assertThat(validator.validate(leitura))
                .extracting(violacao -> violacao.getPropertyPath().toString())
                .containsExactlyInAnyOrder(
                        "leituraId",
                        "participanteId",
                        "eventoId",
                        "tipoAcao",
                        "dispositivoId",
                        "dataHoraLidaNoCelular"
                );
    }
}
