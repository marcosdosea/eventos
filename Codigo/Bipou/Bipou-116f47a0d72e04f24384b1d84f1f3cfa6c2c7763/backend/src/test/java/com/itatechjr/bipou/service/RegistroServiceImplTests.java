package com.itatechjr.bipou.service;

import com.itatechjr.bipou.dto.LeituraQrCodeDTO;
import com.itatechjr.bipou.dto.RegistroResponseDTO;
import com.itatechjr.bipou.dto.RegistroResultadoDTO;
import com.itatechjr.bipou.exception.IdempotenciaConflitanteException;
import com.itatechjr.bipou.exception.ParticipanteNaoEncontradoException;
import com.itatechjr.bipou.exception.ParticipanteNaoInscritoException;
import com.itatechjr.bipou.exception.RegistroDuplicadoException;
import com.itatechjr.bipou.exception.TransicaoAcaoInvalidaException;
import com.itatechjr.bipou.mapper.RegistroMapper;
import com.itatechjr.bipou.model.entity.Participante;
import com.itatechjr.bipou.model.entity.Registro;
import com.itatechjr.bipou.model.enums.TipoAcao;
import com.itatechjr.bipou.repository.ParticipanteRepository;
import com.itatechjr.bipou.repository.RegistroRepository;
import com.itatechjr.bipou.service.impl.RegistroServiceImpl;
import com.itatechjr.bipou.service.validation.RegistroTransicaoValidator;
import org.junit.jupiter.api.BeforeEach;
import org.junit.jupiter.api.Test;
import org.junit.jupiter.api.extension.ExtendWith;
import org.mockito.Mock;
import org.mockito.junit.jupiter.MockitoExtension;

import java.time.Clock;
import java.time.Instant;
import java.time.OffsetDateTime;
import java.time.ZoneOffset;
import java.util.List;
import java.util.Optional;
import java.util.Set;
import java.util.UUID;

import static org.assertj.core.api.Assertions.assertThat;
import static org.assertj.core.api.Assertions.assertThatThrownBy;
import static org.mockito.ArgumentMatchers.any;
import static org.mockito.Mockito.never;
import static org.mockito.Mockito.verify;
import static org.mockito.Mockito.when;

@ExtendWith(MockitoExtension.class)
class RegistroServiceImplTests {

    private static final Instant AGORA = Instant.parse("2026-08-22T18:00:00Z");
    private static final Long EVENTO_ID = 15L;
    private static final Long SUBEVENTO_ID = 42L;

    @Mock
    private ParticipanteRepository participanteRepository;

    @Mock
    private RegistroRepository registroRepository;

    private RegistroService registroService;
    private Participante participante;

    @BeforeEach
    void configurar() {
        registroService = new RegistroServiceImpl(
                participanteRepository,
                registroRepository,
                new RegistroMapper(),
                new RegistroTransicaoValidator(),
                Clock.fixed(AGORA, ZoneOffset.UTC)
        );
        participante = Participante.builder()
                .id(UUID.randomUUID())
                .nome("Participante Teste")
                .eventoId(EVENTO_ID)
                .subeventoIds(Set.of(SUBEVENTO_ID))
                .build();
    }

    @Test
    void deveRegistrarPrimeiraEntradaComHorarioUtcDoServidor() {
        LeituraQrCodeDTO leitura = novaLeitura(UUID.randomUUID(), null, TipoAcao.ENTRADA);
        when(participanteRepository.findByIdForUpdate(leitura.participanteId()))
                .thenReturn(Optional.of(participante));
        when(registroRepository.findTopByContextoOrderByDataHoraDesc(participante.getId(), EVENTO_ID, null))
                .thenReturn(Optional.empty());
        when(registroRepository.saveAndFlush(any(Registro.class))).thenAnswer(invocacao -> {
            Registro registro = invocacao.getArgument(0);
            registro.setId(UUID.randomUUID());
            return registro;
        });

        RegistroResultadoDTO resultado = registroService.registrar(leitura);

        assertThat(resultado.criado()).isTrue();
        assertThat(resultado.registro().leituraId()).isEqualTo(leitura.leituraId());
        assertThat(resultado.registro().eventoId()).isEqualTo(EVENTO_ID);
        assertThat(resultado.registro().subeventoId()).isNull();
        assertThat(resultado.registro().operadorUsuario()).isEqualTo("operador.portaria");
        assertThat(resultado.registro().dataHora()).isEqualTo(OffsetDateTime.ofInstant(AGORA, ZoneOffset.UTC));
        assertThat(resultado.registro().dataHoraLidaNoCelular()).isEqualTo(leitura.dataHoraLidaNoCelular());
    }

