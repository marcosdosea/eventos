package com.itatechjr.bipou.controller;

import com.itatechjr.bipou.dto.ConsultaParticipanteCpfDTO;
import com.itatechjr.bipou.dto.LeituraQrCodeDTO;
import com.itatechjr.bipou.dto.ParticipanteRequestDTO;
import com.itatechjr.bipou.dto.ParticipanteResponseDTO;
import com.itatechjr.bipou.dto.RegistroResponseDTO;
import com.itatechjr.bipou.dto.RegistroResultadoDTO;
import com.itatechjr.bipou.exception.GlobalExceptionHandler;
import com.itatechjr.bipou.exception.ParticipanteNaoInscritoException;
import com.itatechjr.bipou.exception.PessoaBoralaJaCadastradaException;
import com.itatechjr.bipou.model.enums.TipoAcao;
import com.itatechjr.bipou.service.ParticipanteService;
import com.itatechjr.bipou.service.RegistroService;
import org.junit.jupiter.api.BeforeEach;
import org.junit.jupiter.api.Test;
import org.junit.jupiter.api.extension.ExtendWith;
import org.mockito.Mock;
import org.mockito.junit.jupiter.MockitoExtension;
import org.springframework.http.MediaType;
import org.springframework.test.web.servlet.MockMvc;
import org.springframework.test.web.servlet.setup.MockMvcBuilders;
import tools.jackson.databind.ObjectMapper;

import java.time.Clock;
import java.time.Instant;
import java.time.OffsetDateTime;
import java.time.ZoneOffset;
import java.util.List;
import java.util.UUID;

import static org.mockito.ArgumentMatchers.any;
import static org.mockito.Mockito.verify;
import static org.mockito.Mockito.when;
import static org.springframework.test.web.servlet.request.MockMvcRequestBuilders.get;
import static org.springframework.test.web.servlet.request.MockMvcRequestBuilders.post;
import static org.springframework.test.web.servlet.result.MockMvcResultMatchers.jsonPath;
import static org.springframework.test.web.servlet.result.MockMvcResultMatchers.status;

@ExtendWith(MockitoExtension.class)
class ControllerWebTests {

    private static final Instant AGORA = Instant.parse("2026-11-16T12:00:00Z");

    @Mock
    private ParticipanteService participanteService;

    @Mock
    private RegistroService registroService;

    private MockMvc mockMvc;
    private ObjectMapper objectMapper;

    @BeforeEach
    void configurar() {
        objectMapper = new ObjectMapper();
        GlobalExceptionHandler exceptionHandler = new GlobalExceptionHandler(Clock.fixed(AGORA, ZoneOffset.UTC));
        mockMvc = MockMvcBuilders.standaloneSetup(
                        new ParticipanteController(participanteService),
                        new RegistroController(registroService)
                )
                .setControllerAdvice(exceptionHandler)
                .build();
    }

    @Test
    void deveRepassarFiltrosDeEventoESubeventoAoListarParticipantes() throws Exception {
        UUID id = UUID.randomUUID();
        when(participanteService.listar(15L, 42L)).thenReturn(List.of(
                new ParticipanteResponseDTO(id, "Maria", "52998224725", 1042L, 15L, List.of(42L))
        ));

        mockMvc.perform(get("/api/participantes")
                        .param("eventoId", "15")
                        .param("subeventoId", "42"))
                .andExpect(status().isOk())
                .andExpect(jsonPath("$[0].id").value(id.toString()))
                .andExpect(jsonPath("$[0].eventoId").value(15))
                .andExpect(jsonPath("$[0].subeventoIds[0]").value(42));

        verify(participanteService).listar(15L, 42L);
    }

    @Test
    void deveRepassarEventoIdNaConsultaPorCpf() throws Exception {
        UUID id = UUID.randomUUID();
        when(participanteService.buscarPorCpf("52998224725", 15L)).thenReturn(
                new ParticipanteResponseDTO(id, "Maria", "52998224725", 1042L, 15L, List.of(42L))
        );

        mockMvc.perform(post("/api/participantes/consultar-por-cpf")
                        .contentType(MediaType.APPLICATION_JSON)
                        .content(objectMapper.writeValueAsString(new ConsultaParticipanteCpfDTO("52998224725", 15L))))
                .andExpect(status().isOk())
                .andExpect(jsonPath("$.eventoId").value(15));

        verify(participanteService).buscarPorCpf("52998224725", 15L);
    }

