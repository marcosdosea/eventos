package com.itatechjr.bipou.controller;

import com.itatechjr.bipou.dto.ConsultaParticipanteCpfDTO;
import com.itatechjr.bipou.dto.LeituraQrCodeDTO;
import com.itatechjr.bipou.dto.ParticipanteRequestDTO;
import com.itatechjr.bipou.model.enums.TipoAcao;
import com.jayway.jsonpath.JsonPath;
import org.junit.jupiter.api.AfterEach;
import org.junit.jupiter.api.BeforeEach;
import org.junit.jupiter.api.Test;
import org.springframework.beans.factory.annotation.Autowired;
import org.springframework.boot.test.context.SpringBootTest;
import org.springframework.boot.webmvc.test.autoconfigure.AutoConfigureMockMvc;
import org.springframework.http.MediaType;
import org.springframework.jdbc.core.JdbcTemplate;
import org.springframework.test.web.servlet.MockMvc;
import org.springframework.test.web.servlet.MvcResult;
import tools.jackson.databind.ObjectMapper;

import java.time.OffsetDateTime;
import java.time.ZoneOffset;
import java.util.UUID;

import static org.hamcrest.Matchers.containsString;
import static org.hamcrest.Matchers.startsWith;
import static org.springframework.test.web.servlet.request.MockMvcRequestBuilders.get;
import static org.springframework.test.web.servlet.request.MockMvcRequestBuilders.post;
import static org.springframework.test.web.servlet.result.MockMvcResultMatchers.header;
import static org.springframework.test.web.servlet.result.MockMvcResultMatchers.content;
import static org.springframework.test.web.servlet.result.MockMvcResultMatchers.jsonPath;
import static org.springframework.test.web.servlet.result.MockMvcResultMatchers.status;

@SpringBootTest
@AutoConfigureMockMvc
class ApiIntegrationTests {

    @Autowired
    private MockMvc mockMvc;

    @Autowired
    private ObjectMapper objectMapper;

    @Autowired
    private JdbcTemplate jdbcTemplate;

    private String cpf;
    private UUID participanteId;

    @BeforeEach
    void gerarCpfDoTeste() {
        long base = Math.floorMod(UUID.randomUUID().getMostSignificantBits(), 1_000_000_000L);
        String primeirosDigitos = String.format("%09d", base);
        int primeiroVerificador = calcularDigitoCpf(primeirosDigitos, 10);
        int segundoVerificador = calcularDigitoCpf(primeirosDigitos + primeiroVerificador, 11);
        cpf = primeirosDigitos + primeiroVerificador + segundoVerificador;
    }

    @AfterEach
    void limparDadosDoTeste() {
        if (participanteId != null) {
            jdbcTemplate.update("DELETE FROM registro WHERE participante_id = ?", participanteId);
            jdbcTemplate.update("DELETE FROM participante WHERE id = ?", participanteId);
        }
    }

    @Test
    void deveCadastrarParticipanteComStatusCreated() throws Exception {
        MvcResult resultado = mockMvc.perform(post("/api/participantes")
                        .contentType(MediaType.APPLICATION_JSON)
                        .content(objectMapper.writeValueAsString(novoParticipante())))
                .andExpect(status().isCreated())
                .andExpect(header().string("Location", startsWith("/api/participantes/")))
                .andExpect(jsonPath("$.id").isNotEmpty())
                .andExpect(jsonPath("$.nome").value("Participante API"))
                .andExpect(jsonPath("$.cpf").value(cpf))
                .andReturn();
        participanteId = extrairParticipanteId(resultado);
    }

    @Test
    void deveRejeitarParticipanteSemCpf() throws Exception {
        ParticipanteRequestDTO request = new ParticipanteRequestDTO("Participante API", null);

        mockMvc.perform(post("/api/participantes")
                        .contentType(MediaType.APPLICATION_JSON)
                        .content(objectMapper.writeValueAsString(request)))
                .andExpect(status().isBadRequest())
                .andExpect(jsonPath("$.campos.cpf").exists());
    }