    @Test
    void devePermitirEntradaNoMinicursoSemInterferirNoEventoGeral() {
        LeituraQrCodeDTO leituraMinicurso = novaLeitura(UUID.randomUUID(), SUBEVENTO_ID, TipoAcao.ENTRADA);
        when(participanteRepository.findByIdForUpdate(leituraMinicurso.participanteId()))
                .thenReturn(Optional.of(participante));
        when(registroRepository.findTopByContextoOrderByDataHoraDesc(participante.getId(), EVENTO_ID, SUBEVENTO_ID))
                .thenReturn(Optional.empty());
        when(registroRepository.saveAndFlush(any(Registro.class))).thenAnswer(invocacao -> {
            Registro registro = invocacao.getArgument(0);
            registro.setId(UUID.randomUUID());
            return registro;
        });

        RegistroResultadoDTO resultado = registroService.registrar(leituraMinicurso);

        assertThat(resultado.criado()).isTrue();
        assertThat(resultado.registro().eventoId()).isEqualTo(EVENTO_ID);
        assertThat(resultado.registro().subeventoId()).isEqualTo(SUBEVENTO_ID);
    }

    @Test
    void deveRejeitarLeituraQuandoParticipanteNaoEstaInscritoNoSubevento() {
        LeituraQrCodeDTO leituraOutroCurso = novaLeitura(UUID.randomUUID(), 999L, TipoAcao.ENTRADA);
        when(participanteRepository.findByIdForUpdate(leituraOutroCurso.participanteId()))
                .thenReturn(Optional.of(participante));

        assertThatThrownBy(() -> registroService.registrar(leituraOutroCurso))
                .isInstanceOf(ParticipanteNaoInscritoException.class);

        verify(registroRepository, never()).saveAndFlush(any());
    }

    @Test
    void deveRejeitarLeituraQuandoEventoDivergeDoParticipante() {
        LeituraQrCodeDTO leituraOutroEvento = new LeituraQrCodeDTO(
                UUID.randomUUID(),
                participante.getId(),
                99L,
                null,
                TipoAcao.ENTRADA,
                "celular-01",
                "operador.portaria",
                OffsetDateTime.ofInstant(AGORA.minusSeconds(1), ZoneOffset.ofHours(-3))
        );
        when(participanteRepository.findByIdForUpdate(leituraOutroEvento.participanteId()))
                .thenReturn(Optional.of(participante));

        assertThatThrownBy(() -> registroService.registrar(leituraOutroEvento))
                .isInstanceOf(ParticipanteNaoInscritoException.class);

        verify(registroRepository, never()).saveAndFlush(any());
    }

    @Test
    void deveRetornarMesmoRegistroEmReenvioIdempotente() {
        UUID leituraId = UUID.randomUUID();
        LeituraQrCodeDTO leitura = novaLeitura(leituraId, SUBEVENTO_ID, TipoAcao.ENTRADA);
        Registro existente = registroExistente(leitura, OffsetDateTime.ofInstant(AGORA, ZoneOffset.UTC));
        when(registroRepository.findByLeituraId(leituraId)).thenReturn(Optional.of(existente));

        RegistroResultadoDTO resultado = registroService.registrar(leitura);

        assertThat(resultado.criado()).isFalse();
        assertThat(resultado.registro().id()).isEqualTo(existente.getId());
        verify(participanteRepository, never()).findByIdForUpdate(any());
        verify(registroRepository, never()).saveAndFlush(any());
    }

    @Test
    void deveRejeitarLeituraIdReutilizadoComOutrosDados() {
        UUID leituraId = UUID.randomUUID();
        LeituraQrCodeDTO leituraOriginal = novaLeitura(leituraId, SUBEVENTO_ID, TipoAcao.ENTRADA);
        Registro existente = registroExistente(leituraOriginal, OffsetDateTime.ofInstant(AGORA, ZoneOffset.UTC));
        when(registroRepository.findByLeituraId(leituraId)).thenReturn(Optional.of(existente));

        LeituraQrCodeDTO leituraAlterada = new LeituraQrCodeDTO(
                leituraId,
                leituraOriginal.participanteId(),
                EVENTO_ID,
                null,
                leituraOriginal.tipoAcao(),
                leituraOriginal.dispositivoId(),
                leituraOriginal.operadorUsuario(),
                leituraOriginal.dataHoraLidaNoCelular()
        );

        assertThatThrownBy(() -> registroService.registrar(leituraAlterada))
                .isInstanceOf(IdempotenciaConflitanteException.class);
    }

