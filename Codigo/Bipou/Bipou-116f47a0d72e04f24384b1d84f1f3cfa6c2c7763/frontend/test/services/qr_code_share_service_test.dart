import 'dart:typed_data';

import 'package:bipou_frontend/services/qr_code_share_service.dart';
import 'package:flutter_test/flutter_test.dart';

void main() {
  test('compartilha PNG com nome de arquivo seguro', () async {
    late Uint8List sharedBytes;
    late String sharedFileName;
    late String sharedName;
    final service = QrCodeShareService(
      shareImage:
          ({
            required pngBytes,
            required fileName,
            required participanteNome,
          }) async {
            sharedBytes = pngBytes;
            sharedFileName = fileName;
            sharedName = participanteNome;
          },
    );
    final bytes = Uint8List.fromList(<int>[137, 80, 78, 71]);

    await service.share(
      pngBytes: bytes,
      participanteNome: 'Ana Maria da Silva',
    );

    expect(sharedBytes, same(bytes));
    expect(sharedFileName, 'credencial-bipou-ana-maria-da-silva.png');
    expect(sharedName, 'Ana Maria da Silva');
  });

  test('nao tenta compartilhar imagem vazia', () async {
    var calls = 0;
    final service = QrCodeShareService(
      shareImage:
          ({
            required pngBytes,
            required fileName,
            required participanteNome,
          }) async {
            calls++;
          },
    );

    await expectLater(
      service.share(pngBytes: Uint8List(0), participanteNome: 'Participante'),
      throwsA(isA<QrCodeShareException>()),
    );
    expect(calls, 0);
  });

  test('converte falha nativa em erro de compartilhamento', () async {
    final service = QrCodeShareService(
      shareImage:
          ({
            required pngBytes,
            required fileName,
            required participanteNome,
          }) async {
            throw StateError('share indisponivel');
          },
    );

    await expectLater(
      service.share(
        pngBytes: Uint8List.fromList(<int>[1]),
        participanteNome: 'Participante',
      ),
      throwsA(
        isA<QrCodeShareException>().having(
          (error) => error.cause,
          'cause',
          isA<StateError>(),
        ),
      ),
    );
  });
}
