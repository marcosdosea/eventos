package com.itatechjr.bipou.repository;

import com.itatechjr.bipou.model.entity.Participante;
import com.itatechjr.bipou.model.valueobject.Cpf;
import jakarta.persistence.LockModeType;
import org.springframework.data.jpa.repository.JpaRepository;
import org.springframework.data.jpa.repository.Lock;
import org.springframework.data.jpa.repository.Query;
import org.springframework.data.repository.query.Param;

import java.util.List;
import java.util.Collection;
import java.util.Optional;
import java.util.UUID;

public interface ParticipanteRepository extends JpaRepository<Participante, UUID> {

    List<Participante> findAllByOrderByNomeAsc();

    List<Participante> findAllByEventoIdOrderByNomeAsc(Long eventoId);

    @Query("""
            SELECT DISTINCT participante
              FROM Participante participante
              JOIN participante.subeventoIds subeventoId
             WHERE participante.eventoId = :eventoId
               AND subeventoId = :subeventoId
             ORDER BY participante.nome ASC
            """)
    List<Participante> findAllByEventoIdAndSubeventoIdOrderByNomeAsc(
            @Param("eventoId") Long eventoId,
            @Param("subeventoId") Long subeventoId
    );

    @Query("""
            SELECT DISTINCT participante
              FROM Participante participante
              JOIN participante.subeventoIds subeventoId
             WHERE subeventoId = :subeventoId
             ORDER BY participante.nome ASC
            """)
    List<Participante> findAllBySubeventoIdOrderByNomeAsc(@Param("subeventoId") Long subeventoId);

    boolean existsByEventoIdAndCpf(Long eventoId, Cpf cpf);

    boolean existsByEventoIdAndIdPessoaBorala(Long eventoId, Long idPessoaBorala);

    Optional<Participante> findByEventoIdAndCpf(Long eventoId, Cpf cpf);

    Optional<Participante> findFirstByCpfOrderByEventoIdAsc(Cpf cpf);

    List<Participante> findAllByEventoIdAndCpfIn(Long eventoId, Collection<Cpf> cpfs);

    List<Participante> findAllByEventoIdAndIdPessoaBoralaIn(Long eventoId, Collection<Long> idsPessoaBorala);

    Optional<Participante> findByCadastroId(UUID cadastroId);

    @Lock(LockModeType.PESSIMISTIC_WRITE)
    @Query("select participante from Participante participante where participante.id = :participanteId")
    Optional<Participante> findByIdForUpdate(@Param("participanteId") UUID participanteId);
}
