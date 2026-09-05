package com.itatechjr.bipou.repository;

import com.itatechjr.bipou.model.entity.Registro;
import org.springframework.data.jpa.repository.JpaRepository;

import java.util.Optional;
import java.util.UUID;

public interface RegistroRepository extends JpaRepository<Registro, UUID> {

    Optional<Registro> findByLeituraId(UUID leituraId);

    Optional<Registro> findTopByParticipanteIdOrderByDataHoraDesc(UUID participanteId);
}
