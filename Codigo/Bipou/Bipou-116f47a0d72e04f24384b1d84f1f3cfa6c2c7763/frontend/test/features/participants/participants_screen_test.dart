import 'dart:typed_data';

import 'package:bipou_frontend/features/participants/views/participants_screen.dart';
import 'package:bipou_frontend/models/borala_models.dart';
import 'package:bipou_frontend/models/local_participant.dart';
import 'package:bipou_frontend/models/operation_result.dart';
import 'package:bipou_frontend/models/participante_request.dart';
import 'package:bipou_frontend/models/participante_response.dart';
import 'package:bipou_frontend/models/portaria_session.dart';
import 'package:bipou_frontend/models/registro_response.dart';
import 'package:bipou_frontend/models/tipo_acao.dart';
import 'package:bipou_frontend/services/credenciamento_service.dart';
import 'package:bipou_frontend/services/local_participant_repository.dart';
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

  testWidgets(
    'lista participantes do cache local filtrados pelo subevento mesmo sem API',
    (tester) async {
      final lookup = _FakeLocalLookup(<LocalParticipant>[
        LocalParticipant.fromBorala(
          eventoId: 15,
          participante: BoralaParticipante(
            idPessoa: 10,
            nome: 'Maria Curso Python',
            cpf: '52998224725',
            subeventosInscritos: const <int>[42],
          ),
        ),
        LocalParticipant.fromBorala(
          eventoId: 15,
          participante: BoralaParticipante(
            idPessoa: 11,
            nome: 'Pedro Outro Curso',
            cpf: '11144477735',
            subeventosInscritos: const <int>[99],
          ),
        ),
      ]);

      final sessao = PortariaSession(
        eventoId: 15,
        eventoNome: 'Seminfo 2026',
        subeventoId: 42,
        subeventoNome: 'Curso de Python',
        totalParticipantesCarregados: 1,
        sincronizadoEm: DateTime.utc(2026, 11, 18),
      );

      await tester.pumpWidget(
        MaterialApp(
          home: ParticipantsScreen(
            credenciamento: const _ParticipantsGatewayFake(),
            qrCodeShare: _QrCodeShareGatewayFake(),
            sessaoAtiva: sessao,
            localParticipants: lookup,
          ),
        ),
      );
      await tester.pumpAndSettle();

      expect(
        find.text(
          '[Evento: Seminfo 2026] » [Curso: Curso de Python] (cache local)',
        ),
        findsOneWidget,
      );
      expect(find.text('Maria Curso Python'), findsOneWidget);
      expect(find.text('Pedro Outro Curso'), findsNothing);
    },
  );
}

final class _FakeLocalLookup implements LocalParticipantLookup {
  const _FakeLocalLookup(this._items);

  final List<LocalParticipant> _items;

  @override
  Future<int> countByContext({required int eventoId, int? subeventoId}) async =>
      (await listByContext(eventoId: eventoId, subeventoId: subeventoId)).length;

  @override
  Future<LocalParticipant?> findByCpf(
    String cpf, {
    required int eventoId,
  }) async => null;

  @override
  Future<LocalParticipant?> findById(String id, {int? eventoId}) async => null;

  @override
  Future<LocalParticipant?> findByIdPessoa(
    int idPessoa, {
    required int eventoId,
  }) async => null;

  @override
  Future<List<LocalParticipant>> listByContext({
    required int eventoId,
    int? subeventoId,
  }) async {
    return _items.where((p) {
      if (p.eventoId != eventoId) {
        return false;
      }
      return subeventoId == null || p.inscritoNoSubevento(subeventoId);
    }).toList();
  }
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
  Future<ParticipanteResponse> buscarParticipantePorCpf(
    String cpf, {
    int? eventoId,
  }) {
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
    int? eventoId,
    int? subeventoId,
    String? operadorUsuario,
  }) {
    throw UnimplementedError();
  }

  @override
  Future<void> verificarConexao() {
    throw UnimplementedError();
  }
}
