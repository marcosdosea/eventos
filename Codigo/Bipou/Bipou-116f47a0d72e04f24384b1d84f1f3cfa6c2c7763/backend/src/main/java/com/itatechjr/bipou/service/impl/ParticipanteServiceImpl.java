package com.itatechjr.bipou.service.impl;

import com.itatechjr.bipou.dto.ParticipanteRequestDTO;
import com.itatechjr.bipou.dto.ParticipanteResponseDTO;
import com.itatechjr.bipou.exception.CadastroIdReutilizadoException;
import com.itatechjr.bipou.exception.CpfJaCadastradoException;
import com.itatechjr.bipou.exception.ParticipanteNaoEncontradoException;
import com.itatechjr.bipou.mapper.ParticipanteMapper;
import com.itatechjr.bipou.model.entity.Participante;
import com.itatechjr.bipou.model.valueobject.Cpf;
import com.itatechjr.bipou.repository.ParticipanteRepository;
import com.itatechjr.bipou.service.ParticipanteService;
import lombok.RequiredArgsConstructor;
import org.springframework.dao.DataIntegrityViolationException;
import org.springframework.stereotype.Service;
import org.springframework.transaction.annotation.Transactional;

import java.util.List;
import java.util.HashSet;
import java.util.Set;

@Service
@RequiredArgsConstructor
public class ParticipanteServiceImpl implements ParticipanteService {

    private final ParticipanteRepository participanteRepository;
    private final ParticipanteMapper participanteMapper;
    private final ParticipanteCadastroTransactionalService cadastroTransactionalService;

    @Override
    public ParticipanteResponseDTO cadastrar(ParticipanteRequestDTO participanteDTO) {
        if (participanteDTO.cadastroId() != null) {
            Participante existente = cadastroTransactionalService
                    .buscarPorCadastroId(participanteDTO.cadastroId())
                    .orElse(null);
            if (existente != null) {
                return validarReenvioIdempotente(existente, participanteDTO);
            }
        }

        try {
            Participante participanteSalvo = cadastroTransactionalService.cadastrarNovo(participanteDTO);
            return participanteMapper.toResponse(participanteSalvo);
        } catch (DataIntegrityViolationException exception) {
            if (participanteDTO.cadastroId() != null) {
                Participante existente = cadastroTransactionalService
                        .buscarPorCadastroId(participanteDTO.cadastroId())
                        .orElse(null);
                if (existente != null) {
                    return validarReenvioIdempotente(existente, participanteDTO);
                }
            }
            if (foiViolacaoDeCpf(exception)) {
                throw new CpfJaCadastradoException(participanteDTO.cpf());
            }
            throw exception;
        }
    }

    @Override
    @Transactional
    public List<ParticipanteResponseDTO> cadastrarEmLote(List<ParticipanteRequestDTO> participantesDTO) {
        List<Participante> participantes = participantesDTO.stream()
                .map(participanteMapper::toEntity)
                .toList();
        Set<Cpf> cpfs = new HashSet<>();

        for (Participante participante : participantes) {
            Cpf cpf = participante.getCpf();
            if (cpf != null && !cpfs.add(cpf)) {
                throw new CpfJaCadastradoException(cpf.valor());
            }
        }

        if (!cpfs.isEmpty()) {
            List<Participante> cadastrados = participanteRepository.findAllByCpfIn(cpfs);
            if (!cadastrados.isEmpty()) {
                throw new CpfJaCadastradoException(cadastrados.getFirst().getCpf().valor());
            }
        }

        try {
            List<Participante> participantesSalvos = participanteRepository.saveAllAndFlush(participantes);
            return participantesSalvos.stream()
                    .map(participanteMapper::toResponse)
                    .toList();
        } catch (DataIntegrityViolationException exception) {
            if (foiViolacaoDeCpf(exception)) {
                throw new CpfJaCadastradoException();
            }
            throw exception;
        }
    }

    @Override
    @Transactional(readOnly = true)
    public ParticipanteResponseDTO buscarPorCpf(String cpfInformado) {
        Cpf cpf = Cpf.paraNovoCadastro(cpfInformado.strip());
        Participante participante = participanteRepository.findByCpf(cpf)
                .orElseThrow(() -> new ParticipanteNaoEncontradoException(cpf.valor()));
        return participanteMapper.toResponse(participante);
    }

    @Override
    @Transactional(readOnly = true)
    public List<ParticipanteResponseDTO> listar() {
        return participanteRepository.findAllByOrderByNomeAsc()
                .stream()
                .map(participanteMapper::toResponse)
                .toList();
    }

    private boolean foiViolacaoDeCpf(DataIntegrityViolationException exception) {
        String message = exception.getMostSpecificCause().getMessage();
        return message != null && message.contains("participante_cpf_key");
    }

    private ParticipanteResponseDTO validarReenvioIdempotente(
            Participante existente,
            ParticipanteRequestDTO request
    ) {
        String nome = request.nome().strip();
        String cpf = request.cpf().strip();
        if (!existente.getNome().equals(nome) || !existente.getCpf().valor().equals(cpf)) {
            throw new CadastroIdReutilizadoException();
        }
        return participanteMapper.toResponse(existente);
    }
}
