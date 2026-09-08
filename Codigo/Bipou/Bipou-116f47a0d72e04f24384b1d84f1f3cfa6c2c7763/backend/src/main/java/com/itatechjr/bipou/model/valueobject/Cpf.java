package com.itatechjr.bipou.model.valueobject;

import java.io.Serializable;

public record Cpf(String valor) implements Serializable {

    private static final int TAMANHO = 11;

    public Cpf {
        if (valor == null || valor.length() != TAMANHO || !contemSomenteNumeros(valor)) {
            throw new IllegalArgumentException("O CPF deve conter exatamente 11 números");
        }
    }

    public static Cpf paraNovoCadastro(String valor) {
        Cpf cpf = new Cpf(valor);
        if (!temDigitosVerificadoresValidos(cpf.valor)) {
            throw new IllegalArgumentException("O CPF informado é inválido");
        }
        return cpf;
    }

    private static boolean contemSomenteNumeros(String valor) {
        for (int indice = 0; indice < valor.length(); indice++) {
            char caractere = valor.charAt(indice);
            if (caractere < '0' || caractere > '9') {
                return false;
            }
        }
        return true;
    }

    private static boolean temDigitosVerificadoresValidos(String valor) {
        boolean todosIguais = valor.chars().allMatch(digito -> digito == valor.charAt(0));
        if (todosIguais) {
            return false;
        }

        int primeiroDigito = calcularDigito(valor, 9, 10);
        int segundoDigito = calcularDigito(valor, 10, 11);
        return primeiroDigito == Character.digit(valor.charAt(9), 10)
                && segundoDigito == Character.digit(valor.charAt(10), 10);
    }

    private static int calcularDigito(String valor, int quantidade, int pesoInicial) {
        int soma = 0;
        for (int indice = 0; indice < quantidade; indice++) {
            soma += Character.digit(valor.charAt(indice), 10) * (pesoInicial - indice);
        }
        int resto = soma % 11;
        return resto < 2 ? 0 : 11 - resto;
    }
}