    @Test
    void deveRejeitarDuploBipDaMesmaAcaoEmMenosDeDezSegundos() {
        LeituraQrCodeDTO leitura = novaLeitura(UUID.randomUUID(), null, TipoAcao.ENTRADA);
        Registro ultimoRegistro = registroExistente(
                novaLeitura(UUID.randomUUID(), null, TipoAcao.ENTRADA),
                OffsetDateTime.ofInstant(AGORA.minusSeconds(5), ZoneOffset.UTC)
        );
        when(participanteRepository.findByIdForUpdate(leitura.participanteId()))
                .thenReturn(Optional.of(participante));
        when(registroRepository.findTopByContextoOrderByDataHoraDesc(participante.getId(), EVENTO_ID, null))
                .thenReturn(Optional.of(ultimoRegistro));

        assertThatThrownBy(() -> registroService.registrar(leitura))
                .isInstanceOf(RegistroDuplicadoException.class);

        verify(registroRepository, never()).saveAndFlush(any());
    }

    @Test
    void deveRejeitarPrimeiraAcaoDiferenteDeEntrada() {
        LeituraQrCodeDTO leitura = novaLeitura(UUID.randomUUID(), null, TipoAcao.SAIDA);
        when(participanteRepository.findByIdForUpdate(leitura.participanteId()))
                .thenReturn(Optional.of(participante));
        when(registroRepository.findTopByContextoOrderByDataHoraDesc(participante.getId(), EVENTO_ID, null))
                .thenReturn(Optional.empty());

        assertThatThrownBy(() -> registroService.registrar(leitura))
                .isInstanceOf(TransicaoAcaoInvalidaException.class);
    }

    @Test
    void deveRejeitarParticipanteInexistente() {
        LeituraQrCodeDTO leitura = novaLeitura(UUID.randomUUID(), null, TipoAcao.ENTRADA);
        when(participanteRepository.findByIdForUpdate(leitura.participanteId()))
                .thenReturn(Optional.empty());

        assertThatThrownBy(() -> registroService.registrar(leitura))
                .isInstanceOf(ParticipanteNaoEncontradoException.class);
    }

    @Test
    void deveFiltrarRegistrosPorEventoESubevento() {
        Registro registroSubevento = registroExistente(
                novaLeitura(UUID.randomUUID(), SUBEVENTO_ID, TipoAcao.ENTRADA),
                OffsetDateTime.ofInstant(AGORA, ZoneOffset.UTC)
        );
        when(registroRepository.findAllByEventoIdAndSubeventoIdOrderByDataHoraDesc(EVENTO_ID, SUBEVENTO_ID))
                .thenReturn(List.of(registroSubevento));

        List<RegistroResponseDTO> resultado = registroService.listar(EVENTO_ID, SUBEVENTO_ID);

        assertThat(resultado).singleElement().satisfies(dto -> {
            assertThat(dto.eventoId()).isEqualTo(EVENTO_ID);
            assertThat(dto.subeventoId()).isEqualTo(SUBEVENTO_ID);
            assertThat(dto.operadorUsuario()).isEqualTo("operador.portaria");
        });
    }

    private LeituraQrCodeDTO novaLeitura(UUID leituraId, Long subeventoId, TipoAcao tipoAcao) {
        return new LeituraQrCodeDTO(
                leituraId,
                participante.getId(),
                EVENTO_ID,
                subeventoId,
                tipoAcao,
                "celular-01",
                "operador.portaria",
                OffsetDateTime.ofInstant(AGORA.minusSeconds(1), ZoneOffset.ofHours(-3))
        );
    }

    private Registro registroExistente(LeituraQrCodeDTO leitura, OffsetDateTime dataHoraServidor) {
        return Registro.builder()
                .id(UUID.randomUUID())
                .leituraId(leitura.leituraId())
                .participante(participante)
                .eventoId(leitura.eventoId())
                .subeventoId(leitura.subeventoId())
                .tipoAcao(leitura.tipoAcao())
                .dataHora(dataHoraServidor)
                .dataHoraLidaNoCelular(leitura.dataHoraLidaNoCelular())
                .dispositivoId(leitura.dispositivoId())
                .operadorUsuario(leitura.operadorUsuario())
                .build();
    }
}
