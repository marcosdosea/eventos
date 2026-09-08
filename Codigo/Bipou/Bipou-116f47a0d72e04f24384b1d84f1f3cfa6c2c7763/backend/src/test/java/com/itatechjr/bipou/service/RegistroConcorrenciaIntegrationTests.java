package com.itatechjr.bipou.service;

import com.itatechjr.bipou.dto.LeituraQrCodeDTO;
import com.itatechjr.bipou.dto.RegistroResultadoDTO;
import com.itatechjr.bipou.exception.RegistroDuplicadoException;
import com.itatechjr.bipou.model.entity.Participante;
import com.itatechjr.bipou.model.enums.TipoAcao;
import com.itatechjr.bipou.model.valueobject.Cpf;
import com.itatechjr.bipou.repository.ParticipanteRepository;
import org.junit.jupiter.api.AfterEach;
import org.junit.jupiter.api.BeforeEach;
import org.junit.jupiter.api.Test;
import org.springframework.beans.factory.annotation.Autowired;
import org.springframework.boot.test.context.SpringBootTest;
import org.springframework.jdbc.core.JdbcTemplate;

import java.time.OffsetDateTime;
import java.time.ZoneOffset;
import java.util.List;
import java.util.UUID;
import java.util.concurrent.Callable;
import java.util.concurrent.CountDownLatch;
import java.util.concurrent.ExecutorService;
import java.util.concurrent.Executors;
import java.util.concurrent.Future;
import java.util.concurrent.TimeUnit;

import static org.assertj.core.api.Assertions.assertThat;

@SpringBootTest
class RegistroConcorrenciaIntegrationTests {

    @Autowired
    private RegistroService registroService;

    @Autowired
    private ParticipanteRepository participanteRepository;

    @Autowired
    private JdbcTemplate jdbcTemplate;

    private Participante participante;

    @BeforeEach
    void cadastrarParticipante() {
        long cpfNumerico = Math.floorMod(UUID.randomUUID().getMostSignificantBits(), 100_000_000_000L);
        String cpf = String.format("%011d", cpfNumerico);
        participante = participanteRepository.saveAndFlush(Participante.builder()
                .nome("Participante Concorrência")
                .cpf(new Cpf(cpf))
                .build());
    }

    @AfterEach
    void removerDadosDoTeste() {
        jdbcTemplate.update("DELETE FROM registro WHERE participante_id = ?", participante.getId());
        jdbcTemplate.update("DELETE FROM participante WHERE id = ?", participante.getId());
    }

    @Test
    void deveImpedirDuasEntradasSimultaneasParaMesmoParticipante() throws Exception {
        LeituraQrCodeDTO primeiraLeitura = novaLeitura(UUID.randomUUID(), "celular-01");
        LeituraQrCodeDTO segundaLeitura = novaLeitura(UUID.randomUUID(), "celular-02");

        List<Object> resultados = executarSimultaneamente(primeiraLeitura, segundaLeitura);

        assertThat(resultados).filteredOn(RegistroResultadoDTO.class::isInstance).hasSize(1);
        assertThat(resultados).filteredOn(RegistroDuplicadoException.class::isInstance).hasSize(1);
        assertThat(quantidadeRegistros()).isEqualTo(1);
    }

    @Test
    void deveCriarUmaVezERetornarMesmoRegistroParaReenviosSimultaneos() throws Exception {
        UUID leituraId = UUID.randomUUID();
        LeituraQrCodeDTO leitura = novaLeitura(leituraId, "celular-01");

        List<Object> resultados = executarSimultaneamente(leitura, leitura);

        List<RegistroResultadoDTO> respostas = resultados.stream()
                .filter(RegistroResultadoDTO.class::isInstance)
                .map(RegistroResultadoDTO.class::cast)
                .toList();

        assertThat(respostas).hasSize(2);
        assertThat(respostas).extracting(RegistroResultadoDTO::criado)
                .containsExactlyInAnyOrder(true, false);
        assertThat(respostas).extracting(resultado -> resultado.registro().id())
                .containsOnly(respostas.getFirst().registro().id());
        assertThat(quantidadeRegistros()).isEqualTo(1);
    }

    private List<Object> executarSimultaneamente(
            LeituraQrCodeDTO primeiraLeitura,
            LeituraQrCodeDTO segundaLeitura
    ) throws Exception {
        ExecutorService executor = Executors.newFixedThreadPool(2);
        CountDownLatch prontas = new CountDownLatch(2);
        CountDownLatch iniciar = new CountDownLatch(1);

        try {
            Future<Object> primeira = executor.submit(tarefa(primeiraLeitura, prontas, iniciar));
            Future<Object> segunda = executor.submit(tarefa(segundaLeitura, prontas, iniciar));

            assertThat(prontas.await(5, TimeUnit.SECONDS)).isTrue();
            iniciar.countDown();

            return List.of(
                    primeira.get(10, TimeUnit.SECONDS),
                    segunda.get(10, TimeUnit.SECONDS)
            );
        } finally {
            executor.shutdownNow();
        }
    }

    private Callable<Object> tarefa(
            LeituraQrCodeDTO leitura,
            CountDownLatch prontas,
            CountDownLatch iniciar
    ) {
        return () -> {
            prontas.countDown();
            iniciar.await(5, TimeUnit.SECONDS);

            try {
                return registroService.registrar(leitura);
            } catch (RuntimeException exception) {
                return exception;
            }
        };
    }

    private LeituraQrCodeDTO novaLeitura(UUID leituraId, String dispositivoId) {
        return new LeituraQrCodeDTO(
                leituraId,
                participante.getId(),
                TipoAcao.ENTRADA,
                dispositivoId,
                OffsetDateTime.now(ZoneOffset.UTC)
        );
    }

    private int quantidadeRegistros() {
        return jdbcTemplate.queryForObject(
                "SELECT count(*) FROM registro WHERE participante_id = ?",
                Integer.class,
                participante.getId()
        );
    }
}
