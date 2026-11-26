final class PortariaSession {
  const PortariaSession({
    required this.eventoId,
    required this.eventoNome,
    this.subeventoId,
    this.subeventoNome,
    required this.totalParticipantesCarregados,
    required this.sincronizadoEm,
    this.boralaBaseUrl,
    this.operadorUsuario,
    this.accessToken,
    this.refreshToken,
    this.expiresAt,
  });

  factory PortariaSession.fromJson(Map<String, dynamic> json) {
    final eventoId = (json['eventoId'] as num?)?.toInt();
    final eventoNome = json['eventoNome'];
    if (eventoId == null ||
        eventoId <= 0 ||
        eventoNome is! String ||
        eventoNome.trim().isEmpty) {
      throw const FormatException('Sessao de portaria invalida.');
    }

    final rawSubId = json['subeventoId'];
    final subeventoId = rawSubId is num && rawSubId.toInt() > 0
        ? rawSubId.toInt()
        : null;
    final rawSubNome = json['subeventoNome'];
    final subeventoNome = rawSubNome is String && rawSubNome.trim().isNotEmpty
        ? rawSubNome.trim()
        : null;

    final total = (json['totalParticipantesCarregados'] as num?)?.toInt() ?? 0;
    final rawSync = json['sincronizadoEm'];
    final sincronizadoEm = rawSync is String
        ? (DateTime.tryParse(rawSync) ?? DateTime.now().toUtc())
        : DateTime.now().toUtc();

    return PortariaSession(
      eventoId: eventoId,
      eventoNome: eventoNome.trim(),
      subeventoId: subeventoId,
      subeventoNome: subeventoNome,
      totalParticipantesCarregados: total < 0 ? 0 : total,
      sincronizadoEm: sincronizadoEm,
      boralaBaseUrl: json['boralaBaseUrl'] is String
          ? json['boralaBaseUrl'] as String
          : null,
      operadorUsuario: json['operadorUsuario'] is String
          ? json['operadorUsuario'] as String
          : null,
      accessToken: json['accessToken'] is String
          ? json['accessToken'] as String
          : null,
      refreshToken: json['refreshToken'] is String
          ? json['refreshToken'] as String
          : null,
      expiresAt: json['expiresAt'] is String
          ? DateTime.tryParse(json['expiresAt'] as String)
          : null,
    );
  }

  final int eventoId;
  final String eventoNome;
  final int? subeventoId;
  final String? subeventoNome;
  final int totalParticipantesCarregados;
  final DateTime sincronizadoEm;
  final String? boralaBaseUrl;
  final String? operadorUsuario;
  final String? accessToken;
  final String? refreshToken;
  final DateTime? expiresAt;

  bool isTokenExpirandoOuExpirado({
    DateTime? agora,
    Duration margem = const Duration(minutes: 5),
  }) {
    final exp = expiresAt;
    if (exp == null) {
      return false;
    }
    final referencia = (agora ?? DateTime.now()).toUtc();
    return !referencia.isBefore(exp.toUtc().subtract(margem));
  }

  bool get isEventoGeral => subeventoId == null;

  String get destinoLabel =>
      isEventoGeral ? 'Evento Geral' : (subeventoNome ?? 'Subevento #$subeventoId');

  String get contextoBanner {
    if (isEventoGeral) {
      return '[Evento: $eventoNome] » [Evento Geral]';
    }
    return '[Evento: $eventoNome] » [Curso: $destinoLabel]';
  }

  String get resumoParticipantes {
    final count = totalParticipantesCarregados;
    final unidade = count == 1 ? 'participante' : 'participantes';
    final carregado = count == 1 ? 'carregado' : 'carregados';
    if (isEventoGeral) {
      return '$count $unidade do Evento Geral $carregado localmente';
    }
    return '$count $unidade de $destinoLabel $carregado localmente';
  }

  PortariaSession copyWith({
    int? eventoId,
    String? eventoNome,
    int? subeventoId,
    bool clearSubevento = false,
    String? subeventoNome,
    int? totalParticipantesCarregados,
    DateTime? sincronizadoEm,
    String? boralaBaseUrl,
    String? operadorUsuario,
    String? accessToken,
    String? refreshToken,
    DateTime? expiresAt,
  }) {
    return PortariaSession(
      eventoId: eventoId ?? this.eventoId,
      eventoNome: eventoNome ?? this.eventoNome,
      subeventoId: clearSubevento ? null : (subeventoId ?? this.subeventoId),
      subeventoNome: clearSubevento
          ? null
          : (subeventoNome ?? this.subeventoNome),
      totalParticipantesCarregados:
          totalParticipantesCarregados ?? this.totalParticipantesCarregados,
      sincronizadoEm: sincronizadoEm ?? this.sincronizadoEm,
      boralaBaseUrl: boralaBaseUrl ?? this.boralaBaseUrl,
      operadorUsuario: operadorUsuario ?? this.operadorUsuario,
      accessToken: accessToken ?? this.accessToken,
      refreshToken: refreshToken ?? this.refreshToken,
      expiresAt: expiresAt ?? this.expiresAt,
    );
  }

  Map<String, Object?> toJson() => <String, Object?>{
    'eventoId': eventoId,
    'eventoNome': eventoNome,
    if (subeventoId != null) 'subeventoId': subeventoId,
    if (subeventoNome != null) 'subeventoNome': subeventoNome,
    'totalParticipantesCarregados': totalParticipantesCarregados,
    'sincronizadoEm': sincronizadoEm.toUtc().toIso8601String(),
    if (boralaBaseUrl != null) 'boralaBaseUrl': boralaBaseUrl,
    if (operadorUsuario != null) 'operadorUsuario': operadorUsuario,
    if (accessToken != null) 'accessToken': accessToken,
    if (refreshToken != null) 'refreshToken': refreshToken,
    if (expiresAt != null) 'expiresAt': expiresAt!.toUtc().toIso8601String(),
  };
}
