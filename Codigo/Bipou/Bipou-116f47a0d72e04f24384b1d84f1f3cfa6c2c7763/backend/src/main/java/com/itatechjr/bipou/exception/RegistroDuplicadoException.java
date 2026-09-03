package com.itatechjr.bipou.exception;

public class RegistroDuplicadoException extends RuntimeException {

    public RegistroDuplicadoException() {
        super("A mesma ação já foi registrada para o participante nos últimos 10 segundos");
    }
}
