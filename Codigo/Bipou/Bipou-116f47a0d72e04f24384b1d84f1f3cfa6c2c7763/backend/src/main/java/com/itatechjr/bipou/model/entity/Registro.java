package com.itatechjr.bipou.model.entity;

import com.itatechjr.bipou.model.enums.TipoAcao;
import jakarta.persistence.Column;
import jakarta.persistence.Entity;
import jakarta.persistence.EnumType;
import jakarta.persistence.Enumerated;
import jakarta.persistence.FetchType;
import jakarta.persistence.GeneratedValue;
import jakarta.persistence.GenerationType;
import jakarta.persistence.Id;
import jakarta.persistence.JoinColumn;
import jakarta.persistence.ManyToOne;
import jakarta.persistence.Table;
import lombok.AllArgsConstructor;
import lombok.Builder;
import lombok.Getter;
import lombok.NoArgsConstructor;
import lombok.Setter;

import java.time.OffsetDateTime;
import java.util.UUID;

@Getter
@Setter
@Builder
@NoArgsConstructor
@AllArgsConstructor
@Entity
@Table(name = "registro")
public class Registro {

    @Id
    @GeneratedValue(strategy = GenerationType.UUID)
    private UUID id;

    @Column(name = "leitura_id", nullable = false, unique = true, updatable = false)
    private UUID leituraId;

    @ManyToOne(fetch = FetchType.LAZY, optional = false)
    @JoinColumn(name = "participante_id", nullable = false)
    private Participante participante;

    @Enumerated(EnumType.STRING)
    @Column(name = "tipo_acao", nullable = false, length = 30)
    private TipoAcao tipoAcao;

    @Column(name = "data_hora", nullable = false)
    private OffsetDateTime dataHora;

    @Column(name = "data_hora_lida_no_celular", nullable = false)
    private OffsetDateTime dataHoraLidaNoCelular;

    @Column(name = "dispositivo_id", nullable = false, length = 100)
    private String dispositivoId;
}
