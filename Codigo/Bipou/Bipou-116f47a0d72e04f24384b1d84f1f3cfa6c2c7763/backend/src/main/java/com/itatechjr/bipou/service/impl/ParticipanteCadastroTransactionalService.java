package com.itatechjr.bipou.service.impl;

import com.itatechjr.bipou.dto.ParticipanteRequestDTO;
import com.itatechjr.bipou.exception.CpfJaCadastradoException;
import com.itatechjr.bipou.mapper.ParticipanteMapper;
import com.itatechjr.bipou.model.entity.Participante;
import com.itatechjr.bipou.model.valueobject.Cpf;
import com.itatechjr.bipou.repository.ParticipanteRepository;
import lombok.RequiredArgsConstructor;
import org.springframework.stereotype.Service;
import org.springframework.transaction.annotation.Transactional;

import java.util.Optional;
import java.util.UUID;

@Service
@RequiredArgsConstructor
public class ParticipanteCadastroTransactionalService {

    private final ParticipanteRepository participanteRepository;
    private final ParticipanteMapper participanteMapper;

    @Transactional
    public Participante cadastrarNovo(ParticipanteRequestDTO participanteDTO) {
        Cpf cpf = Cpf.paraNovoCadastro(participanteDTO.cpf().strip());
        if (participanteRepository.existsByCpf(cpf)) {
            throw new CpfJaCadastradoException(cpf.valor());
        }

        return participanteRepository.saveAndFlush(participanteMapper.toEntity(participanteDTO));
    }

    @Transactional(readOnly = true)
    public Optional<Participante> buscarPorCadastroId(UUID cadastroId) {
        return participanteRepository.findByCadastroId(cadastroId);
    }
}
