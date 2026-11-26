package com.itatechjr.bipou.mapper;

import com.itatechjr.bipou.dto.ParticipanteRequestDTO;
import com.itatechjr.bipou.dto.ParticipanteResponseDTO;
import com.itatechjr.bipou.model.entity.Participante;
import com.itatechjr.bipou.model.valueobject.Cpf;
import org.springframework.stereotype.Component;

import java.util.LinkedHashSet;
import java.util.List;
import java.util.Set;

@Component
public class ParticipanteMapper {

    public Participante toEntity(ParticipanteRequestDTO dto) {
        Set<Long> subeventoIds = dto.subeventoIds() == null
                ? new LinkedHashSet<>()
                : new LinkedHashSet<>(dto.subeventoIds());

        return Participante.builder()
                .nome(dto.nome().strip())
                .cpf(Cpf.paraNovoCadastro(dto.cpf().strip()))
                .email(dto.email())
                .cadastroId(dto.cadastroId())
                .idPessoaBorala(dto.idPessoaBorala())
                .eventoId(dto.eventoId())
                .subeventoIds(subeventoIds)
                .build();
    }

    public ParticipanteResponseDTO toResponse(Participante participante) {
        List<Long> subeventoIds = participante.getSubeventoIds() == null
                ? List.of()
                : participante.getSubeventoIds().stream().sorted().toList();

        return new ParticipanteResponseDTO(
                participante.getId(),
                participante.getNome(),
                participante.getCpf().valor(),
                participante.getIdPessoaBorala(),
                participante.getEventoId(),
                subeventoIds,
                participante.getEmail()
        );
    }
}
