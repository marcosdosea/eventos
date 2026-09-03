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

    boolean existsByCpf(Cpf cpf);

    Optional<Participante> findByCpf(Cpf cpf);

    List<Participante> findAllByCpfIn(Collection<Cpf> cpfs);

    Optional<Participante> findByCadastroId(UUID cadastroId);

    @Lock(LockModeType.PESSIMISTIC_WRITE)
    @Query("select participante from Participante participante where participante.id = :participanteId")
    Optional<Participante> findByIdForUpdate(@Param("participanteId") UUID participanteId);
}
