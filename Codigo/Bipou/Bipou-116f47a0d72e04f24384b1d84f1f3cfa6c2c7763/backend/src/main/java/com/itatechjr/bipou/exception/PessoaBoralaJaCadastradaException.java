package com.itatechjr.bipou.exception;

public class PessoaBoralaJaCadastradaException extends RuntimeException {

    public PessoaBoralaJaCadastradaException(Long idPessoaBorala, Long eventoId) {
        super("Já existe um participante cadastrado no evento " + eventoId
                + " para a pessoa do Boralá " + idPessoaBorala);
    }

    public PessoaBoralaJaCadastradaException() {
        super("Já existe um participante cadastrado para um dos identificadores do Boralá informados no lote");
    }
}
