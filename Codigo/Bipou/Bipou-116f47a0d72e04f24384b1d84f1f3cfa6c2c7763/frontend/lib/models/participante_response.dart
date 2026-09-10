final class ParticipanteResponse {
  const ParticipanteResponse({
    required this.id,
    required this.nome,
    required this.cpf,
  });

  factory ParticipanteResponse.fromJson(Map<String, dynamic> json) {
    return ParticipanteResponse(
      id: _uuid(json, 'id'),
      nome: _string(json, 'nome'),
      cpf: _string(json, 'cpf'),
    );
  }

  final String id;
  final String nome;
  final String cpf;

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
