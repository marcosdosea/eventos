package com.itatechjr.bipou.controller;

import com.itatechjr.bipou.dto.LeituraQrCodeDTO;
import com.itatechjr.bipou.dto.RegistroResponseDTO;
import com.itatechjr.bipou.dto.RegistroResultadoDTO;
import com.itatechjr.bipou.service.RegistroService;
import jakarta.validation.Valid;
import lombok.RequiredArgsConstructor;
import org.springframework.http.ResponseEntity;
import org.springframework.web.bind.annotation.PostMapping;
import org.springframework.web.bind.annotation.RequestBody;
import org.springframework.web.bind.annotation.RequestMapping;
import org.springframework.web.bind.annotation.RestController;

import java.net.URI;

@RestController
@RequestMapping("/api/registros")
@RequiredArgsConstructor
public class RegistroController {

    private final RegistroService registroService;

    @PostMapping
    public ResponseEntity<RegistroResponseDTO> registrar(
            @Valid @RequestBody LeituraQrCodeDTO leitura
    ) {
        RegistroResultadoDTO resultado = registroService.registrar(leitura);
        RegistroResponseDTO registro = resultado.registro();
        URI localizacao = URI.create("/api/registros/" + registro.id());

        if (!resultado.criado()) {
            return ResponseEntity.ok()
                    .location(localizacao)
                    .header("X-Idempotent-Replay", "true")
                    .body(registro);
        }

        return ResponseEntity.created(localizacao).body(registro);
    }
}
