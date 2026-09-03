import 'dart:typed_data';

import 'package:share_plus/share_plus.dart';

abstract interface class QrCodeShareGateway {
  Future<void> share({
    required Uint8List pngBytes,
    required String participanteNome,
  });
}

typedef QrImageShare =
    Future<void> Function({
      required Uint8List pngBytes,
      required String fileName,
      required String participanteNome,
    });

final class QrCodeShareService implements QrCodeShareGateway {
  QrCodeShareService({QrImageShare? shareImage})
    : _shareImage = shareImage ?? _shareQrImage;

  final QrImageShare _shareImage;

  @override
  Future<void> share({
    required Uint8List pngBytes,
    required String participanteNome,
  }) async {
    if (pngBytes.isEmpty) {
      throw const QrCodeShareException(
        'Nao foi possivel gerar a imagem do QR Code.',
      );
    }

    try {
      await _shareImage(
        pngBytes: pngBytes,
        fileName: _fileName(participanteNome),
        participanteNome: participanteNome,
      );
    } on Object catch (error, stackTrace) {
      if (error is QrCodeShareException) {
        rethrow;
      }
      Error.throwWithStackTrace(
        QrCodeShareException(
          'Nao foi possivel abrir o compartilhamento do QR Code.',
          error,
        ),
        stackTrace,
      );
    }
  }

  String _fileName(String participanteNome) {
    final safeName = participanteNome
        .trim()
        .toLowerCase()
        .replaceAll(RegExp(r'[^a-z0-9]+'), '-')
        .replaceAll(RegExp(r'^-+|-+$'), '');
    final suffix = safeName.isEmpty ? 'participante' : safeName;
    return 'credencial-bipou-$suffix.png';
  }
}

Future<void> _shareQrImage({
  required Uint8List pngBytes,
  required String fileName,
  required String participanteNome,
}) async {
  await SharePlus.instance.share(
    ShareParams(
      files: <XFile>[XFile.fromData(pngBytes, mimeType: 'image/png')],
      fileNameOverrides: <String>[fileName],
      title: 'Compartilhar credencial Bipou',
      subject: 'Credencial de $participanteNome',
      text: 'Credencial Bipou de $participanteNome.',
    ),
  );
}

final class QrCodeShareException implements Exception {
  const QrCodeShareException(this.message, [this.cause]);

  final String message;
  final Object? cause;

  @override
  String toString() => message;
}
