package com.itatechjr.bipou.exception;

import java.util.UUID;

public class IdempotenciaConflitanteException extends RuntimeException {

    public IdempotenciaConflitanteException(UUID leituraId) {
        super("O identificador de leitura " + leituraId + " já foi utilizado com outros dados");
    }
}
