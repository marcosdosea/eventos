final class ParticipanteResponse {
  const ParticipanteResponse({
    required this.id,
    required this.nome,
    required this.cpf,
    this.idPessoaBorala,
    this.eventoId,
    this.subeventoIds = const <int>[],
  });

  factory ParticipanteResponse.fromJson(Map<String, dynamic> json) {
    final rawSubIds = json['subeventoIds'];
    final subIds = <int>[];
    if (rawSubIds is List) {
      for (final item in rawSubIds) {
        if (item is num && item.toInt() > 0) {
          subIds.add(item.toInt());
        }
      }
    }

    final rawPid = json['idPessoaBorala'];
    final rawEventoId = json['eventoId'];

    return ParticipanteResponse(
      id: _uuid(json, 'id'),
      nome: _string(json, 'nome'),
      cpf: _string(json, 'cpf'),
      idPessoaBorala: rawPid is num && rawPid.toInt() > 0
          ? rawPid.toInt()
          : null,
      eventoId: rawEventoId is num && rawEventoId.toInt() > 0
          ? rawEventoId.toInt()
          : null,
      subeventoIds: List<int>.unmodifiable(subIds.toSet().toList()..sort()),
    );
  }

  final String id;
  final String nome;
  final String cpf;
  final int? idPessoaBorala;
  final int? eventoId;
  final List<int> subeventoIds;

  static String _uuid(Map<String, dynamic> json, String key) {
    final value = _string(json, key);
    if (!RegExp(
      r'^[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[1-8][0-9a-fA-F]{3}-[89abAB][0-9a-fA-F]{3}-[0-9a-fA-F]{12}$',
    ).hasMatch(value)) {
      throw FormatException('Campo "$key" nao contem um UUID valido.');
    }
    return value;
  }

  static String _string(Map<String, dynamic> json, String key) {
    final value = json[key];
    if (value is! String || value.isEmpty) {
      throw FormatException('Campo "$key" ausente ou invalido na resposta.');
    }
    return value;
  }
}
