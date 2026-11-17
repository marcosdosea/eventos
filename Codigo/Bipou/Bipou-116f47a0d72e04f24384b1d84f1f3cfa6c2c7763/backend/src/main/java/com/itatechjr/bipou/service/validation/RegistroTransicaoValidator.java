package com.itatechjr.bipou.service.validation;

import com.itatechjr.bipou.exception.TransicaoAcaoInvalidaException;
import com.itatechjr.bipou.model.entity.Registro;
import com.itatechjr.bipou.model.enums.TipoAcao;
import org.springframework.stereotype.Component;

import java.util.Collection;
import java.util.Comparator;
import java.util.Objects;
import java.util.Set;

@Component
public class RegistroTransicaoValidator {

    public void validar(TipoAcao acaoAnterior, TipoAcao novaAcao) {
        Set<TipoAcao> acoesPermitidas = acoesPermitidasApos(acaoAnterior);

        if (!acoesPermitidas.contains(novaAcao)) {
            throw new TransicaoAcaoInvalidaException(acaoAnterior, novaAcao);
        }
    }

    public void validar(TipoAcao acaoAnterior, TipoAcao novaAcao, Long eventoId, Long subeventoId) {
        Set<TipoAcao> acoesPermitidas = acoesPermitidasApos(acaoAnterior);

        if (!acoesPermitidas.contains(novaAcao)) {
            throw new TransicaoAcaoInvalidaException(acaoAnterior, novaAcao, eventoId, subeventoId);
        }
    }

    public void validar(
            Long eventoId,
            Long subeventoId,
            Collection<Registro> registrosParticipante,
            TipoAcao novaAcao
    ) {
        TipoAcao acaoAnteriorNoContexto = registrosParticipante == null
                ? null
                : registrosParticipante.stream()
                        .filter(registro -> Objects.equals(registro.getEventoId(), eventoId)
                                && Objects.equals(registro.getSubeventoId(), subeventoId))
                        .max(Comparator.comparing(Registro::getDataHora))
                        .map(Registro::getTipoAcao)
                        .orElse(null);

        validar(acaoAnteriorNoContexto, novaAcao, eventoId, subeventoId);
    }

    private Set<TipoAcao> acoesPermitidasApos(TipoAcao acaoAnterior) {
        if (acaoAnterior == null) {
            return Set.of(TipoAcao.ENTRADA);
        }

        return switch (acaoAnterior) {
            case ENTRADA -> Set.of(TipoAcao.SAIDA);
            case SAIDA -> Set.of(TipoAcao.ENTRADA);
        };
    }
}