    @Test
    void deveRejeitarCpfComDigitosVerificadoresInvalidos() throws Exception {
        ParticipanteRequestDTO request = new ParticipanteRequestDTO("Participante API", "12345678900");

        mockMvc.perform(post("/api/participantes")
                        .contentType(MediaType.APPLICATION_JSON)
                        .content(objectMapper.writeValueAsString(request)))
                .andExpect(status().isBadRequest())
                .andExpect(jsonPath("$.mensagem").value("O CPF informado é inválido"));
    }

    @Test
    void deveListarParticipantes() throws Exception {
        cadastrarParticipante();

        mockMvc.perform(get("/api/participantes"))
                .andExpect(status().isOk())
                .andExpect(content().string(containsString("Participante API")))
                .andExpect(content().string(containsString(participanteId.toString())));
    }

    @Test
    void deveConsultarParticipantePorCpf() throws Exception {
        cadastrarParticipante();

        mockMvc.perform(post("/api/participantes/consultar-por-cpf")
                        .contentType(MediaType.APPLICATION_JSON)
                        .content(objectMapper.writeValueAsString(new ConsultaParticipanteCpfDTO(cpf))))
                .andExpect(status().isOk())
                .andExpect(jsonPath("$.id").value(participanteId.toString()))
                .andExpect(jsonPath("$.nome").value("Participante API"))
                .andExpect(jsonPath("$.cpf").value(cpf));
    }

    @Test
    void deveRetornarBadRequestParaParticipanteInvalido() throws Exception {
        ParticipanteRequestDTO request = new ParticipanteRequestDTO("", "123");

        mockMvc.perform(post("/api/participantes")
                        .contentType(MediaType.APPLICATION_JSON)
                        .content(objectMapper.writeValueAsString(request)))
                .andExpect(status().isBadRequest())
                .andExpect(jsonPath("$.status").value(400))
                .andExpect(jsonPath("$.campos.nome").exists())
                .andExpect(jsonPath("$.campos.cpf").exists());
    }

    @Test
    void deveRetornarConflictParaCpfDuplicado() throws Exception {
        cadastrarParticipante();
        ParticipanteRequestDTO mesmoCpf = new ParticipanteRequestDTO("Outro Participante", cpf);

        mockMvc.perform(post("/api/participantes")
                        .contentType(MediaType.APPLICATION_JSON)
                        .content(objectMapper.writeValueAsString(mesmoCpf)))
                .andExpect(status().isConflict())
                .andExpect(jsonPath("$.status").value(409))
                .andExpect(jsonPath("$.caminho").value("/api/participantes"))
                .andExpect(jsonPath("$.mensagem").value("Já existe um participante cadastrado com o CPF " + cpf));
    }

    @Test
    void deveCriarRegistroETratarReenvioIdempotente() throws Exception {
        cadastrarParticipante();
        LeituraQrCodeDTO leitura = novaLeitura(UUID.randomUUID(), TipoAcao.ENTRADA, "celular-01");

        MvcResult primeiraResposta = mockMvc.perform(post("/api/registros")
                        .contentType(MediaType.APPLICATION_JSON)
                        .content(objectMapper.writeValueAsString(leitura)))
                .andExpect(status().isCreated())
                .andExpect(header().string("Location", startsWith("/api/registros/")))
                .andExpect(jsonPath("$.tipoAcao").value("ENTRADA"))
                .andExpect(jsonPath("$.dataHora").isNotEmpty())
                .andReturn();

        String registroId = JsonPath.read(primeiraResposta.getResponse().getContentAsString(), "$.id");

        mockMvc.perform(post("/api/registros")
                        .contentType(MediaType.APPLICATION_JSON)
                        .content(objectMapper.writeValueAsString(leitura)))
                .andExpect(status().isOk())
                .andExpect(header().string("X-Idempotent-Replay", "true"))
                .andExpect(jsonPath("$.id").value(registroId));
    }

