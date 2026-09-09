package com.itatechjr.bipou.service.validation;

import com.itatechjr.bipou.exception.TransicaoAcaoInvalidaException;
import com.itatechjr.bipou.model.enums.TipoAcao;
import org.springframework.stereotype.Component;

import java.util.Set;

@Component
public class RegistroTransicaoValidator {

    public void validar(TipoAcao acaoAnterior, TipoAcao novaAcao) {
        Set<TipoAcao> acoesPermitidas = acoesPermitidasApos(acaoAnterior);

        if (!acoesPermitidas.contains(novaAcao)) {
            throw new TransicaoAcaoInvalidaException(acaoAnterior, novaAcao);
        }
    }

    private Set<TipoAcao> acoesPermitidasApos(TipoAcao acaoAnterior) {
        if (acaoAnterior == null) {
            return Set.of(TipoAcao.ENTRADA);
        }

        return switch (acaoAnterior) {
            case ENTRADA -> Set.of(TipoAcao.SAIDA);
            case SAIDA -> Set.of();
        };
    }
}
