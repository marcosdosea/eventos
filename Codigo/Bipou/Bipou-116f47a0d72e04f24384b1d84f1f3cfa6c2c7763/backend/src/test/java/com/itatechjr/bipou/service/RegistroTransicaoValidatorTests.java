package com.itatechjr.bipou.service;

import com.itatechjr.bipou.exception.TransicaoAcaoInvalidaException;
import com.itatechjr.bipou.model.enums.TipoAcao;
import com.itatechjr.bipou.service.validation.RegistroTransicaoValidator;
import org.junit.jupiter.api.Test;
import org.junit.jupiter.params.ParameterizedTest;
import org.junit.jupiter.params.provider.Arguments;
import org.junit.jupiter.params.provider.MethodSource;

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
    void deveEncerrarFluxoDepoisDaSaida() {
        for (TipoAcao novaAcao : TipoAcao.values()) {
            assertThatThrownBy(() -> validator.validar(TipoAcao.SAIDA, novaAcao))
                    .isInstanceOf(TransicaoAcaoInvalidaException.class);
        }
    }

    private static Stream<Arguments> transicoesPermitidas() {
        return Stream.of(
                Arguments.of(null, TipoAcao.ENTRADA),
                Arguments.of(TipoAcao.ENTRADA, TipoAcao.SAIDA)
        );
    }

    private static Stream<Arguments> transicoesProibidas() {
        return Stream.of(
                Arguments.of(null, TipoAcao.SAIDA),
                Arguments.of(TipoAcao.ENTRADA, TipoAcao.ENTRADA)
        );
    }
}
