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
import java.util.Set;
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
        Participante participante = participanteRepository.saveAndFlush(novoParticipante(cpf, 15L));
        entityManager.clear();

        Participante encontrado = participanteRepository.findByIdForUpdate(participante.getId()).orElseThrow();

        assertThat(encontrado.getId()).isEqualTo(participante.getId());
        assertThat(encontrado.getNome()).isEqualTo("Participante Teste");
        assertThat(encontrado.getCadastroId()).isNotNull();
        assertThat(encontrado.getEventoId()).isEqualTo(15L);
        assertThat(encontrado.getSubeventoIds()).containsExactly(42L);
        assertThat(participanteRepository.existsByEventoIdAndCpf(15L, new Cpf(cpf))).isTrue();
    }

    @Test
    void deveImpedirCpfDuplicadoNoMesmoEventoEPermitirEmEventosDiferentes() {
        String cpf = cpfAleatorio();
        participanteRepository.saveAndFlush(novoParticipante(cpf, 15L));
        entityManager.clear();

        Participante outroEvento = novoParticipante(cpf, 16L);
        outroEvento.setIdPessoaBorala(1042L);
        participanteRepository.saveAndFlush(outroEvento);
        entityManager.clear();

        Participante duplicado = novoParticipante(cpf, 15L);
        duplicado.setNome("Outro Participante");

        assertThatThrownBy(() -> participanteRepository.saveAndFlush(duplicado))
                .isInstanceOf(DataIntegrityViolationException.class);
    }

    @Test
    void deveBuscarUltimoRegistroPorContextoEPreservarInstanteComOffset() {
        Participante participante = participanteRepository.saveAndFlush(novoParticipante(cpfAleatorio(), 15L));
        OffsetDateTime entrada = OffsetDateTime.of(2026, 8, 22, 9, 0, 0, 123_456_000,
                ZoneOffset.ofHours(-3));
        OffsetDateTime entradaCurso = entrada.plusMinutes(10);
        OffsetDateTime saida = entrada.plusHours(8);

        registroRepository.save(Registro.builder()
                .leituraId(UUID.randomUUID())
                .participante(participante)
                .eventoId(15L)
                .subeventoId(null)
                .tipoAcao(TipoAcao.ENTRADA)
                .dataHora(entrada)
                .dataHoraLidaNoCelular(entrada)
                .dispositivoId("celular-01")
                .operadorUsuario("operador.geral")
                .build());
        registroRepository.save(Registro.builder()
                .leituraId(UUID.randomUUID())
                .participante(participante)
                .eventoId(15L)
                .subeventoId(42L)
                .tipoAcao(TipoAcao.ENTRADA)
                .dataHora(entradaCurso)
                .dataHoraLidaNoCelular(entradaCurso)
                .dispositivoId("celular-02")
                .operadorUsuario("operador.curso")
                .build());
        registroRepository.saveAndFlush(Registro.builder()
                .leituraId(UUID.randomUUID())
                .participante(participante)
                .eventoId(15L)
                .subeventoId(null)
                .tipoAcao(TipoAcao.SAIDA)
                .dataHora(saida)
                .dataHoraLidaNoCelular(saida)
                .dispositivoId("celular-01")
                .operadorUsuario("operador.geral")
                .build());
        entityManager.clear();

        Registro ultimoEventoGeral = registroRepository
                .findTopByContextoOrderByDataHoraDesc(participante.getId(), 15L, null)
                .orElseThrow();
        Registro ultimoSubevento = registroRepository
                .findTopByContextoOrderByDataHoraDesc(participante.getId(), 15L, 42L)
                .orElseThrow();

        assertThat(ultimoEventoGeral.getTipoAcao()).isEqualTo(TipoAcao.SAIDA);
        assertThat(ultimoEventoGeral.getDataHora().toInstant()).isEqualTo(saida.toInstant());
        assertThat(ultimoSubevento.getTipoAcao()).isEqualTo(TipoAcao.ENTRADA);
        assertThat(ultimoSubevento.getSubeventoId()).isEqualTo(42L);
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

    private Participante novoParticipante(String cpf, Long eventoId) {
        return Participante.builder()
                .nome("Participante Teste")
                .cpf(new Cpf(cpf))
                .cadastroId(UUID.randomUUID())
                .idPessoaBorala(1042L)
                .eventoId(eventoId)
                .subeventoIds(Set.of(42L))
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
