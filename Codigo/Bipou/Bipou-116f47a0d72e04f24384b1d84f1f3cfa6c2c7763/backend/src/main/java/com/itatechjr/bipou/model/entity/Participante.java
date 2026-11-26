package com.itatechjr.bipou.model.entity;

import com.itatechjr.bipou.model.valueobject.Cpf;
import jakarta.persistence.CollectionTable;
import jakarta.persistence.Column;
import jakarta.persistence.ElementCollection;
import jakarta.persistence.Entity;
import jakarta.persistence.FetchType;
import jakarta.persistence.GeneratedValue;
import jakarta.persistence.GenerationType;
import jakarta.persistence.Id;
import jakarta.persistence.JoinColumn;
import jakarta.persistence.Table;
import jakarta.persistence.UniqueConstraint;
import lombok.AllArgsConstructor;
import lombok.Builder;
import lombok.Getter;
import lombok.NoArgsConstructor;
import lombok.Setter;

import java.util.LinkedHashSet;
import java.util.Set;
import java.util.UUID;

@Getter
@Setter
@Builder
@NoArgsConstructor
@AllArgsConstructor
@Entity
@Table(
        name = "participante",
        uniqueConstraints = {
                @UniqueConstraint(name = "uk_participante_evento_cpf", columnNames = {"evento_id", "cpf"})
        }
)
public class Participante {

    @Id
    @GeneratedValue(strategy = GenerationType.UUID)
    private UUID id;

    @Column(nullable = false, length = 150)
    private String nome;

    @Column(nullable = false, length = 11)
    private Cpf cpf;

    @Column(length = 254)
    private String email;

    @Column(name = "cadastro_id", unique = true)
    private UUID cadastroId;

    @Column(name = "id_pessoa_borala")
    private Long idPessoaBorala;

    @Column(name = "evento_id", nullable = false)
    private Long eventoId;

    @Builder.Default
    @ElementCollection(fetch = FetchType.EAGER)
    @CollectionTable(name = "participante_subevento", joinColumns = @JoinColumn(name = "participante_id"))
    @Column(name = "subevento_id", nullable = false)
    private Set<Long> subeventoIds = new LinkedHashSet<>();

}
