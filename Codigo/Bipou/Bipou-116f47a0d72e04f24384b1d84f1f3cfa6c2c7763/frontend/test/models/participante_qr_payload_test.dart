import 'package:bipou_frontend/models/participante_qr_payload.dart';
import 'package:flutter_test/flutter_test.dart';

void main() {
  const participanteId = '6ba7b810-9dad-41d1-80b4-00c04fd430c8';
  const chaveTeste = 'chave_teste_123';

  group('ParticipanteQrPayload - Formato Legado', () {
    test('serializa e interpreta QR Code com id e nome', () {
      final original = ParticipanteQrPayload(
        id: participanteId,
        nome: 'Participante Teste',
      );

      final payload = ParticipanteQrPayload.fromRawJson(original.toRawJson());

      expect(payload.id, participanteId);
      expect(payload.nome, 'Participante Teste');
      expect(payload.isV1, isFalse);
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
  });

  group('ParticipanteQrPayload - Formato Unificado v1 (Boralá ⟷ Bipou)', () {
    test('calcula hash identico ao vetor golden do Borala (C#)', () {
      final hash = ParticipanteQrPayload.calcularHash(
        v: 1,
        pid: 1042,
        cpf: '12345678901',
        nome: 'Maria Santos',
        eid: 15,
        sid: [42, 43],
        secretKey: chaveTeste,
      );

      // Hash canônico idêntico calculado em C#: "58663eab"
      expect(hash, '58663eab');
    });

    test('serializa e interpreta payload v1 completo com sucesso (chave padrao e chave customizada)', () {
      // 1. Com chave padrão derivada do evento 15 ("borala:evento:15:secret" -> hash "0ec02205")
      final padrao = ParticipanteQrPayload.v1(
        pid: 1042,
        cpf: '12345678901',
        nome: 'Maria Santos',
        eid: 15,
        sid: [42, 43],
      );

      expect(padrao.isV1, isTrue);
      expect(padrao.hash, '0ec02205');

      final lidoPadrao = ParticipanteQrPayload.fromRawJson(padrao.toRawJson());
      expect(lidoPadrao.pid, 1042);
      expect(lidoPadrao.nome, 'Maria Santos');
      expect(lidoPadrao.eid, 15);
      expect(lidoPadrao.sid, [42, 43]);
      expect(lidoPadrao.hash, '0ec02205');
      expect(lidoPadrao.validarHash(), isTrue);

      // 2. Com chave customizada
      final original = ParticipanteQrPayload.v1(
        pid: 1042,
        cpf: '12345678901',
        nome: 'Maria Santos',
        eid: 15,
        sid: [42, 43],
        secretKey: chaveTeste,
      );

      expect(original.hash, '58663eab');

      final lidoValidado = ParticipanteQrPayload.fromRawJson(
        original.toRawJson(),
        secretKey: chaveTeste,
      );
      expect(lidoValidado.pid, 1042);
      expect(lidoValidado.validarHash(chaveTeste), isTrue);
    });

    test('rejeita QR Code com dados adulterados (hash mismatch) mesmo sem informar secretKey', () {
      const jsonAdulterado =
          '{"v":1,"pid":9999,"cpf":"12345678901","nome":"Maria Santos","eid":15,"sid":[42,43],"hash":"0ec02205"}';

      // Sem informar a chave, valida contra a chave padrão do evento 15 e lança FormatException
      expect(
        () => ParticipanteQrPayload.fromRawJson(jsonAdulterado),
        throwsFormatException,
      );

      // Passando secretKey customizada também lança FormatException
      expect(
        () => ParticipanteQrPayload.fromRawJson(
          jsonAdulterado,
          secretKey: chaveTeste,
        ),
        throwsFormatException,
      );
    });

    test('ordena e remove duplicatas de subeventos e sanitiza CPF na canonizacao', () {
      final hash1 = ParticipanteQrPayload.calcularHash(
        v: 1,
        pid: 1042,
        cpf: '123.456.789-01999',
        nome: 'Maria Santos',
        eid: 15,
        sid: [43, 42, 42],
        secretKey: chaveTeste,
      );

      final hash2 = ParticipanteQrPayload.calcularHash(
        v: 1,
        pid: 1042,
        cpf: '12345678901',
        nome: 'Maria Santos',
        eid: 15,
        sid: [42, 43],
        secretKey: chaveTeste,
      );

      expect(hash1, hash2);
      expect(hash1, '58663eab');
      expect(ParticipanteQrPayload.sanitizarCpf('123'), '00000000123');
      expect(ParticipanteQrPayload.sanitizarCpf(''), '00000000000');
    });

    test('interpreta formato textual legado do Borala por compatibilidade', () {
      const textoLegado = '[1042] [Maria Santos] [15] [42] [43]';
      final payload = ParticipanteQrPayload.fromRawJson(textoLegado);

      expect(payload.isV1, isTrue);
      expect(payload.pid, 1042);
      expect(payload.nome, 'Maria Santos');
      expect(payload.eid, 15);
      expect(payload.sid, [42, 43]);
      expect(payload.validarHash(), isTrue);
    });
  });
}
