package com.itatechjr.bipou.mapper;

import com.itatechjr.bipou.dto.ParticipanteRequestDTO;
import com.itatechjr.bipou.dto.ParticipanteResponseDTO;
import com.itatechjr.bipou.model.entity.Participante;
import com.itatechjr.bipou.model.valueobject.Cpf;
import org.springframework.stereotype.Component;

@Component
public class ParticipanteMapper {

    public Participante toEntity(ParticipanteRequestDTO dto) {
        return Participante.builder()
                .nome(dto.nome().strip())
                .cpf(Cpf.paraNovoCadastro(dto.cpf().strip()))
                .cadastroId(dto.cadastroId())
                .build();
    }

    public ParticipanteResponseDTO toResponse(Participante participante) {
        return new ParticipanteResponseDTO(
                participante.getId(),
                participante.getNome(),
                participante.getCpf().valor()
        );
    }
}
