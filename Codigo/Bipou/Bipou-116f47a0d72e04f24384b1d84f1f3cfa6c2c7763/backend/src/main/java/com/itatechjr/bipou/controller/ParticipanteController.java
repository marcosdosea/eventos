package com.itatechjr.bipou.controller;

import com.itatechjr.bipou.dto.ConsultaParticipanteCpfDTO;
import com.itatechjr.bipou.dto.ParticipanteRequestDTO;
import com.itatechjr.bipou.dto.ParticipanteResponseDTO;
import com.itatechjr.bipou.dto.ParticipantesLoteRequestDTO;
import com.itatechjr.bipou.service.ParticipanteService;
import jakarta.validation.Valid;
import lombok.RequiredArgsConstructor;
import org.springframework.http.ResponseEntity;
import org.springframework.web.bind.annotation.GetMapping;
import org.springframework.web.bind.annotation.PostMapping;
import org.springframework.web.bind.annotation.RequestBody;
import org.springframework.web.bind.annotation.RequestMapping;
import org.springframework.web.bind.annotation.RestController;

import java.net.URI;
import java.util.List;

@RestController
@RequestMapping("/api/participantes")
@RequiredArgsConstructor
public class ParticipanteController {

    private final ParticipanteService participanteService;

    @GetMapping
    public List<ParticipanteResponseDTO> listar() {
        return participanteService.listar();
    }

    @PostMapping
    public ResponseEntity<ParticipanteResponseDTO> cadastrar(
            @Valid @RequestBody ParticipanteRequestDTO participante
    ) {
        ParticipanteResponseDTO participanteCriado = participanteService.cadastrar(participante);
        URI localizacao = URI.create("/api/participantes/" + participanteCriado.id());

        return ResponseEntity.created(localizacao).body(participanteCriado);
    }

    @PostMapping("/lote")
    public ResponseEntity<List<ParticipanteResponseDTO>> cadastrarEmLote(
            @Valid @RequestBody ParticipantesLoteRequestDTO lote
    ) {
        List<ParticipanteResponseDTO> participantesCriados =
                participanteService.cadastrarEmLote(lote.participantes());

        return ResponseEntity.status(201).body(participantesCriados);
    }

    @PostMapping("/consultar-por-cpf")
    public ParticipanteResponseDTO buscarPorCpf(
            @Valid @RequestBody ConsultaParticipanteCpfDTO consulta
    ) {
        return participanteService.buscarPorCpf(consulta.cpf());
    }
}
