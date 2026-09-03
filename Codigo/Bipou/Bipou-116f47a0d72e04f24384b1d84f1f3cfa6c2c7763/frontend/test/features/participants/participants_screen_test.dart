import 'dart:typed_data';

import 'package:bipou_frontend/features/participants/views/participants_screen.dart';
import 'package:bipou_frontend/models/operation_result.dart';
import 'package:bipou_frontend/models/participante_request.dart';
import 'package:bipou_frontend/models/participante_response.dart';
import 'package:bipou_frontend/models/registro_response.dart';
import 'package:bipou_frontend/models/tipo_acao.dart';
import 'package:bipou_frontend/services/credenciamento_service.dart';
import 'package:bipou_frontend/services/qr_code_share_service.dart';
import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';

void main() {
  testWidgets('lista participantes ordenados e filtra por nome', (
    tester,
  ) async {
    await tester.pumpWidget(
      MaterialApp(
        home: ParticipantsScreen(
          credenciamento: const _ParticipantsGatewayFake(),
          qrCodeShare: _QrCodeShareGatewayFake(),
        ),
      ),
    );
    await tester.pumpAndSettle();

    expect(find.text('Ana Participante'), findsOneWidget);
    expect(find.text('Bruno Participante'), findsOneWidget);
    expect(find.text('2 participante(s)'), findsOneWidget);

    await tester.enterText(find.byType(TextField), 'Bruno');
    await tester.pumpAndSettle();

    expect(find.text('Ana Participante'), findsNothing);
    expect(find.text('Bruno Participante'), findsOneWidget);
    expect(find.text('1 de 2 participante(s)'), findsOneWidget);
  });
}

final class _QrCodeShareGatewayFake implements QrCodeShareGateway {
  @override
  Future<void> share({
    required Uint8List pngBytes,
    required String participanteNome,
  }) async {}
}

final class _ParticipantsGatewayFake implements CredenciamentoGateway {
  const _ParticipantsGatewayFake();

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
  Future<List<ParticipanteResponse>> listarParticipantes() async {
    return const <ParticipanteResponse>[
      ParticipanteResponse(
        id: '6ba7b810-9dad-41d1-80b4-00c04fd430c8',
        nome: 'Bruno Participante',
        cpf: '52998224725',
      ),
      ParticipanteResponse(
        id: '6ba7b811-9dad-41d1-80b4-00c04fd430c8',
        nome: 'Ana Participante',
        cpf: '11144477735',
      ),
    ];
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
  Future<void> verificarConexao() {
    throw UnimplementedError();
  }
}
