package com.itatechjr.bipou.mapper;

import com.itatechjr.bipou.dto.LeituraQrCodeDTO;
import com.itatechjr.bipou.dto.RegistroResponseDTO;
import com.itatechjr.bipou.model.entity.Participante;
import com.itatechjr.bipou.model.entity.Registro;
import org.springframework.stereotype.Component;

import java.time.OffsetDateTime;

@Component
public class RegistroMapper {

    public Registro toEntity(
            LeituraQrCodeDTO dto,
            Participante participante,
            OffsetDateTime dataHoraServidor
    ) {
        return Registro.builder()
                .leituraId(dto.leituraId())
                .participante(participante)
                .eventoId(dto.eventoId())
                .subeventoId(dto.subeventoId())
                .tipoAcao(dto.tipoAcao())
                .dataHora(dataHoraServidor)
                .dataHoraLidaNoCelular(dto.dataHoraLidaNoCelular())
                .dispositivoId(dto.dispositivoId().strip())
                .operadorUsuario(normalizarOperador(dto.operadorUsuario()))
                .build();
    }

    public RegistroResponseDTO toResponse(Registro registro) {
        return new RegistroResponseDTO(
                registro.getId(),
                registro.getLeituraId(),
                registro.getParticipante().getId(),
                registro.getParticipante().getNome(),
                registro.getEventoId(),
                registro.getSubeventoId(),
                registro.getTipoAcao(),
                registro.getDataHora(),
                registro.getDataHoraLidaNoCelular(),
                registro.getDispositivoId(),
                registro.getOperadorUsuario()
        );
    }

    public static String normalizarOperador(String operadorUsuario) {
        if (operadorUsuario == null) {
            return null;
        }
        String tratado = operadorUsuario.strip();
        return tratado.isEmpty() ? null : tratado;
    }
}
