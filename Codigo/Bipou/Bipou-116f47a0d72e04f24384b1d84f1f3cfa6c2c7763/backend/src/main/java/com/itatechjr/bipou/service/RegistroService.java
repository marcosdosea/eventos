package com.itatechjr.bipou.service;

import com.itatechjr.bipou.dto.LeituraQrCodeDTO;
import com.itatechjr.bipou.dto.RegistroResponseDTO;
import com.itatechjr.bipou.dto.RegistroResultadoDTO;

import java.util.List;

public interface RegistroService {

    RegistroResultadoDTO registrar(LeituraQrCodeDTO leitura);

    List<RegistroResponseDTO> listar(Long eventoId, Long subeventoId);

    List<RegistroResponseDTO> listar(Long eventoId, Long subeventoId, boolean apenasEventoGeral);
}
