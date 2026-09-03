package com.itatechjr.bipou.exception;

import com.itatechjr.bipou.model.enums.TipoAcao;

public class TransicaoAcaoInvalidaException extends RuntimeException {

    public TransicaoAcaoInvalidaException(TipoAcao acaoAnterior, TipoAcao novaAcao) {
        super("Não é permitido registrar " + novaAcao + " após " + descricaoAcaoAnterior(acaoAnterior));
    }

    private static String descricaoAcaoAnterior(TipoAcao acaoAnterior) {
        return acaoAnterior == null ? "nenhuma ação" : acaoAnterior.name();
    }
}
