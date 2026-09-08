final class AuditLogEntry {
  AuditLogEntry({
    required DateTime dataHora,
    required this.participanteId,
    required this.participanteNome,
    required this.tipoAcao,
    required this.dispositivoId,
  }) : dataHora = dataHora.toUtc() {
    if (participanteId != null) {
      _validarCampo(participanteId!, 'participanteId');
      if (!RegExp(
        r'^[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[1-8][0-9a-fA-F]{3}-[89abAB][0-9a-fA-F]{3}-[0-9a-fA-F]{12}$',
      ).hasMatch(participanteId!)) {
        throw ArgumentError.value(
          participanteId,
          'participanteId',
          'O identificador deve ser um UUID valido',
        );
      }
    }
    if (participanteNome.trim().isEmpty || participanteNome.length > 150) {
      throw ArgumentError.value(
        participanteNome,
        'participanteNome',
        'O nome deve ter entre 1 e 150 caracteres',
      );
    }
    _validarCampo(tipoAcao, 'tipoAcao');
    _validarCampo(dispositivoId, 'dispositivoId');
    if (dispositivoId.length > 100) {
      throw ArgumentError.value(
        dispositivoId,
        'dispositivoId',
        'O identificador deve ter no maximo 100 caracteres',
      );
    }
  }

  final DateTime dataHora;
  final String? participanteId;
  final String participanteNome;
  final String tipoAcao;
  final String dispositivoId;

  String toLine() {
    return '${dataHora.toIso8601String()};${participanteId ?? ''};'
        '${_escaparCampo(participanteNome)};$tipoAcao;$dispositivoId';
  }

  static String _escaparCampo(String value) {
    return value
        .replaceAll('%', '%25')
        .replaceAll(';', '%3B')
        .replaceAll('\r', '%0D')
        .replaceAll('\n', '%0A');
  }

  static void _validarCampo(String value, String nome) {
    if (value.trim().isEmpty ||
        value.contains(';') ||
        value.contains('\n') ||
        value.contains('\r')) {
      throw ArgumentError.value(
        value,
        nome,
        'O campo nao pode estar vazio nem conter ponto e virgula ou quebra de linha',
      );
    }
  }
}
