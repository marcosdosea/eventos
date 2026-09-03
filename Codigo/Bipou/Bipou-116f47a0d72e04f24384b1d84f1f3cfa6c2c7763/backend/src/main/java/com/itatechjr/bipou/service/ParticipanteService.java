package com.itatechjr.bipou.service;

import com.itatechjr.bipou.dto.ParticipanteRequestDTO;
import com.itatechjr.bipou.dto.ParticipanteResponseDTO;

import java.util.List;

public interface ParticipanteService {

    ParticipanteResponseDTO cadastrar(ParticipanteRequestDTO participante);

    List<ParticipanteResponseDTO> cadastrarEmLote(List<ParticipanteRequestDTO> participantes);

    ParticipanteResponseDTO buscarPorCpf(String cpf);

    List<ParticipanteResponseDTO> listar();
}
