import 'dart:typed_data';

import 'package:bipou_frontend/features/participants/views/participant_qr_screen.dart';
import 'package:bipou_frontend/models/participante_response.dart';
import 'package:bipou_frontend/services/qr_code_share_service.dart';
import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:qr_flutter/qr_flutter.dart';

void main() {
  const participante = ParticipanteResponse(
    id: '6ba7b810-9dad-41d1-80b4-00c04fd430c8',
    nome: 'Ana Participante',
    cpf: '52998224725',
  );

  testWidgets('exibe nome no cartao padronizado e QR Code', (tester) async {
    await tester.pumpWidget(
      MaterialApp(
        home: ParticipantQrScreen(
          participante: participante,
          qrCodeShare: _QrCodeShareSpy(),
        ),
      ),
    );

    expect(find.text('BIPOU'), findsNothing);
    expect(find.byKey(const Key('logo-ufs')), findsOneWidget);
    expect(find.byKey(const Key('marca-dsi')), findsOneWidget);
    expect(find.text('Ana Participante'), findsOneWidget);
    expect(find.byType(QrImageView), findsOneWidget);
    expect(find.text('Compartilhar QR Code'), findsOneWidget);
  });

  testWidgets('gera PNG do cartao antes de compartilhar', (tester) async {
    final share = _QrCodeShareSpy();
    await tester.pumpWidget(
      MaterialApp(
        home: ParticipantQrScreen(
          participante: participante,
          qrCodeShare: share,
        ),
      ),
    );
    await tester.pumpAndSettle();

    final button = find.text('Compartilhar QR Code');
    await tester.ensureVisible(button);
    await tester.tap(button);
    await tester.pump();
    await tester.runAsync(() async {
      for (var attempt = 0; attempt < 20 && share.calls == 0; attempt++) {
        await Future<void>.delayed(const Duration(milliseconds: 50));
      }
    });
    await tester.pump();

    expect(share.calls, 1);
    expect(share.participanteNome, participante.nome);
    expect(share.pngBytes, isNotNull);
    expect(share.pngBytes!.take(8), <int>[137, 80, 78, 71, 13, 10, 26, 10]);
  });

  testWidgets('mantem acoes acessiveis em tela compacta', (tester) async {
    await tester.binding.setSurfaceSize(const Size(320, 568));
    addTearDown(() => tester.binding.setSurfaceSize(null));

    await tester.pumpWidget(
      MaterialApp(
        home: ParticipantQrScreen(
          participante: const ParticipanteResponse(
            id: '6ba7b810-9dad-41d1-80b4-00c04fd430c8',
            nome: 'Participante com um nome bastante comprido para credencial',
            cpf: '52998224725',
          ),
          qrCodeShare: _QrCodeShareSpy(),
        ),
      ),
    );
    await tester.pump();

    expect(tester.takeException(), isNull);
    expect(find.text('Compartilhar QR Code'), findsOneWidget);
    expect(find.text('Concluir'), findsOneWidget);
  });
}

final class _QrCodeShareSpy implements QrCodeShareGateway {
  int calls = 0;
  Uint8List? pngBytes;
  String? participanteNome;

  @override
  Future<void> share({
    required Uint8List pngBytes,
    required String participanteNome,
  }) async {
    calls++;
    this.pngBytes = pngBytes;
    this.participanteNome = participanteNome;
  }
}
