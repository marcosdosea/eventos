package com.itatechjr.bipou.repository;

import com.itatechjr.bipou.model.entity.Participante;
import com.itatechjr.bipou.model.entity.Registro;
import com.itatechjr.bipou.model.enums.TipoAcao;
import com.itatechjr.bipou.model.valueobject.Cpf;
import jakarta.persistence.EntityManager;
import org.junit.jupiter.api.Test;
import org.springframework.beans.factory.annotation.Autowired;
import org.springframework.boot.test.context.SpringBootTest;
import org.springframework.dao.DataIntegrityViolationException;
import org.springframework.jdbc.core.JdbcTemplate;
import org.springframework.transaction.annotation.Transactional;

import java.time.OffsetDateTime;
import java.time.ZoneOffset;
import java.util.UUID;

import static org.assertj.core.api.Assertions.assertThat;
import static org.assertj.core.api.Assertions.assertThatThrownBy;

@SpringBootTest
@Transactional
class PersistenceIntegrationTests {

    @Autowired
    private ParticipanteRepository participanteRepository;

    @Autowired
    private RegistroRepository registroRepository;

    @Autowired
    private EntityManager entityManager;

    @Autowired
    private JdbcTemplate jdbcTemplate;

    @Test
    void devePersistirParticipanteEBuscarPorIdComBloqueio() {
        String cpf = cpfAleatorio();
        Participante participante = participanteRepository.saveAndFlush(novoParticipante(cpf));
        entityManager.clear();

        Participante encontrado = participanteRepository.findByIdForUpdate(participante.getId()).orElseThrow();

        assertThat(encontrado.getId()).isEqualTo(participante.getId());
        assertThat(encontrado.getNome()).isEqualTo("Participante Teste");
        assertThat(encontrado.getCadastroId()).isNotNull();
        assertThat(participanteRepository.existsByCpf(new Cpf(cpf))).isTrue();
    }

    @Test
    void deveImpedirCpfDuplicadoNoBanco() {
        String cpf = cpfAleatorio();
        participanteRepository.saveAndFlush(novoParticipante(cpf));
        entityManager.clear();

        Participante duplicado = novoParticipante(cpf);
        duplicado.setNome("Outro Participante");

        assertThatThrownBy(() -> participanteRepository.saveAndFlush(duplicado))
                .isInstanceOf(DataIntegrityViolationException.class);
    }

    @Test
    void deveBuscarUltimoRegistroEPreservarInstanteComOffset() {
        Participante participante = participanteRepository.saveAndFlush(novoParticipante(cpfAleatorio()));
        OffsetDateTime entrada = OffsetDateTime.of(2026, 8, 22, 9, 0, 0, 123_456_000,
                ZoneOffset.ofHours(-3));
        OffsetDateTime saida = entrada.plusHours(8);

        registroRepository.save(Registro.builder()
                .leituraId(java.util.UUID.randomUUID())
                .participante(participante)
                .tipoAcao(TipoAcao.ENTRADA)
                .dataHora(entrada)
                .dataHoraLidaNoCelular(entrada)
                .dispositivoId("celular-01")
                .build());
        registroRepository.saveAndFlush(Registro.builder()
                .leituraId(java.util.UUID.randomUUID())
                .participante(participante)
                .tipoAcao(TipoAcao.SAIDA)
                .dataHora(saida)
                .dataHoraLidaNoCelular(saida)
                .dispositivoId("celular-02")
                .build());
        entityManager.clear();

        Registro ultimoRegistro = registroRepository
                .findTopByParticipanteIdOrderByDataHoraDesc(participante.getId())
                .orElseThrow();

        assertThat(ultimoRegistro.getTipoAcao()).isEqualTo(TipoAcao.SAIDA);
        assertThat(ultimoRegistro.getDataHora().toInstant()).isEqualTo(saida.toInstant());
        assertThat(ultimoRegistro.getDispositivoId()).isEqualTo("celular-02");
    }

    @Test
    void deveUsarTimestampComFusoHorarioNoPostgresql() {
        String tipoDaColuna = jdbcTemplate.queryForObject("""
                SELECT data_type
                  FROM information_schema.columns
                 WHERE table_schema = 'public'
                   AND table_name = 'registro'
                   AND column_name = 'data_hora'
                """, String.class);

        assertThat(tipoDaColuna).isEqualTo("timestamp with time zone");
    }

    private Participante novoParticipante(String cpf) {
        return Participante.builder()
                .nome("Participante Teste")
                .cpf(new Cpf(cpf))
                .cadastroId(UUID.randomUUID())
                .build();
    }

    private String cpfAleatorio() {
        long valor = Math.floorMod(UUID.randomUUID().getMostSignificantBits(), 1_000_000_000L);
        String primeirosDigitos = String.format("%09d", valor);
        int primeiroVerificador = calcularDigitoCpf(primeirosDigitos, 10);
        int segundoVerificador = calcularDigitoCpf(primeirosDigitos + primeiroVerificador, 11);
        return primeirosDigitos + primeiroVerificador + segundoVerificador;
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
