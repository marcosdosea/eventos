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
                UUID.randomUUID()
        );

        assertThat(validator.validate(participante)).isEmpty();
    }

    @Test
    void deveRejeitarParticipanteComNomeVazioECpfInvalido() {
        ParticipanteRequestDTO participante = new ParticipanteRequestDTO(" ", "123");

        assertThat(validator.validate(participante))
                .extracting(violacao -> violacao.getPropertyPath().toString())
                .contains("nome", "cpf");
    }

    @Test
    void deveRejeitarParticipanteSemCpf() {
        ParticipanteRequestDTO participante = new ParticipanteRequestDTO(
                "Participante Teste",
                null
        );

        assertThat(validator.validate(participante))
                .extracting(violacao -> violacao.getPropertyPath().toString())
                .contains("cpf");
    }

    @Test
    void deveValidarCadaParticipanteDoLote() {
        ParticipantesLoteRequestDTO lote = new ParticipantesLoteRequestDTO(List.of(
                new ParticipanteRequestDTO("Participante válido", "52998224725"),
                new ParticipanteRequestDTO(" ", "123")
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
                TipoAcao.ENTRADA,
                "celular-01",
                OffsetDateTime.of(2026, 8, 22, 15, 0, 0, 0, ZoneOffset.ofHours(-3))
        );

        assertThat(validator.validate(leitura)).isEmpty();
        assertThat(leitura.dataHoraLidaNoCelular().getOffset()).isEqualTo(ZoneOffset.ofHours(-3));
    }

    @Test
    void deveRejeitarLeituraQrCodeSemCamposObrigatorios() {
        LeituraQrCodeDTO leitura = new LeituraQrCodeDTO(null, null, null, " ", null);

        assertThat(validator.validate(leitura))
                .extracting(violacao -> violacao.getPropertyPath().toString())
                .containsExactlyInAnyOrder(
                        "leituraId",
                        "participanteId",
                        "tipoAcao",
                        "dispositivoId",
                        "dataHoraLidaNoCelular"
                );
    }
}
