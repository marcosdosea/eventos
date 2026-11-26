package com.itatechjr.bipou.service.impl;

import com.itatechjr.bipou.dto.ParticipanteRequestDTO;
import com.itatechjr.bipou.dto.ParticipanteResponseDTO;
import com.itatechjr.bipou.exception.CadastroIdReutilizadoException;
import com.itatechjr.bipou.exception.CpfJaCadastradoException;
import com.itatechjr.bipou.exception.ParticipanteNaoEncontradoException;
import com.itatechjr.bipou.exception.PessoaBoralaJaCadastradaException;
import com.itatechjr.bipou.mapper.ParticipanteMapper;
import com.itatechjr.bipou.model.entity.Participante;
import com.itatechjr.bipou.model.valueobject.Cpf;
import com.itatechjr.bipou.repository.ParticipanteRepository;
import com.itatechjr.bipou.service.ParticipanteService;
import lombok.RequiredArgsConstructor;
import org.springframework.dao.DataIntegrityViolationException;
import org.springframework.stereotype.Service;
import org.springframework.transaction.annotation.Transactional;

import java.util.HashMap;
import java.util.HashSet;
import java.util.List;
import java.util.Map;
import java.util.Objects;
import java.util.Optional;
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
            if (foiViolacaoDePessoaBorala(exception)) {
                throw new PessoaBoralaJaCadastradaException(
                        participanteDTO.idPessoaBorala(),
                        participanteDTO.eventoId()
                );
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

        Map<Long, Set<Cpf>> cpfsPorEvento = new HashMap<>();
        Map<Long, Set<Long>> pessoasPorEvento = new HashMap<>();

        for (Participante participante : participantes) {
            Long eventoId = participante.getEventoId();
            Cpf cpf = participante.getCpf();
            if (cpf != null && !cpfsPorEvento.computeIfAbsent(eventoId, ignored -> new HashSet<>()).add(cpf)) {
                throw new CpfJaCadastradoException(cpf.valor());
            }

            Long idPessoaBorala = participante.getIdPessoaBorala();
            if (idPessoaBorala != null
                    && !pessoasPorEvento.computeIfAbsent(eventoId, ignored -> new HashSet<>()).add(idPessoaBorala)) {
                throw new PessoaBoralaJaCadastradaException(idPessoaBorala, eventoId);
            }
        }

        for (Map.Entry<Long, Set<Cpf>> entry : cpfsPorEvento.entrySet()) {
            List<Participante> cadastrados = participanteRepository
                    .findAllByEventoIdAndCpfIn(entry.getKey(), entry.getValue());
            if (!cadastrados.isEmpty()) {
                throw new CpfJaCadastradoException(cadastrados.getFirst().getCpf().valor());
            }
        }

        for (Map.Entry<Long, Set<Long>> entry : pessoasPorEvento.entrySet()) {
            List<Participante> cadastrados = participanteRepository
                    .findAllByEventoIdAndIdPessoaBoralaIn(entry.getKey(), entry.getValue());
            if (!cadastrados.isEmpty()) {
                throw new PessoaBoralaJaCadastradaException(
                        cadastrados.getFirst().getIdPessoaBorala(),
                        entry.getKey()
                );
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
            if (foiViolacaoDePessoaBorala(exception)) {
                throw new PessoaBoralaJaCadastradaException();
            }
            throw exception;
        }
    }

    @Override
    @Transactional(readOnly = true)
    public ParticipanteResponseDTO buscarPorCpf(String cpfInformado) {
        return buscarPorCpf(cpfInformado, null);
    }

    @Override
    @Transactional(readOnly = true)
    public ParticipanteResponseDTO buscarPorCpf(String cpfInformado, Long eventoId) {
        Cpf cpf = Cpf.paraNovoCadastro(cpfInformado.strip());
        Optional<Participante> participante = eventoId == null
                ? participanteRepository.findFirstByCpfOrderByEventoIdAsc(cpf)
                : participanteRepository.findByEventoIdAndCpf(eventoId, cpf);

        return participante
                .map(participanteMapper::toResponse)
                .orElseThrow(() -> new ParticipanteNaoEncontradoException(cpf.valor()));
    }

    @Override
    @Transactional(readOnly = true)
    public List<ParticipanteResponseDTO> listar() {
        return listar(null, null);
    }

    @Override
    @Transactional(readOnly = true)
    public List<ParticipanteResponseDTO> listar(Long eventoId, Long subeventoId) {
        List<Participante> participantes;
        if (eventoId != null && subeventoId != null) {
            participantes = participanteRepository
                    .findAllByEventoIdAndSubeventoIdOrderByNomeAsc(eventoId, subeventoId);
        } else if (eventoId != null) {
            participantes = participanteRepository.findAllByEventoIdOrderByNomeAsc(eventoId);
        } else if (subeventoId != null) {
            participantes = participanteRepository.findAllBySubeventoIdOrderByNomeAsc(subeventoId);
        } else {
            participantes = participanteRepository.findAllByOrderByNomeAsc();
        }

        return participantes.stream()
                .map(participanteMapper::toResponse)
                .toList();
    }

    private boolean foiViolacaoDeCpf(DataIntegrityViolationException exception) {
        String message = exception.getMostSpecificCause().getMessage();
        return message != null
                && (message.contains("uk_participante_evento_cpf") || message.contains("participante_cpf_key"));
    }

    private boolean foiViolacaoDePessoaBorala(DataIntegrityViolationException exception) {
        String message = exception.getMostSpecificCause().getMessage();
        return message != null && message.contains("uk_participante_evento_pessoa_borala");
    }

    private ParticipanteResponseDTO validarReenvioIdempotente(
            Participante existente,
            ParticipanteRequestDTO request
    ) {
        String nome = request.nome().strip();
        String cpf = request.cpf().strip();
        Set<Long> subeventosExistentes = existente.getSubeventoIds() == null
                ? Set.of()
                : existente.getSubeventoIds();
        Set<Long> subeventosRequest = new HashSet<>(request.subeventoIds());

        if (!existente.getNome().equals(nome)
                || !existente.getCpf().valor().equals(cpf)
                || !Objects.equals(existente.getEmail(), request.email())
                || !Objects.equals(existente.getEventoId(), request.eventoId())
                || !Objects.equals(existente.getIdPessoaBorala(), request.idPessoaBorala())
                || !subeventosExistentes.equals(subeventosRequest)) {
            throw new CadastroIdReutilizadoException();
        }
        return participanteMapper.toResponse(existente);
    }
}