    @Test
    void deveRetornarConflictQuandoPessoaBoralaJaEstiverCadastradaNoEvento() throws Exception {
        when(participanteService.cadastrar(any(ParticipanteRequestDTO.class)))
                .thenThrow(new PessoaBoralaJaCadastradaException(1042L, 15L));

        ParticipanteRequestDTO request = new ParticipanteRequestDTO(
                "Maria",
                "52998224725",
                null,
                1042L,
                15L,
                List.of(42L)
        );

        mockMvc.perform(post("/api/participantes")
                        .contentType(MediaType.APPLICATION_JSON)
                        .content(objectMapper.writeValueAsString(request)))
                .andExpect(status().isConflict())
                .andExpect(jsonPath("$.status").value(409));
    }

    @Test
    void deveRepassarFiltrosDeContextoAoListarRegistros() throws Exception {
        UUID registroId = UUID.randomUUID();
        UUID leituraId = UUID.randomUUID();
        UUID participanteId = UUID.randomUUID();
        OffsetDateTime dataHora = OffsetDateTime.ofInstant(AGORA, ZoneOffset.UTC);

        when(registroService.listar(15L, null, true)).thenReturn(List.of(
                new RegistroResponseDTO(
                        registroId,
                        leituraId,
                        participanteId,
                        "Maria",
                        15L,
                        null,
                        TipoAcao.ENTRADA,
                        dataHora,
                        dataHora.minusSeconds(2),
                        "celular-01",
                        "operador.portaria"
                )
        ));

        mockMvc.perform(get("/api/registros")
                        .param("eventoId", "15")
                        .param("apenasEventoGeral", "true"))
                .andExpect(status().isOk())
                .andExpect(jsonPath("$[0].eventoId").value(15))
                .andExpect(jsonPath("$[0].operadorUsuario").value("operador.portaria"));

        verify(registroService).listar(15L, null, true);
    }

    @Test
    void deveRetornar422QuandoParticipanteNaoEstiverInscritoNoContexto() throws Exception {
        UUID participanteId = UUID.randomUUID();
        when(registroService.registrar(any(LeituraQrCodeDTO.class)))
                .thenThrow(new ParticipanteNaoInscritoException(participanteId, 15L, 99L));

        LeituraQrCodeDTO leitura = new LeituraQrCodeDTO(
                UUID.randomUUID(),
                participanteId,
                15L,
                99L,
                TipoAcao.ENTRADA,
                "celular-01",
                "operador.portaria",
                OffsetDateTime.ofInstant(AGORA, ZoneOffset.UTC)
        );

        mockMvc.perform(post("/api/registros")
                        .contentType(MediaType.APPLICATION_JSON)
                        .content(objectMapper.writeValueAsString(leitura)))
                .andExpect(status().isUnprocessableContent())
                .andExpect(jsonPath("$.status").value(422));
    }

    @Test
    void deveRetornar201AoRegistrarLeituraComContextoEOperador() throws Exception {
        UUID registroId = UUID.randomUUID();
        UUID leituraId = UUID.randomUUID();
        UUID participanteId = UUID.randomUUID();
        OffsetDateTime dataHora = OffsetDateTime.ofInstant(AGORA, ZoneOffset.UTC);

        when(registroService.registrar(any(LeituraQrCodeDTO.class))).thenReturn(
                new RegistroResultadoDTO(
                        new RegistroResponseDTO(
                                registroId,
                                leituraId,
                                participanteId,
                                "Maria",
                                15L,
                                42L,
                                TipoAcao.ENTRADA,
                                dataHora,
                                dataHora,
                                "celular-01",
                                "operador.portaria"
                        ),
                        true
                )
        );

        LeituraQrCodeDTO leitura = new LeituraQrCodeDTO(
                leituraId,
                participanteId,
                15L,
                42L,
                TipoAcao.ENTRADA,
                "celular-01",
                "operador.portaria",
                dataHora
        );

        mockMvc.perform(post("/api/registros")
                        .contentType(MediaType.APPLICATION_JSON)
                        .content(objectMapper.writeValueAsString(leitura)))
                .andExpect(status().isCreated())
                .andExpect(jsonPath("$.eventoId").value(15))
                .andExpect(jsonPath("$.subeventoId").value(42))
                .andExpect(jsonPath("$.operadorUsuario").value("operador.portaria"));
    }
}
