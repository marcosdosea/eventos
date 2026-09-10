import 'package:bipou_frontend/models/participante_qr_payload.dart';
import 'package:flutter_test/flutter_test.dart';

void main() {
  const participanteId = '6ba7b810-9dad-41d1-80b4-00c04fd430c8';

  test('serializa e interpreta QR Code com id e nome', () {
    final original = ParticipanteQrPayload(
      id: participanteId,
      nome: 'Participante Teste',
    );

    final payload = ParticipanteQrPayload.fromRawJson(original.toRawJson());

    expect(payload.id, participanteId);
    expect(payload.nome, 'Participante Teste');
    expect(payload.toJson().keys, orderedEquals(<String>['id', 'nome']));
    expect(
      original.toRawJson(),
      '{"id":"$participanteId","nome":"Participante Teste"}',
    );
  });

  test('rejeita QR Code sem UUID valido ou sem nome', () {
    expect(
      () => ParticipanteQrPayload.fromRawJson(
        '{"id":"invalido","nome":"Participante"}',
      ),
      throwsFormatException,
    );
    expect(
      () => ParticipanteQrPayload.fromRawJson(
        '{"id":"$participanteId","nome":""}',
      ),
      throwsFormatException,
    );
  });
}
