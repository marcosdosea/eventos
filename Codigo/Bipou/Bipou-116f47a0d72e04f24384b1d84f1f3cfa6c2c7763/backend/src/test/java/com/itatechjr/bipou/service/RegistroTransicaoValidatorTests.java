package com.itatechjr.bipou.service;

import com.itatechjr.bipou.exception.TransicaoAcaoInvalidaException;
import com.itatechjr.bipou.model.entity.Registro;
import com.itatechjr.bipou.model.enums.TipoAcao;
import com.itatechjr.bipou.service.validation.RegistroTransicaoValidator;
import org.junit.jupiter.api.Test;
import org.junit.jupiter.params.ParameterizedTest;
import org.junit.jupiter.params.provider.Arguments;
import org.junit.jupiter.params.provider.MethodSource;

import java.time.OffsetDateTime;
import java.time.ZoneOffset;
import java.util.List;
import java.util.UUID;
import java.util.stream.Stream;

import static org.assertj.core.api.Assertions.assertThatCode;
import static org.assertj.core.api.Assertions.assertThatThrownBy;

class RegistroTransicaoValidatorTests {

    private final RegistroTransicaoValidator validator = new RegistroTransicaoValidator();

    @ParameterizedTest
    @MethodSource("transicoesPermitidas")
    void deveAceitarTransicoesPermitidas(TipoAcao anterior, TipoAcao novaAcao) {
        assertThatCode(() -> validator.validar(anterior, novaAcao)).doesNotThrowAnyException();
    }

    @ParameterizedTest
    @MethodSource("transicoesProibidas")
    void deveRejeitarTransicoesProibidas(TipoAcao anterior, TipoAcao novaAcao) {
        assertThatThrownBy(() -> validator.validar(anterior, novaAcao))
                .isInstanceOf(TransicaoAcaoInvalidaException.class);
    }

    @Test
    void deveCalcularAlternanciaDeFormaIndependentePorContextoDeSubevento() {
        OffsetDateTime base = OffsetDateTime.of(2026, 11, 16, 9, 0, 0, 0, ZoneOffset.UTC);
        List<Registro> historico = List.of(
                registro(15L, null, TipoAcao.ENTRADA, base),
                registro(15L, 42L, TipoAcao.ENTRADA, base.plusMinutes(5)),
                registro(15L, 42L, TipoAcao.SAIDA, base.plusMinutes(60))
        );

        // Participante segue "Dentro" do evento geral (15, null): permite SAIDA no evento geral, rejeita ENTRADA duplicada
        assertThatCode(() -> validator.validar(15L, null, historico, TipoAcao.SAIDA))
                .doesNotThrowAnyException();
        assertThatThrownBy(() -> validator.validar(15L, null, historico, TipoAcao.ENTRADA))
                .isInstanceOf(TransicaoAcaoInvalidaException.class);

        // Participante saiu do subevento 42: pode entrar novamente no subevento 42 (alternancia) e entrar em outro subevento (43)
        assertThatCode(() -> validator.validar(15L, 42L, historico, TipoAcao.ENTRADA))
                .doesNotThrowAnyException();
        assertThatCode(() -> validator.validar(15L, 43L, historico, TipoAcao.ENTRADA))
                .doesNotThrowAnyException();
    }

    @Test
    void devePermitirEntradaSimultaneaNoEventoGeralENoSubevento() {
        OffsetDateTime base = OffsetDateTime.of(2026, 11, 16, 9, 0, 0, 0, ZoneOffset.UTC);
        List<Registro> historico = List.of(
                registro(15L, null, TipoAcao.ENTRADA, base)
        );

        assertThatCode(() -> validator.validar(15L, 42L, historico, TipoAcao.ENTRADA))
                .doesNotThrowAnyException();
    }

    private static Stream<Arguments> transicoesPermitidas() {
        return Stream.of(
                Arguments.of(null, TipoAcao.ENTRADA),
                Arguments.of(TipoAcao.ENTRADA, TipoAcao.SAIDA),
                Arguments.of(TipoAcao.SAIDA, TipoAcao.ENTRADA)
        );
    }

    private static Stream<Arguments> transicoesProibidas() {
        return Stream.of(
                Arguments.of(null, TipoAcao.SAIDA),
                Arguments.of(TipoAcao.ENTRADA, TipoAcao.ENTRADA),
                Arguments.of(TipoAcao.SAIDA, TipoAcao.SAIDA)
        );
    }

    private Registro registro(Long eventoId, Long subeventoId, TipoAcao tipoAcao, OffsetDateTime dataHora) {
        return Registro.builder()
                .id(UUID.randomUUID())
                .leituraId(UUID.randomUUID())
                .eventoId(eventoId)
                .subeventoId(subeventoId)
                .tipoAcao(tipoAcao)
                .dataHora(dataHora)
                .dataHoraLidaNoCelular(dataHora)
                .dispositivoId("celular-01")
                .build();
    }
}
