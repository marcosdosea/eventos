package com.itatechjr.bipou.service;

import com.itatechjr.bipou.dto.ParticipanteRequestDTO;
import com.itatechjr.bipou.dto.ParticipanteResponseDTO;
import com.itatechjr.bipou.exception.CadastroIdReutilizadoException;
import com.itatechjr.bipou.exception.CpfJaCadastradoException;
import com.itatechjr.bipou.exception.ParticipanteNaoEncontradoException;
import com.itatechjr.bipou.mapper.ParticipanteMapper;
import com.itatechjr.bipou.model.entity.Participante;
import com.itatechjr.bipou.model.valueobject.Cpf;
import com.itatechjr.bipou.repository.ParticipanteRepository;
import com.itatechjr.bipou.service.impl.ParticipanteServiceImpl;
import com.itatechjr.bipou.service.impl.ParticipanteCadastroTransactionalService;
import org.junit.jupiter.api.BeforeEach;
import org.junit.jupiter.api.Test;
import org.junit.jupiter.api.extension.ExtendWith;
import org.mockito.Mock;
import org.mockito.junit.jupiter.MockitoExtension;
import org.springframework.dao.DataIntegrityViolationException;

import java.util.UUID;
import java.util.List;
import java.util.Optional;

import static org.assertj.core.api.Assertions.assertThat;
import static org.assertj.core.api.Assertions.assertThatThrownBy;
import static org.mockito.ArgumentMatchers.any;
import static org.mockito.Mockito.never;
import static org.mockito.Mockito.verify;
import static org.mockito.Mockito.when;

@ExtendWith(MockitoExtension.class)
class ParticipanteServiceImplTests {

    @Mock
    private ParticipanteRepository participanteRepository;

    @Mock
    private ParticipanteCadastroTransactionalService cadastroTransactionalService;

    private ParticipanteService participanteService;

    @BeforeEach
    void configurar() {
        participanteService = new ParticipanteServiceImpl(
                participanteRepository,
                new ParticipanteMapper(),
                cadastroTransactionalService
        );
    }

    @Test
    void deveCadastrarParticipante() {
        ParticipanteRequestDTO request = new ParticipanteRequestDTO(
                "  Participante Teste  ",
                "52998224725",
                UUID.randomUUID(),
                1042L,
                15L,
                List.of(43L, 42L),
                "  Participante@UFS.br "
        );
        Participante participante = new ParticipanteMapper().toEntity(request);
        participante.setId(UUID.randomUUID());
        when(cadastroTransactionalService.buscarPorCadastroId(request.cadastroId()))
                .thenReturn(Optional.empty());
        when(cadastroTransactionalService.cadastrarNovo(request)).thenReturn(participante);

        ParticipanteResponseDTO response = participanteService.cadastrar(request);

        assertThat(response.id()).isNotNull();
        assertThat(response.nome()).isEqualTo("Participante Teste");
        assertThat(response.cpf()).isEqualTo("52998224725");
        assertThat(response.email()).isEqualTo("participante@ufs.br");
        assertThat(response.idPessoaBorala()).isEqualTo(1042L);
        assertThat(response.eventoId()).isEqualTo(15L);
        assertThat(response.subeventoIds()).containsExactly(42L, 43L);
    }

    @Test
    void deveRejeitarCpfJaCadastrado() {
        ParticipanteRequestDTO request = new ParticipanteRequestDTO("Participante", "52998224725", 15L);
        when(cadastroTransactionalService.cadastrarNovo(request))
                .thenThrow(new CpfJaCadastradoException(request.cpf()));

        assertThatThrownBy(() -> participanteService.cadastrar(request))
                .isInstanceOf(CpfJaCadastradoException.class);

        verify(participanteRepository, never()).saveAllAndFlush(any());
    }

    @Test
    void deveTraduzirCorridaDeCpfParaExcecaoDeNegocio() {
        ParticipanteRequestDTO request = new ParticipanteRequestDTO("Participante", "52998224725", 15L);
        when(cadastroTransactionalService.cadastrarNovo(request))
                .thenThrow(new DataIntegrityViolationException("uk_participante_evento_cpf"));

        assertThatThrownBy(() -> participanteService.cadastrar(request))
                .isInstanceOf(CpfJaCadastradoException.class);
    }

    @Test
    void deveRetornarMesmoParticipanteAoReenviarCadastroId() {
        UUID cadastroId = UUID.randomUUID();
        ParticipanteRequestDTO request = new ParticipanteRequestDTO(
                "Participante",
                "52998224725",
                cadastroId,
                1042L,
                15L,
                List.of(42L)
        );
        Participante existente = new ParticipanteMapper().toEntity(request);
        existente.setId(UUID.randomUUID());
        when(cadastroTransactionalService.buscarPorCadastroId(cadastroId))
                .thenReturn(Optional.of(existente));

        ParticipanteResponseDTO response = participanteService.cadastrar(request);

        assertThat(response.id()).isEqualTo(existente.getId());
        verify(cadastroTransactionalService, never()).cadastrarNovo(any());
    }

