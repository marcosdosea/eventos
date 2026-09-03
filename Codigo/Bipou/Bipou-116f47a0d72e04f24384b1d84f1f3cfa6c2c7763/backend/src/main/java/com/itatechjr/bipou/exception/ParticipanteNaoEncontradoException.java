package com.itatechjr.bipou.exception;

import java.util.UUID;

public class ParticipanteNaoEncontradoException extends RuntimeException {

    public ParticipanteNaoEncontradoException(UUID participanteId) {
        super("Participante não encontrado para o identificador " + participanteId);
    }

    public ParticipanteNaoEncontradoException(String cpf) {
        super("Participante não encontrado para o CPF " + cpf);
    }
}
