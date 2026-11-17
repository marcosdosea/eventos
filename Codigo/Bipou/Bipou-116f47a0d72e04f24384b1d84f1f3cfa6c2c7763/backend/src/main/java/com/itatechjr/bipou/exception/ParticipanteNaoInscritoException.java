package com.itatechjr.bipou.exception;

import java.util.UUID;

public class ParticipanteNaoInscritoException extends RuntimeException {

    public ParticipanteNaoInscritoException(UUID participanteId, Long eventoId, Long subeventoId) {
        super(criarMensagem(participanteId, eventoId, subeventoId));
    }

    private static String criarMensagem(UUID participanteId, Long eventoId, Long subeventoId) {
        if (subeventoId == null) {
            return "Participante " + participanteId + " não está vinculado ao evento " + eventoId;
        }
        return "Participante " + participanteId + " não está inscrito no subevento " + subeventoId
                + " do evento " + eventoId;
    }
}
