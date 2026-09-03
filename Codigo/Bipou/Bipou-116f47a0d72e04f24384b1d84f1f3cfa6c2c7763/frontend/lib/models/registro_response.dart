import 'package:bipou_frontend/models/tipo_acao.dart';

final class RegistroResponse {
  const RegistroResponse({
    required this.id,
    required this.leituraId,
    required this.participanteId,
    required this.participanteNome,
    required this.tipoAcao,
    required this.dataHora,
    required this.dataHoraLidaNoCelular,
    required this.dispositivoId,
  });

  factory RegistroResponse.fromJson(Map<String, dynamic> json) {
    return RegistroResponse(
      id: _string(json, 'id'),
      leituraId: _string(json, 'leituraId'),
      participanteId: _string(json, 'participanteId'),
      participanteNome: _string(json, 'participanteNome'),
      tipoAcao: _tipoAcao(_string(json, 'tipoAcao')),
      dataHora: _dateTime(json, 'dataHora'),
      dataHoraLidaNoCelular: _dateTime(json, 'dataHoraLidaNoCelular'),
      dispositivoId: _string(json, 'dispositivoId'),
    );
  }

  final String id;
  final String leituraId;
  final String participanteId;
  final String participanteNome;
  final TipoAcao tipoAcao;
  final DateTime dataHora;
  final DateTime dataHoraLidaNoCelular;
  final String dispositivoId;

  static String _string(Map<String, dynamic> json, String key) {
    final value = json[key];
    if (value is! String || value.isEmpty) {
      throw FormatException('Campo "$key" ausente ou invalido na resposta.');
    }
    return value;
  }

  static DateTime _dateTime(Map<String, dynamic> json, String key) {
    final value = _string(json, key);
    final parsed = DateTime.tryParse(value);
    if (parsed == null) {
      throw FormatException('Campo "$key" nao contem uma data valida.');
    }
    return parsed;
  }

  static TipoAcao _tipoAcao(String value) {
    for (final tipo in TipoAcao.values) {
      if (tipo.apiValue == value) {
        return tipo;
      }
    }
    throw FormatException('Tipo de acao desconhecido: $value');
  }
}
