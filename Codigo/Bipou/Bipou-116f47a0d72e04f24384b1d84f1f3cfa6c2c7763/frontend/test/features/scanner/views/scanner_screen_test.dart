import 'dart:async';

import 'package:bipou_frontend/features/scanner/models/scanner_context.dart';
import 'package:bipou_frontend/features/scanner/services/scanner_sensory_feedback.dart';
import 'package:bipou_frontend/features/scanner/views/scanner_screen.dart';
import 'package:bipou_frontend/models/operation_result.dart';
import 'package:bipou_frontend/models/participante_qr_payload.dart';
import 'package:bipou_frontend/models/participante_request.dart';
import 'package:bipou_frontend/models/participante_response.dart';
import 'package:bipou_frontend/models/registro_response.dart';
import 'package:bipou_frontend/models/tipo_acao.dart';
import 'package:bipou_frontend/services/credenciamento_service.dart';
import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';

void main() {
  testWidgets(
    'exibe moldura verde, feedback sensorial e historico continuo dos ultimos 5',
    (tester) async {
      final now = DateTime.utc(2026, 11, 19, 14, 0, 0);
      final payloadController = StreamController<String>.broadcast();
      final sensory = _SensoryFeedbackSpy();
      final credenciamento = _CredenciamentoScreenSpy();
      addTearDown(payloadController.close);

      await tester.pumpWidget(
        MaterialApp(
          home: ScannerScreen(
            tipoAcao: TipoAcao.entrada,
            credenciamento: credenciamento,
            dispositivoId: 'portaria-01',
            contextoAtivo: const ScannerSessionContext(
              eventoId: 15,
              eventoNome: 'Seminfo 2026',
              subeventoId: 42,
              subeventoNome: 'Curso A',
            ),
            sensoryFeedback: sensory,
            payloadStream: payloadController.stream,
            cameraPreviewOverride: const ColoredBox(color: Colors.black87),
            clock: () => now,
          ),
        ),
      );

      expect(
        find.text('[Evento: Seminfo 2026] » [Curso: Curso A]'),
        findsOneWidget,
      );
      expect(find.text('Nenhum registro nesta sessão'), findsOneWidget);

      // 1. Leitura valida (Vetor Golden RFC-0001)
      const goldenV1 =
          '{"v":1,"pid":1042,"cpf":"12345678901","nome":"Maria Santos","eid":15,"sid":[42,43],"hash":"0ec02205"}';
      payloadController.add(goldenV1);
      await tester.pump();
      await tester.pump(const Duration(milliseconds: 150));

      expect(sensory.successCalls, 1);
      expect(find.text('ENTRADA REGISTRADA'), findsOneWidget);
      expect(find.text('Maria Santos'), findsNWidgets(2)); // banner + historico
      expect(_frameBorderColor(tester), const Color(0xFF22C55E));
      expect(find.text('1/5'), findsOneWidget);

      // 2. Leitura duplicada em < 5s -> moldura amarela
      payloadController.add(goldenV1);
      await tester.pump();
      await tester.pump(const Duration(milliseconds: 150));

      expect(sensory.duplicateCalls, 1);
      expect(find.text('Aguarde para bipar novamente'), findsOneWidget);
      expect(_frameBorderColor(tester), const Color(0xFFFACC15));
      expect(find.text('1/5'), findsOneWidget);

      // 3. Participante nao inscrito neste curso -> moldura vermelha
      final outroCurso = ParticipanteQrPayload.v1(
        pid: 2001,
        cpf: '52998224725',
        nome: 'Carlos Curso B',
        eid: 15,
        sid: const <int>[43],
      ).toRawJson();
      payloadController.add(outroCurso);
      await tester.pump();
      await tester.pump(const Duration(milliseconds: 150));

      expect(sensory.notEnrolledCalls, 1);
      expect(find.text('Não inscrito neste curso!'), findsOneWidget);
      expect(_frameBorderColor(tester), const Color(0xFFEF4444));
      expect(find.text('1/5'), findsOneWidget);

      // 4. QR Code com hash adulterado -> moldura vermelha
      const adulterado =
          '{"v":1,"pid":9999,"cpf":"12345678901","nome":"Fraude","eid":15,"sid":[42],"hash":"0ec02205"}';
      payloadController.add(adulterado);
      await tester.pump();
      await tester.pump(const Duration(milliseconds: 150));

      expect(sensory.errorCalls, 1);
      expect(find.text('QR Code inválido'), findsOneWidget);
      expect(find.text('Assinatura hash divergente'), findsOneWidget);
      expect(_frameBorderColor(tester), const Color(0xFFEF4444));

      // 5. Mais 5 leituras validas -> tela continua aberta e exibe ultimos 5
      for (var i = 1; i <= 5; i++) {
        final payload = ParticipanteQrPayload.v1(
          pid: 3000 + i,
          cpf: '52998224725',
          nome: 'Aluno $i',
          eid: 15,
          sid: const <int>[42],
        ).toRawJson();
        payloadController.add(payload);
        await tester.pump();
      }
      await tester.pump(const Duration(milliseconds: 150));

      expect(find.byType(ScannerScreen), findsOneWidget);
      expect(find.text('5/5'), findsOneWidget);
      expect(find.text('Aluno 5'), findsNWidgets(2)); // banner + historico
      expect(find.text('Aluno 1'), findsOneWidget);
      expect(find.text('Maria Santos'), findsNothing);

      // Limpa o timer visual de 2200ms antes do encerramento do teste
      await tester.pump(const Duration(seconds: 3));
    },
  );

  testWidgets('exibe SAÍDA REGISTRADA no modo de saida', (tester) async {
    final payloadController = StreamController<String>.broadcast();
    final sensory = _SensoryFeedbackSpy();
    addTearDown(payloadController.close);

    await tester.pumpWidget(
      MaterialApp(
        home: ScannerScreen(
          tipoAcao: TipoAcao.saida,
          credenciamento: _CredenciamentoScreenSpy(),
          dispositivoId: 'portaria-01',
          sensoryFeedback: sensory,
          payloadStream: payloadController.stream,
          cameraPreviewOverride: const ColoredBox(color: Colors.black87),
        ),
      ),
    );

    expect(find.text('Registrar SAÍDA'), findsOneWidget);

    const goldenV1 =
        '{"v":1,"pid":1042,"cpf":"12345678901","nome":"Maria Santos","eid":15,"sid":[42,43],"hash":"0ec02205"}';
    payloadController.add(goldenV1);
    await tester.pump();
    await tester.pump(const Duration(milliseconds: 150));

    expect(find.text('SAÍDA REGISTRADA'), findsOneWidget);
    expect(find.text('SAÍDA'), findsOneWidget);
    expect(sensory.successCalls, 1);

    await tester.pump(const Duration(seconds: 3));
  });
}