    @Test
    void deveRejeitarCadastroIdReutilizadoComOutroPayload() {
        UUID cadastroId = UUID.randomUUID();
        ParticipanteRequestDTO original = new ParticipanteRequestDTO(
                "Participante Original",
                "52998224725",
                cadastroId,
                1042L,
                15L,
                List.of(42L)
        );
        ParticipanteRequestDTO alterado = new ParticipanteRequestDTO(
                "Participante Original",
                "52998224725",
                cadastroId,
                1042L,
                16L,
                List.of(42L)
        );
        Participante existente = new ParticipanteMapper().toEntity(original);
        existente.setId(UUID.randomUUID());
        when(cadastroTransactionalService.buscarPorCadastroId(cadastroId))
                .thenReturn(Optional.of(existente));

        assertThatThrownBy(() -> participanteService.cadastrar(alterado))
                .isInstanceOf(CadastroIdReutilizadoException.class);
    }

    @Test
    void deveBuscarParticipantePorCpfNoContextoDoEvento() {
        ParticipanteRequestDTO request = new ParticipanteRequestDTO(
                "Participante",
                "52998224725",
                null,
                1042L,
                15L,
                List.of(42L)
        );
        Participante participante = new ParticipanteMapper().toEntity(request);
        participante.setId(UUID.randomUUID());
        when(participanteRepository.findByEventoIdAndCpf(15L, new Cpf("52998224725")))
                .thenReturn(Optional.of(participante));

        ParticipanteResponseDTO response = participanteService.buscarPorCpf("52998224725", 15L);

        assertThat(response.id()).isEqualTo(participante.getId());
        assertThat(response.nome()).isEqualTo("Participante");
        assertThat(response.cpf()).isEqualTo("52998224725");
        assertThat(response.eventoId()).isEqualTo(15L);
    }

    @Test
    void deveInformarQuandoCpfNaoPertenceAParticipante() {
        when(participanteRepository.findFirstByCpfOrderByEventoIdAsc(new Cpf("52998224725")))
                .thenReturn(Optional.empty());

        assertThatThrownBy(() -> participanteService.buscarPorCpf("52998224725"))
                .isInstanceOf(ParticipanteNaoEncontradoException.class);
    }

    @Test
    void deveCadastrarParticipantesEmUmUnicoLoteEPermitirMesmoCpfEmEventosDiferentes() {
        List<ParticipanteRequestDTO> requests = List.of(
                new ParticipanteRequestDTO("Participante Evento 1", "52998224725", 15L),
                new ParticipanteRequestDTO("Participante Evento 2", "52998224725", 16L)
        );
        when(participanteRepository.findAllByEventoIdAndCpfIn(any(), any())).thenReturn(List.of());
        when(participanteRepository.saveAllAndFlush(any())).thenAnswer(invocacao -> {
            List<Participante> participantes = invocacao.getArgument(0);
            participantes.forEach(participante -> participante.setId(UUID.randomUUID()));
            return participantes;
        });

        List<ParticipanteResponseDTO> responses = participanteService.cadastrarEmLote(requests);

        assertThat(responses).hasSize(2);
        assertThat(responses).extracting(ParticipanteResponseDTO::eventoId)
                .containsExactly(15L, 16L);
    }

    @Test
    void deveRejeitarLoteComCpfRepetidoNoMesmoEventoAntesDeSalvar() {
        List<ParticipanteRequestDTO> requests = List.of(
                new ParticipanteRequestDTO("Participante Um", "52998224725", 15L),
                new ParticipanteRequestDTO("Participante Dois", "52998224725", 15L)
        );

        assertThatThrownBy(() -> participanteService.cadastrarEmLote(requests))
                .isInstanceOf(CpfJaCadastradoException.class);

        verify(participanteRepository, never()).saveAllAndFlush(any());
    }

    @Test
    void deveFiltrarParticipantesPorEventoESubevento() {
        Participante p1 = new ParticipanteMapper().toEntity(
                new ParticipanteRequestDTO("Ana", "52998224725", null, 100L, 15L, List.of(42L)));
        p1.setId(UUID.randomUUID());

        when(participanteRepository.findAllByEventoIdAndSubeventoIdOrderByNomeAsc(15L, 42L))
                .thenReturn(List.of(p1));
        when(participanteRepository.findAllByEventoIdOrderByNomeAsc(15L))
                .thenReturn(List.of(p1));

        assertThat(participanteService.listar(15L, 42L))
                .singleElement()
                .satisfies(dto -> {
                    assertThat(dto.eventoId()).isEqualTo(15L);
                    assertThat(dto.subeventoIds()).containsExactly(42L);
                });

        assertThat(participanteService.listar(15L, null)).hasSize(1);
    }
}
