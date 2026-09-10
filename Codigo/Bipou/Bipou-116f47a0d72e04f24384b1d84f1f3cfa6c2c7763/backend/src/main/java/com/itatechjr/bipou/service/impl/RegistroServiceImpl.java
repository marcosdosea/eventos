package com.itatechjr.bipou.service.impl;

import com.itatechjr.bipou.dto.LeituraQrCodeDTO;
import com.itatechjr.bipou.dto.RegistroResultadoDTO;
import com.itatechjr.bipou.exception.IdempotenciaConflitanteException;
import com.itatechjr.bipou.exception.ParticipanteNaoEncontradoException;
import com.itatechjr.bipou.exception.RegistroDuplicadoException;
import com.itatechjr.bipou.mapper.RegistroMapper;
import com.itatechjr.bipou.model.entity.Participante;
import com.itatechjr.bipou.model.entity.Registro;
import com.itatechjr.bipou.repository.ParticipanteRepository;
import com.itatechjr.bipou.repository.RegistroRepository;
import com.itatechjr.bipou.service.RegistroService;
import com.itatechjr.bipou.service.validation.RegistroTransicaoValidator;
import lombok.RequiredArgsConstructor;
import org.springframework.dao.DataIntegrityViolationException;
import org.springframework.stereotype.Service;
import org.springframework.transaction.annotation.Transactional;

import java.time.Clock;
import java.time.Duration;
import java.time.OffsetDateTime;
import java.time.ZoneOffset;
import java.time.temporal.ChronoUnit;
import java.util.Optional;

@Service
@RequiredArgsConstructor
public class RegistroServiceImpl implements RegistroService {

    private static final Duration INTERVALO_DUPLICIDADE = Duration.ofSeconds(10);

    private final ParticipanteRepository participanteRepository;
    private final RegistroRepository registroRepository;
    private final RegistroMapper registroMapper;
    private final RegistroTransicaoValidator transicaoValidator;
    private final Clock clock;

    @Override
    @Transactional
    public RegistroResultadoDTO registrar(LeituraQrCodeDTO leitura) {
        Optional<Registro> registroExistente = registroRepository.findByLeituraId(leitura.leituraId());
        if (registroExistente.isPresent()) {
            return resultadoIdempotente(registroExistente.get(), leitura);
        }

        Participante participante = participanteRepository.findByIdForUpdate(leitura.participanteId())
                .orElseThrow(() -> new ParticipanteNaoEncontradoException(leitura.participanteId()));

        registroExistente = registroRepository.findByLeituraId(leitura.leituraId());
        if (registroExistente.isPresent()) {
            return resultadoIdempotente(registroExistente.get(), leitura);
        }

        OffsetDateTime dataHoraServidor = OffsetDateTime.now(clock).withOffsetSameInstant(ZoneOffset.UTC);
        Optional<Registro> ultimoRegistro = registroRepository
                .findTopByParticipanteIdOrderByDataHoraDesc(participante.getId());

        validarDuplicidade(ultimoRegistro, leitura, dataHoraServidor);
        transicaoValidator.validar(
                ultimoRegistro.map(Registro::getTipoAcao).orElse(null),
                leitura.tipoAcao()
        );

        Registro novoRegistro = registroMapper.toEntity(leitura, participante, dataHoraServidor);

        try {
            Registro registroSalvo = registroRepository.saveAndFlush(novoRegistro);
            return new RegistroResultadoDTO(registroMapper.toResponse(registroSalvo), true);
        } catch (DataIntegrityViolationException exception) {
            throw new IdempotenciaConflitanteException(leitura.leituraId());
        }
    }

    private void validarDuplicidade(
            Optional<Registro> ultimoRegistro,
            LeituraQrCodeDTO leitura,
            OffsetDateTime dataHoraServidor
    ) {
        ultimoRegistro
                .filter(registro -> registro.getTipoAcao() == leitura.tipoAcao())
                .filter(registro -> Duration.between(registro.getDataHora(), dataHoraServidor)
                        .compareTo(INTERVALO_DUPLICIDADE) < 0)
                .ifPresent(registro -> {
                    throw new RegistroDuplicadoException();
                });
    }

    private RegistroResultadoDTO resultadoIdempotente(Registro registro, LeituraQrCodeDTO leitura) {
        if (!representaMesmaLeitura(registro, leitura)) {
            throw new IdempotenciaConflitanteException(leitura.leituraId());
        }

        return new RegistroResultadoDTO(registroMapper.toResponse(registro), false);
    }

    private boolean representaMesmaLeitura(Registro registro, LeituraQrCodeDTO leitura) {
        return registro.getParticipante().getId().equals(leitura.participanteId())
                && registro.getTipoAcao() == leitura.tipoAcao()
                && registro.getDispositivoId().equals(leitura.dispositivoId().strip())
                && registro.getDataHoraLidaNoCelular().toInstant().truncatedTo(ChronoUnit.MICROS)
                .equals(leitura.dataHoraLidaNoCelular().toInstant().truncatedTo(ChronoUnit.MICROS));
    }
}