    @Test
    void deveRetornarConflictParaDuploBipComNovaLeitura() throws Exception {
        cadastrarParticipante();
        LeituraQrCodeDTO primeira = novaLeitura(UUID.randomUUID(), TipoAcao.ENTRADA, "celular-01");
        LeituraQrCodeDTO segunda = novaLeitura(UUID.randomUUID(), TipoAcao.ENTRADA, "celular-02");
        registrarComSucesso(primeira);

        mockMvc.perform(post("/api/registros")
                        .contentType(MediaType.APPLICATION_JSON)
                        .content(objectMapper.writeValueAsString(segunda)))
                .andExpect(status().isConflict())
                .andExpect(jsonPath("$.status").value(409))
                .andExpect(jsonPath("$.mensagem").value(
                        "A mesma ação já foi registrada para o participante nos últimos 10 segundos"
                ));
    }

    @Test
    void deveRetornarConflictQuandoLeituraIdForReutilizadoComOutroPayload() throws Exception {
        cadastrarParticipante();
        UUID leituraId = UUID.randomUUID();
        LeituraQrCodeDTO primeira = novaLeitura(leituraId, TipoAcao.ENTRADA, "celular-01");
        LeituraQrCodeDTO alterada = novaLeitura(leituraId, TipoAcao.ENTRADA, "celular-02");
        registrarComSucesso(primeira);

        mockMvc.perform(post("/api/registros")
                        .contentType(MediaType.APPLICATION_JSON)
                        .content(objectMapper.writeValueAsString(alterada)))
                .andExpect(status().isConflict())
                .andExpect(jsonPath("$.status").value(409));
    }

    @Test
    void deveRetornarUnprocessableContentParaTransicaoInvalida() throws Exception {
        cadastrarParticipante();
        LeituraQrCodeDTO leitura = novaLeitura(UUID.randomUUID(), TipoAcao.SAIDA, "celular-01");

        mockMvc.perform(post("/api/registros")
                        .contentType(MediaType.APPLICATION_JSON)
                        .content(objectMapper.writeValueAsString(leitura)))
                .andExpect(status().isUnprocessableContent())
                .andExpect(jsonPath("$.status").value(422));
    }

    @Test
    void deveRetornarNotFoundParaParticipanteInexistente() throws Exception {
        LeituraQrCodeDTO leitura = new LeituraQrCodeDTO(
                UUID.randomUUID(),
                UUID.randomUUID(),
                TipoAcao.ENTRADA,
                "celular-01",
                OffsetDateTime.now(ZoneOffset.UTC).minusSeconds(1)
        );

        mockMvc.perform(post("/api/registros")
                        .contentType(MediaType.APPLICATION_JSON)
                        .content(objectMapper.writeValueAsString(leitura)))
                .andExpect(status().isNotFound())
                .andExpect(jsonPath("$.status").value(404));
    }

    private ParticipanteRequestDTO novoParticipante() {
        return new ParticipanteRequestDTO("Participante API", cpf, UUID.randomUUID());
    }

    private void cadastrarParticipante() throws Exception {
        MvcResult resultado = mockMvc.perform(post("/api/participantes")
                        .contentType(MediaType.APPLICATION_JSON)
                        .content(objectMapper.writeValueAsString(novoParticipante())))
                .andExpect(status().isCreated())
                .andReturn();
        participanteId = extrairParticipanteId(resultado);
    }

    private void registrarComSucesso(LeituraQrCodeDTO leitura) throws Exception {
        mockMvc.perform(post("/api/registros")
                        .contentType(MediaType.APPLICATION_JSON)
                        .content(objectMapper.writeValueAsString(leitura)))
                .andExpect(status().isCreated());
    }

    private LeituraQrCodeDTO novaLeitura(UUID leituraId, TipoAcao tipoAcao, String dispositivoId) {
        return new LeituraQrCodeDTO(
                leituraId,
                participanteId,
                tipoAcao,
                dispositivoId,
                OffsetDateTime.now(ZoneOffset.UTC).minusSeconds(1)
        );
    }

    private UUID extrairParticipanteId(MvcResult resultado) throws Exception {
        String id = JsonPath.read(resultado.getResponse().getContentAsString(), "$.id");
        return UUID.fromString(id);
    }

    private int calcularDigitoCpf(String digitos, int pesoInicial) {
        int soma = 0;
        for (int indice = 0; indice < digitos.length(); indice++) {
            soma += Character.digit(digitos.charAt(indice), 10) * (pesoInicial - indice);
        }
        int resto = soma % 11;
        return resto < 2 ? 0 : 11 - resto;
    }
}
