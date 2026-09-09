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
                UUID.randomUUID()
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
    }

    @Test
    void deveRejeitarCpfJaCadastrado() {
        ParticipanteRequestDTO request = new ParticipanteRequestDTO("Participante", "52998224725");
        when(cadastroTransactionalService.cadastrarNovo(request))
                .thenThrow(new CpfJaCadastradoException(request.cpf()));

        assertThatThrownBy(() -> participanteService.cadastrar(request))
                .isInstanceOf(CpfJaCadastradoException.class);

        verify(participanteRepository, never()).saveAndFlush(any());
    }

    @Test
    void deveTraduzirCorridaDeCpfParaExcecaoDeNegocio() {
        ParticipanteRequestDTO request = new ParticipanteRequestDTO("Participante", "52998224725");
        when(cadastroTransactionalService.cadastrarNovo(request))
                .thenThrow(new DataIntegrityViolationException("participante_cpf_key"));

        assertThatThrownBy(() -> participanteService.cadastrar(request))
                .isInstanceOf(CpfJaCadastradoException.class);
    }

    @Test
    void deveRetornarMesmoParticipanteAoReenviarCadastroId() {
        UUID cadastroId = UUID.randomUUID();
        ParticipanteRequestDTO request = new ParticipanteRequestDTO(
                "Participante",
                "52998224725",
                cadastroId
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
                cadastroId
        );
        ParticipanteRequestDTO alterado = new ParticipanteRequestDTO(
                "Participante Alterado",
                "52998224725",
                cadastroId
        );
        Participante existente = new ParticipanteMapper().toEntity(original);
        existente.setId(UUID.randomUUID());
        when(cadastroTransactionalService.buscarPorCadastroId(cadastroId))
                .thenReturn(Optional.of(existente));

        assertThatThrownBy(() -> participanteService.cadastrar(alterado))
                .isInstanceOf(CadastroIdReutilizadoException.class);
    }

    @Test
    void deveBuscarParticipantePorCpf() {
        ParticipanteRequestDTO request = new ParticipanteRequestDTO(
                "Participante",
                "52998224725"
        );
        Participante participante = new ParticipanteMapper().toEntity(request);
        participante.setId(UUID.randomUUID());
        when(participanteRepository.findByCpf(new Cpf("52998224725")))
                .thenReturn(Optional.of(participante));

        ParticipanteResponseDTO response = participanteService.buscarPorCpf("52998224725");

        assertThat(response.id()).isEqualTo(participante.getId());
        assertThat(response.nome()).isEqualTo("Participante");
        assertThat(response.cpf()).isEqualTo("52998224725");
    }

    @Test
    void deveInformarQuandoCpfNaoPertenceAParticipante() {
        when(participanteRepository.findByCpf(new Cpf("52998224725")))
                .thenReturn(Optional.empty());

        assertThatThrownBy(() -> participanteService.buscarPorCpf("52998224725"))
                .isInstanceOf(ParticipanteNaoEncontradoException.class);
    }

    @Test
    void deveCadastrarParticipantesEmUmUnicoLote() {
        List<ParticipanteRequestDTO> requests = List.of(
                new ParticipanteRequestDTO("Participante Um", "52998224725"),
                new ParticipanteRequestDTO("Participante Dois", "11144477735")
        );
        when(participanteRepository.findAllByCpfIn(any())).thenReturn(List.of());
        when(participanteRepository.saveAllAndFlush(any())).thenAnswer(invocacao -> {
            List<Participante> participantes = invocacao.getArgument(0);
            participantes.forEach(participante -> participante.setId(UUID.randomUUID()));
            return participantes;
        });

        List<ParticipanteResponseDTO> responses = participanteService.cadastrarEmLote(requests);

        assertThat(responses).hasSize(2);
        assertThat(responses).extracting(ParticipanteResponseDTO::nome)
                .containsExactly("Participante Um", "Participante Dois");
    }

    @Test
    void deveRejeitarLoteComCpfRepetidoAntesDeSalvar() {
        List<ParticipanteRequestDTO> requests = List.of(
                new ParticipanteRequestDTO("Participante Um", "52998224725"),
                new ParticipanteRequestDTO("Participante Dois", "52998224725")
        );

        assertThatThrownBy(() -> participanteService.cadastrarEmLote(requests))
                .isInstanceOf(CpfJaCadastradoException.class);

        verify(participanteRepository, never()).saveAllAndFlush(any());
    }
}
