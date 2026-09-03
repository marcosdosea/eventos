package com.itatechjr.bipou.exception;

public class CadastroIdReutilizadoException extends RuntimeException {

    public CadastroIdReutilizadoException() {
        super("O identificador do cadastro já foi utilizado com outros dados");
    }
}
