import 'dart:typed_data';

import 'package:bipou_frontend/features/dashboard/views/dashboard_screen.dart';
import 'package:bipou_frontend/models/operation_result.dart';
import 'package:bipou_frontend/models/participante_request.dart';
import 'package:bipou_frontend/models/participante_response.dart';
import 'package:bipou_frontend/models/registro_response.dart';
import 'package:bipou_frontend/models/tipo_acao.dart';
import 'package:bipou_frontend/services/audit_log_export_service.dart';
import 'package:bipou_frontend/services/credenciamento_service.dart';
import 'package:bipou_frontend/services/qr_code_share_service.dart';
import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';

void main() {
  testWidgets('exibe os atalhos principais e abre selecao de acao', (
    tester,
  ) async {
    await tester.pumpWidget(_app(_CredenciamentoWidgetSpy()));

    expect(find.text('Escanear QR Code'), findsOneWidget);
    expect(find.text('Ver participantes'), findsOneWidget);
    expect(find.text('Cadastrar participante'), findsOneWidget);
    expect(find.text('Entrada'), findsNothing);

    await tester.tap(find.text('Escanear QR Code'));
    await tester.pumpAndSettle();

    expect(find.text('Entrada'), findsOneWidget);
    expect(find.text('Saída'), findsOneWidget);
  });

  testWidgets('cadastra manualmente e mostra QR Code', (tester) async {
    final credenciamento = _CredenciamentoWidgetSpy();
    await tester.pumpWidget(_app(credenciamento));

    final cadastroButton = find.text('Cadastrar participante');
    await tester.ensureVisible(cadastroButton);
    await tester.tap(cadastroButton);
    await tester.pumpAndSettle();

    final fields = find.byType(TextFormField);
    expect(fields, findsNWidgets(2));
    await tester.enterText(fields.at(0), 'Participante Teste');
    await tester.enterText(fields.at(1), '52998224725');
    await tester.tap(find.text('Salvar'));
    await tester.pumpAndSettle();

    expect(find.text('QR Code'), findsOneWidget);
    expect(find.text('Participante Teste'), findsOneWidget);
    expect(find.byKey(const Key('logo-ufs')), findsOneWidget);
    expect(find.byKey(const Key('marca-dsi')), findsOneWidget);
    expect(find.text('Compartilhar QR Code'), findsOneWidget);
    expect(credenciamento.lastParticipante?.cpf, '52998224725');
  });

  testWidgets('avisa quando o CPF possui dígitos verificadores inválidos', (
    tester,
  ) async {
    final credenciamento = _CredenciamentoWidgetSpy();
    await tester.pumpWidget(_app(credenciamento));

    final cadastroButton = find.text('Cadastrar participante');
    await tester.ensureVisible(cadastroButton);
    await tester.tap(cadastroButton);
    await tester.pumpAndSettle();

    final fields = find.byType(TextFormField);
    await tester.enterText(fields.at(0), 'Participante Teste');
    await tester.enterText(fields.at(1), '12345678900');
    await tester.tap(find.text('Salvar'));
    await tester.pump();

    expect(find.text('CPF inválido'), findsOneWidget);
    expect(credenciamento.lastParticipante, isNull);
  });

  testWidgets('mostra status da API e testa conexao', (tester) async {
    final credenciamento = _CredenciamentoWidgetSpy();
    await tester.pumpWidget(_app(credenciamento));

    await tester.tap(find.byTooltip('Status da API'));
    await tester.pumpAndSettle();

    expect(find.text('Status da API'), findsOneWidget);
    expect(find.text('http://localhost:8080'), findsOneWidget);

    await tester.tap(find.text('Testar conexão'));
    await tester.pumpAndSettle();

    expect(find.text('Backend respondeu corretamente.'), findsOneWidget);
    expect(credenciamento.connectionTests, 1);
  });

  testWidgets('exporta auditoria pela tela de status', (tester) async {
    final exporter = _AuditLogExporterSpy();
    await tester.pumpWidget(
      _app(_CredenciamentoWidgetSpy(), auditLogExporter: exporter),
    );

    await tester.tap(find.byTooltip('Status da API'));
    await tester.pumpAndSettle();
    await tester.tap(find.text('Exportar log'));
    await tester.pumpAndSettle();

    expect(exporter.exports, 1);
  });
}

Widget _app(
  CredenciamentoGateway credenciamento, {
  AuditLogExporter? auditLogExporter,
}) {
  return MaterialApp(
    home: DashboardScreen(
      credenciamento: credenciamento,
      auditLogExporter: auditLogExporter ?? _AuditLogExporterSpy(),
      qrCodeShare: _QrCodeShareGatewayFake(),
      dispositivoId: 'portaria-01',
    ),
  );
}

final class _QrCodeShareGatewayFake implements QrCodeShareGateway {
  @override
  Future<void> share({
    required Uint8List pngBytes,
    required String participanteNome,
  }) async {}
}

final class _AuditLogExporterSpy implements AuditLogExporter {
  int exports = 0;

  @override
  Future<void> export() async {
    exports++;
  }
}

final class _CredenciamentoWidgetSpy implements CredenciamentoGateway {
  ParticipanteRequest? lastParticipante;
  int connectionTests = 0;

  @override
  String get apiBaseUrl => 'http://localhost:8080';

  @override
  Future<OperationResult<ParticipanteResponse>> cadastrarParticipante({
    required ParticipanteRequest participante,
    required String dispositivoId,
  }) async {
    lastParticipante = participante;
    return OperationResult<ParticipanteResponse>.enviado(
      ParticipanteResponse(
        id: '6ba7b810-9dad-41d1-80b4-00c04fd430c8',
        nome: participante.nome,
        cpf: participante.cpf,
      ),
    );
  }

  @override
  Future<ParticipanteResponse> buscarParticipantePorCpf(String cpf) {
    throw UnimplementedError();
  }

  @override
  Future<OperationResult<RegistroResponse>> registrarLeitura({
    required String participanteId,
    required String participanteNome,
    required TipoAcao tipoAcao,
    required String dispositivoId,
  }) {
    throw UnimplementedError();
  }

  @override
  Future<List<ParticipanteResponse>> listarParticipantes() async {
    return const <ParticipanteResponse>[];
  }

  @override
  Future<void> verificarConexao() async {
    connectionTests++;
  }
}
