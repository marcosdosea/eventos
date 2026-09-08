import 'dart:convert';

final class ParticipanteQrPayload {
  ParticipanteQrPayload({required this.id, required String nome})
    : nome = nome.trim() {
    if (!_uuidPattern.hasMatch(id)) {
      throw ArgumentError.value(id, 'id', 'UUID invalido');
    }
    if (this.nome.isEmpty || this.nome.length > 150) {
      throw ArgumentError.value(
        nome,
        'nome',
        'O nome deve ter entre 1 e 150 caracteres',
      );
    }
  }

  factory ParticipanteQrPayload.fromRawJson(String? rawJson) {
    if (rawJson == null || rawJson.isEmpty) {
      throw const FormatException('QR Code vazio.');
    }

    final decoded = jsonDecode(rawJson);
    if (decoded is! Map<String, dynamic>) {
      throw const FormatException('QR Code invalido.');
    }

    final id = decoded['id'];
    final nome = decoded['nome'];
    if (id is! String || nome is! String) {
      throw const FormatException('QR Code invalido.');
    }

    try {
      return ParticipanteQrPayload(id: id, nome: nome);
    } on ArgumentError catch (error) {
      throw FormatException('QR Code invalido.', error);
    }
  }

  static final RegExp _uuidPattern = RegExp(
    r'^[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[1-8][0-9a-fA-F]{3}-[89abAB][0-9a-fA-F]{3}-[0-9a-fA-F]{12}$',
  );

  final String id;
  final String nome;

  Map<String, String> toJson() => <String, String>{'id': id, 'nome': nome};

  String toRawJson() => jsonEncode(toJson());
}
