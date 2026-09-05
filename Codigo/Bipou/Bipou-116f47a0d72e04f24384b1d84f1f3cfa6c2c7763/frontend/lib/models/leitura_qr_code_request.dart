import 'package:bipou_frontend/models/tipo_acao.dart';

final class LeituraQrCodeRequest {
  LeituraQrCodeRequest({
    required this.leituraId,
    required this.participanteId,
    required this.tipoAcao,
    required this.dispositivoId,
    required DateTime dataHoraLidaNoCelular,
  }) : dataHoraLidaNoCelular = dataHoraLidaNoCelular.toUtc() {
    _validarUuid(leituraId);
    _validarUuid(participanteId, fieldName: 'participanteId');
    _validarDispositivoId(dispositivoId);
  }

  final String leituraId;
  final String participanteId;
  final TipoAcao tipoAcao;
  final String dispositivoId;
  final DateTime dataHoraLidaNoCelular;

  Map<String, Object> toJson() => <String, Object>{
    'leituraId': leituraId,
    'participanteId': participanteId,
    'tipoAcao': tipoAcao.apiValue,
    'dispositivoId': dispositivoId,
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
