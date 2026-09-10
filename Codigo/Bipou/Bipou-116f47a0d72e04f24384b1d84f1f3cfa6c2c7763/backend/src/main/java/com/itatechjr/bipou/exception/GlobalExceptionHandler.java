package com.itatechjr.bipou.exception;

import com.itatechjr.bipou.dto.ErroResponseDTO;
import jakarta.servlet.http.HttpServletRequest;
import lombok.RequiredArgsConstructor;
import org.springframework.http.HttpStatus;
import org.springframework.http.ResponseEntity;
import org.springframework.http.converter.HttpMessageNotReadableException;
import org.springframework.web.bind.MethodArgumentNotValidException;
import org.springframework.web.bind.annotation.ExceptionHandler;
import org.springframework.web.bind.annotation.RestControllerAdvice;

import java.time.Clock;
import java.time.OffsetDateTime;
import java.util.LinkedHashMap;
import java.util.Map;

@RestControllerAdvice
@RequiredArgsConstructor
public class GlobalExceptionHandler {

    private final Clock clock;

    @ExceptionHandler(CpfJaCadastradoException.class)
    public ResponseEntity<ErroResponseDTO> tratarCadastroDuplicado(
            RuntimeException exception,
            HttpServletRequest request
    ) {
        return criarResposta(HttpStatus.CONFLICT, exception.getMessage(), request, Map.of());
    }

    @ExceptionHandler(ParticipanteNaoEncontradoException.class)
    public ResponseEntity<ErroResponseDTO> tratarParticipanteNaoEncontrado(
            ParticipanteNaoEncontradoException exception,
            HttpServletRequest request
    ) {
        return criarResposta(HttpStatus.NOT_FOUND, exception.getMessage(), request, Map.of());
    }

    @ExceptionHandler({
            RegistroDuplicadoException.class,
            IdempotenciaConflitanteException.class,
            CadastroIdReutilizadoException.class
    })
    public ResponseEntity<ErroResponseDTO> tratarConflito(
            RuntimeException exception,
            HttpServletRequest request
    ) {
        return criarResposta(HttpStatus.CONFLICT, exception.getMessage(), request, Map.of());
    }

    @ExceptionHandler(TransicaoAcaoInvalidaException.class)
    public ResponseEntity<ErroResponseDTO> tratarTransicaoInvalida(
            TransicaoAcaoInvalidaException exception,
            HttpServletRequest request
    ) {
        return criarResposta(HttpStatus.UNPROCESSABLE_CONTENT, exception.getMessage(), request, Map.of());
    }

    @ExceptionHandler(MethodArgumentNotValidException.class)
    public ResponseEntity<ErroResponseDTO> tratarValidacao(
            MethodArgumentNotValidException exception,
            HttpServletRequest request
    ) {
        Map<String, String> campos = new LinkedHashMap<>();
        exception.getBindingResult().getFieldErrors().forEach(erro ->
                campos.putIfAbsent(erro.getField(), erro.getDefaultMessage())
        );

        return criarResposta(HttpStatus.BAD_REQUEST, "Existem campos inválidos na requisição", request, campos);
    }

    @ExceptionHandler(HttpMessageNotReadableException.class)
    public ResponseEntity<ErroResponseDTO> tratarCorpoInvalido(
            HttpMessageNotReadableException exception,
            HttpServletRequest request
    ) {
        return criarResposta(HttpStatus.BAD_REQUEST, "O corpo da requisição está inválido", request, Map.of());
    }

    @ExceptionHandler(IllegalArgumentException.class)
    public ResponseEntity<ErroResponseDTO> tratarArgumentoInvalido(
            IllegalArgumentException exception,
            HttpServletRequest request
    ) {
        return criarResposta(HttpStatus.BAD_REQUEST, exception.getMessage(), request, Map.of());
    }

    private ResponseEntity<ErroResponseDTO> criarResposta(
            HttpStatus status,
            String mensagem,
            HttpServletRequest request,
            Map<String, String> campos
    ) {
        ErroResponseDTO erro = new ErroResponseDTO(
                OffsetDateTime.now(clock),
                status.value(),
                status.name(),
                mensagem,
                request.getRequestURI(),
                campos
        );

        return ResponseEntity.status(status).body(erro);
    }
}
