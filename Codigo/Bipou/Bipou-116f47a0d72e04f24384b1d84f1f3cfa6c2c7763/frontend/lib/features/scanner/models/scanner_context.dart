final class ScannerSessionContext {
  const ScannerSessionContext({
    required this.eventoNome,
    this.eventoId,
    this.subeventoId,
    this.subeventoNome,
    this.secretKey,
  });

  final int? eventoId;
  final String eventoNome;
  final int? subeventoId;
  final String? subeventoNome;
  final String? secretKey;

  bool get isSubevento => subeventoId != null;

  bool get isEventoGeral => subeventoId == null;

  String get destinoLabel {
    if (!isSubevento) {
      return 'Evento Geral';
    }
    final nome = subeventoNome?.trim();
    if (nome != null && nome.isNotEmpty) {
      return nome;
    }
    return 'Subevento #$subeventoId';
  }

  String get bannerLabel {
    final nomeEvento = eventoNome.trim().isEmpty
        ? (eventoId != null ? 'Evento #$eventoId' : 'Evento')
        : eventoNome.trim();
    if (isEventoGeral) {
      return '[Evento: $nomeEvento] » [Evento Geral]';
    }
    return '[Evento: $nomeEvento] » [Curso: $destinoLabel]';
  }

  ScannerSessionContext copyWith({
    int? eventoId,
    bool clearEventoId = false,
    String? eventoNome,
    int? subeventoId,
    bool clearSubevento = false,
    String? subeventoNome,
    String? secretKey,
  }) {
    return ScannerSessionContext(
      eventoId: clearEventoId ? null : (eventoId ?? this.eventoId),
      eventoNome: eventoNome ?? this.eventoNome,
      subeventoId: clearSubevento ? null : (subeventoId ?? this.subeventoId),
      subeventoNome: clearSubevento
          ? null
          : (subeventoNome ?? this.subeventoNome),
      secretKey: secretKey ?? this.secretKey,
    );
  }
}
