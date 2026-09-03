package com.itatechjr.bipou.service;

import com.itatechjr.bipou.dto.LeituraQrCodeDTO;
import com.itatechjr.bipou.dto.RegistroResultadoDTO;

public interface RegistroService {

    RegistroResultadoDTO registrar(LeituraQrCodeDTO leitura);
}