Color? _frameBorderColor(WidgetTester tester) {
  final container = tester.widget<AnimatedContainer>(
    find.byKey(const Key('scanner-frame')),
  );
  final decoration = container.decoration as BoxDecoration?;
  return decoration?.border?.top.color;
}

final class _SensoryFeedbackSpy implements ScannerSensoryFeedback {
  int successCalls = 0;
  int offlineCalls = 0;
  int notEnrolledCalls = 0;
  int duplicateCalls = 0;
  int errorCalls = 0;

  @override
  Future<void> playDuplicate() async {
    duplicateCalls++;
  }

  @override
  Future<void> playError() async {
    errorCalls++;
  }

  @override
  Future<void> playNotEnrolled() async {
    notEnrolledCalls++;
  }

  @override
  Future<void> playOffline() async {
    offlineCalls++;
  }

  @override
  Future<void> playSuccess() async {
    successCalls++;
  }
}

final class _CredenciamentoScreenSpy implements CredenciamentoGateway {
  @override
  String get apiBaseUrl => 'http://localhost:8080';

  @override
  Future<ParticipanteResponse> buscarParticipantePorCpf(String cpf) {
    throw UnimplementedError();
  }

  @override
  Future<OperationResult<ParticipanteResponse>> cadastrarParticipante({
    required ParticipanteRequest participante,
    required String dispositivoId,
  }) {
    throw UnimplementedError();
  }

  @override
  Future<List<ParticipanteResponse>> listarParticipantes() {
    throw UnimplementedError();
  }

  @override
  Future<OperationResult<RegistroResponse>> registrarLeitura({
    required String participanteId,
    required String participanteNome,
    required TipoAcao tipoAcao,
    required String dispositivoId,
  }) async {
    return OperationResult<RegistroResponse>.enviado(
      RegistroResponse(
        id: 'reg-${DateTime.now().microsecondsSinceEpoch}',
        leituraId: '550e8400-e29b-41d4-a716-446655440000',
        participanteId: participanteId,
        participanteNome: participanteNome,
        tipoAcao: tipoAcao,
        dataHora: DateTime.utc(2026, 11, 19, 14, 0),
        dataHoraLidaNoCelular: DateTime.utc(2026, 11, 19, 14, 0),
        dispositivoId: dispositivoId,
      ),
    );
  }

  @override
  Future<void> verificarConexao() async {}
}
