package com.itatechjr.bipou.model.valueobject;

import org.junit.jupiter.api.Test;

import static org.assertj.core.api.Assertions.assertThat;
import static org.assertj.core.api.Assertions.assertThatThrownBy;

class CpfTests {

    @Test
    void deveCriarCpfComDigitosVerificadoresValidos() {
        Cpf cpf = Cpf.paraNovoCadastro("52998224725");

        assertThat(cpf.valor()).isEqualTo("52998224725");
    }

    @Test
    void deveRejeitarCpfComQuantidadeIncorreta() {
        assertThatThrownBy(() -> new Cpf("1234567890"))
                .isInstanceOf(IllegalArgumentException.class)
                .hasMessage("O CPF deve conter exatamente 11 números");
    }

    @Test
    void deveRejeitarCpfComCaracteresNaoNumericos() {
        assertThatThrownBy(() -> new Cpf("1234567890A"))
                .isInstanceOf(IllegalArgumentException.class)
                .hasMessage("O CPF deve conter exatamente 11 números");
    }

    @Test
    void deveRejeitarCpfComDigitosVerificadoresInvalidos() {
        assertThatThrownBy(() -> Cpf.paraNovoCadastro("12345678900"))
                .isInstanceOf(IllegalArgumentException.class)
                .hasMessage("O CPF informado é inválido");
    }

    @Test
    void deveRejeitarCpfComTodosOsDigitosIguais() {
        assertThatThrownBy(() -> Cpf.paraNovoCadastro("11111111111"))
                .isInstanceOf(IllegalArgumentException.class)
                .hasMessage("O CPF informado é inválido");
    }
}
