package com.itatechjr.bipou.repository;

import com.itatechjr.bipou.model.entity.Registro;
import org.springframework.data.jpa.repository.JpaRepository;
import org.springframework.data.jpa.repository.Query;
import org.springframework.data.repository.query.Param;

import java.util.List;
import java.util.Optional;
import java.util.UUID;

public interface RegistroRepository extends JpaRepository<Registro, UUID> {

    Optional<Registro> findByLeituraId(UUID leituraId);

    Optional<Registro> findTopByParticipanteIdAndEventoIdAndSubeventoIdIsNullOrderByDataHoraDesc(
            UUID participanteId,
            Long eventoId
    );

    Optional<Registro> findTopByParticipanteIdAndEventoIdAndSubeventoIdOrderByDataHoraDesc(
            UUID participanteId,
            Long eventoId,
            Long subeventoId
    );

    default Optional<Registro> findTopByContextoOrderByDataHoraDesc(
            UUID participanteId,
            Long eventoId,
            Long subeventoId
    ) {
        if (subeventoId == null) {
            return findTopByParticipanteIdAndEventoIdAndSubeventoIdIsNullOrderByDataHoraDesc(
                    participanteId,
                    eventoId
            );
        }
        return findTopByParticipanteIdAndEventoIdAndSubeventoIdOrderByDataHoraDesc(
                participanteId,
                eventoId,
                subeventoId
        );
    }

    @Query("""
            SELECT registro
              FROM Registro registro
              JOIN FETCH registro.participante
             ORDER BY registro.dataHora DESC
            """)
    List<Registro> findAllComParticipanteOrderByDataHoraDesc();

    @Query("""
            SELECT registro
              FROM Registro registro
              JOIN FETCH registro.participante
             WHERE registro.eventoId = :eventoId
             ORDER BY registro.dataHora DESC
            """)
    List<Registro> findAllByEventoIdOrderByDataHoraDesc(@Param("eventoId") Long eventoId);

    @Query("""
            SELECT registro
              FROM Registro registro
              JOIN FETCH registro.participante
             WHERE registro.eventoId = :eventoId
               AND registro.subeventoId IS NULL
             ORDER BY registro.dataHora DESC
            """)
    List<Registro> findAllByEventoIdAndSubeventoIdIsNullOrderByDataHoraDesc(@Param("eventoId") Long eventoId);

    @Query("""
            SELECT registro
              FROM Registro registro
              JOIN FETCH registro.participante
             WHERE registro.eventoId = :eventoId
               AND registro.subeventoId = :subeventoId
             ORDER BY registro.dataHora DESC
            """)
    List<Registro> findAllByEventoIdAndSubeventoIdOrderByDataHoraDesc(
            @Param("eventoId") Long eventoId,
            @Param("subeventoId") Long subeventoId
    );

    @Query("""
            SELECT registro
              FROM Registro registro
              JOIN FETCH registro.participante
             WHERE registro.subeventoId = :subeventoId
             ORDER BY registro.dataHora DESC
            """)
    List<Registro> findAllBySubeventoIdOrderByDataHoraDesc(@Param("subeventoId") Long subeventoId);
}
