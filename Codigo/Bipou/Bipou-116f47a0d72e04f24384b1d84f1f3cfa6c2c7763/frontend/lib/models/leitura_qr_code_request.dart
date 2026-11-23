import 'package:bipou_frontend/models/tipo_acao.dart';

final class LeituraQrCodeRequest {
  LeituraQrCodeRequest({
    required this.leituraId,
    required this.participanteId,
    required this.tipoAcao,
    required this.dispositivoId,
    required DateTime dataHoraLidaNoCelular,
    this.eventoId,
    this.subeventoId,
    this.operadorUsuario,
  }) : dataHoraLidaNoCelular = dataHoraLidaNoCelular.toUtc() {
    _validarUuid(leituraId);
    _validarUuid(participanteId, fieldName: 'participanteId');
    _validarDispositivoId(dispositivoId);
    if (eventoId != null && eventoId! <= 0) {
      throw ArgumentError.value(eventoId, 'eventoId', 'Deve ser positivo');
    }
    if (subeventoId != null && subeventoId! <= 0) {
      throw ArgumentError.value(
        subeventoId,
        'subeventoId',
        'Deve ser positivo',
      );
    }
  }

  final String leituraId;
  final String participanteId;
  final TipoAcao tipoAcao;
  final String dispositivoId;
  final DateTime dataHoraLidaNoCelular;
  final int? eventoId;
  final int? subeventoId;
  final String? operadorUsuario;

  Map<String, Object> toJson() => <String, Object>{
    'leituraId': leituraId,
    'participanteId': participanteId,
    'eventoId': ?eventoId,
    'subeventoId': ?subeventoId,
    'tipoAcao': tipoAcao.apiValue,
    'dispositivoId': dispositivoId,
    if (operadorUsuario != null && operadorUsuario!.trim().isNotEmpty)
      'operadorUsuario': operadorUsuario!.trim(),
    'dataHoraLidaNoCelular': dataHoraLidaNoCelular.toIso8601String(),
  };

  static void _validarUuid(String value, {String fieldName = 'leituraId'}) {
    final uuid = RegExp(
      r'^[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[1-8][0-9a-fA-F]{3}-[89abAB][0-9a-fA-F]{3}-[0-9a-fA-F]{12}$',
    );
    if (!uuid.hasMatch(value)) {
      throw ArgumentError.value(value, fieldName, 'UUID invalido');
    }
  }

  static void _validarDispositivoId(String value) {
    if (value.isEmpty || value.length > 100) {
      throw ArgumentError.value(
        value,
        'dispositivoId',
        'O identificador deve ter entre 1 e 100 caracteres',
      );
    }
  }
}
