package com.itatechjr.bipou.exception;

import com.itatechjr.bipou.model.enums.TipoAcao;

public class TransicaoAcaoInvalidaException extends RuntimeException {

    public TransicaoAcaoInvalidaException(TipoAcao acaoAnterior, TipoAcao novaAcao) {
        super("Não é permitido registrar " + novaAcao + " após " + descricaoAcaoAnterior(acaoAnterior));
    }

    public TransicaoAcaoInvalidaException(TipoAcao acaoAnterior, TipoAcao novaAcao, Long eventoId, Long subeventoId) {
        super("Não é permitido registrar " + novaAcao + " após " + descricaoAcaoAnterior(acaoAnterior)
                + " no contexto " + descricaoContexto(eventoId, subeventoId));
    }

    private static String descricaoAcaoAnterior(TipoAcao acaoAnterior) {
        return acaoAnterior == null ? "nenhuma ação" : acaoAnterior.name();
    }

    private static String descricaoContexto(Long eventoId, Long subeventoId) {
        if (subeventoId == null) {
            return "evento " + eventoId + " (geral)";
        }
        return "evento " + eventoId + ", subevento " + subeventoId;
    }
}
