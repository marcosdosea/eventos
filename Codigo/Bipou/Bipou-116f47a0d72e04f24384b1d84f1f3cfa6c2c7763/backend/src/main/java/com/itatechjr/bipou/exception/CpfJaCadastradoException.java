package com.itatechjr.bipou.exception;

public class CpfJaCadastradoException extends RuntimeException {

    public CpfJaCadastradoException(String cpf) {
        super("Já existe um participante cadastrado com o CPF " + cpf);
    }

    public CpfJaCadastradoException() {
        super("Já existe um participante cadastrado com um dos CPFs informados no lote");
    }
}
