import 'dart:convert';
import 'dart:io';
import 'dart:typed_data';

import 'package:bipou_frontend/features/dashboard/views/dashboard_screen.dart';
import 'package:bipou_frontend/features/event_selection/view_models/event_selection_view_model.dart';
import 'package:bipou_frontend/features/event_selection/views/event_selection_screen.dart';
import 'package:bipou_frontend/models/borala_models.dart';
import 'package:bipou_frontend/models/local_participant.dart';
import 'package:bipou_frontend/models/operation_result.dart';
import 'package:bipou_frontend/models/participante_qr_payload.dart';
import 'package:bipou_frontend/models/participante_request.dart';
import 'package:bipou_frontend/models/participante_response.dart';
import 'package:bipou_frontend/models/portaria_session.dart';
import 'package:bipou_frontend/models/registro_response.dart';
import 'package:bipou_frontend/models/tipo_acao.dart';
import 'package:bipou_frontend/services/audit_log_export_service.dart';
import 'package:bipou_frontend/services/borala_api_service.dart';
import 'package:bipou_frontend/services/credenciamento_service.dart';
import 'package:bipou_frontend/services/local_participant_repository.dart';
import 'package:bipou_frontend/services/qr_code_share_service.dart';
import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:sqflite_common_ffi/sqflite_ffi.dart';

void main() {
  sqfliteFfiInit();

  late SqliteLocalParticipantRepository repository;
  late _FakeBoralaApi boralaApi;

  setUp(() {
    repository = SqliteLocalParticipantRepository(
      databaseFactory: databaseFactoryFfi,
      databasePath: inMemoryDatabasePath,
    );
    boralaApi = _FakeBoralaApi();
  });

  tearDown(() async {
    await repository.close();
  });

  test(
    'ViewModel conecta ao Borala, alterna entre Evento Geral e Subevento e sincroniza participantes no SQLite',
    () async {
      final vm = EventSelectionViewModel(
        repository: repository,
        boralaApi: boralaApi,
        clock: () => DateTime.utc(2026, 11, 18, 15, 0),
      );
      addTearDown(vm.dispose);

      await vm.inicializar();
      final conectado = await vm.conectarECarregarEventos(
        baseUrl: 'http://10.0.2.2:5000',
        usuario: 'gestor@ufs.br',
        senha: 'Password123',
      );

      expect(conectado, isTrue);
      expect(vm.eventos, hasLength(2));
      expect(vm.eventoSelecionado?.nome, 'Seminfo 2026');
      expect(vm.subeventosDisponiveis, hasLength(2));

      await vm.selecionarModo(PortariaEscopoModo.subeventoEspecifico);
      await vm.selecionarSubevento(42);
      final sessao = await vm.sincronizarParticipantes();

      expect(sessao, isNotNull);
      expect(
        sessao!.contextoBanner,
        '[Evento: Seminfo 2026] » [Curso: Flutter Avançado - Sala 102]',
      );
      expect(
        sessao.resumoParticipantes,
        '2 participantes de Flutter Avançado - Sala 102 carregados localmente',
      );
      expect(vm.participantesNoEvento, 3);
      expect(vm.participantesNoContexto, 2);

      // Re-sincronizacao com 304 Not Modified preserva o cache local.
      boralaApi.returnNotModified = true;
      final sessao304 = await vm.sincronizarParticipantes();
      expect(sessao304?.totalParticipantesCarregados, 2);
      expect(vm.statusMessage, contains('cache local mantido'));

      // Alternando para Evento Geral recalcula contagem local instantaneamente.
      await vm.selecionarModo(PortariaEscopoModo.eventoGeral);
      expect(vm.participantesNoContexto, 3);
      expect(
        vm.resumoParticipantesAtual,
        '3 participantes do Evento Geral carregados localmente',
      );
    },
  );

  test(
    'ViewModel importa carga offline compactada (.json.gz) sem internet e restaura sessao ao reiniciar',
    () async {
      final vm = EventSelectionViewModel(
        repository: repository,
        boralaApi: boralaApi,
      );
      addTearDown(vm.dispose);

      const cargaJson = '''
      {
        "versao": 1,
        "evento": { "idEvento": 99, "nome": "Congresso UFS 2026" },
        "subeventos": [
          { "idSubEvento": 201, "idEvento": 99, "nome": "Curso de Python" }
        ],
        "participantes": [
          {
            "idPessoa": 501,
            "nome": "Ana Oliveira",
            "cpf": "52998224725",
            "statusInscricao": "A",
            "subeventosInscritos": [201]
          }
        ]
      }
      ''';
      final gzBytes = Uint8List.fromList(gzip.encode(utf8.encode(cargaJson)));

      final sessao = await vm.importarCargaOfflineBytes(gzBytes);
      expect(sessao, isNotNull);
      expect(sessao!.eventoNome, 'Congresso UFS 2026');
      expect(sessao.totalParticipantesCarregados, 1);

      await vm.selecionarModo(PortariaEscopoModo.subeventoEspecifico);
      await vm.confirmarSessao();

      // Simula reabertura do aplicativo sem rede: sessao e catalogo restauram.
      final vmReaberto = EventSelectionViewModel(
        repository: repository,
        boralaApi: boralaApi,
      );
      addTearDown(vmReaberto.dispose);
      await vmReaberto.inicializar();

      expect(vmReaberto.eventoSelecionado?.idEvento, 99);
      expect(vmReaberto.modo, PortariaEscopoModo.subeventoEspecifico);
      expect(vmReaberto.subeventoSelecionado?.nome, 'Curso de Python');
      expect(vmReaberto.participantesNoContexto, 1);
    },
  );

  testWidgets(
    'fluxo de configuracao de sessao na interface conecta, seleciona subevento, sincroniza e volta ao Dashboard',
    (tester) async {
      final memoryRepo = _InMemoryLocalParticipantRepository();
      await tester.pumpWidget(
        MaterialApp(
          home: DashboardScreen(
            credenciamento: _FakeCredenciamento(),
            auditLogExporter: _FakeAuditExporter(),
            qrCodeShare: _FakeQrShare(),
            dispositivoId: 'portaria-01',
            localParticipantRepository: memoryRepo,
            boralaApi: boralaApi,
            initialBoralaBaseUrl: 'http://10.0.2.2:5000',
          ),
        ),
      );
      await tester.pumpAndSettle();

      expect(find.text('Sessão de Portaria não configurada'), findsOneWidget);

      await tester.tap(
        find.byKey(const Key('dashboard-configure-session-button')),
      );
      await tester.pumpAndSettle();

      expect(find.text('Configuração de Sessão de Portaria'), findsOneWidget);

      await tester.enterText(
        find.byKey(const Key('borala-user-field')),
        'gestor@ufs.br',
      );
      await tester.enterText(
        find.byKey(const Key('borala-password-field')),
        'Senha123',
      );
      await tester.tap(find.byKey(const Key('borala-connect-button')));
      await tester.pumpAndSettle();

      expect(find.text('Seminfo 2026'), findsOneWidget);

      final modeSubevento = find.byKey(const Key('mode-subevento'));
      await tester.scrollUntilVisible(
        modeSubevento,
        200,
        scrollable: find.byType(Scrollable).first,
      );
      await tester.pumpAndSettle();
      await tester.tap(modeSubevento);
      await tester.pumpAndSettle();

      expect(find.text('Flutter Avançado - Sala 102'), findsOneWidget);

      final syncBtn = find.byKey(const Key('sync-participants-button'));
      await tester.scrollUntilVisible(
        syncBtn,
        250,
        scrollable: find.byType(Scrollable).first,
      );
      await tester.pumpAndSettle();
      await tester.tap(syncBtn);
      await tester.pumpAndSettle();

      await tester.drag(find.byType(ListView), const Offset(0, 600));
      await tester.pumpAndSettle();

      expect(
        find.text(
          '2 participantes de Flutter Avançado - Sala 102 carregados localmente',
        ),
        findsOneWidget,
      );

      await tester.tap(find.byKey(const Key('confirm-session-button')));
      await tester.pumpAndSettle();

      expect(
        find.text(
          '[Evento: Seminfo 2026] » [Curso: Flutter Avançado - Sala 102]',
        ),
        findsOneWidget,
      );
      expect(
        find.text(
          '2 participantes de Flutter Avançado - Sala 102 carregados localmente',
        ),
        findsOneWidget,
      );
      expect(find.byKey(const Key('dashboard-session-card')), findsOneWidget);
      expect(find.byKey(const Key('dashboard-context-card')), findsNothing);
      expect(find.byKey(const Key('dashboard-context-button')), findsNothing);
    },
  );

  testWidgets(
    'EventSelectionScreen permite importar arquivo JSON offline pelo seletor',
    (tester) async {
      final memoryRepo = _InMemoryLocalParticipantRepository();
      const cargaJson = '''
      {
        "versao": 1,
        "evento": { "idEvento": 15, "nome": "Seminfo 2026" },
        "subeventos": [
          { "idSubEvento": 42, "idEvento": 15, "nome": "Curso de Python" }
        ],
        "participantes": [
          {
            "idPessoa": 1042,
            "nome": "Maria Santos",
            "cpf": "12345678901",
            "statusInscricao": "A",
            "subeventosInscritos": [42]
          }
        ]
      }
      ''';

      await tester.pumpWidget(
        MaterialApp(
          home: EventSelectionScreen(
            repository: memoryRepo,
            boralaApi: boralaApi,
            filePicker: () async => Uint8List.fromList(utf8.encode(cargaJson)),
          ),
        ),
      );
      await tester.pumpAndSettle();

      final importBtn = find.byKey(const Key('import-json-file-button'));
      await tester.scrollUntilVisible(
        importBtn,
        300,
        scrollable: find.byType(Scrollable).first,
      );
      await tester.pumpAndSettle();
      await tester.tap(importBtn);
      await tester.pumpAndSettle();

      await tester.drag(find.byType(ListView), const Offset(0, 800));
      await tester.pumpAndSettle();

      expect(
        find.text('1 participante do Evento Geral carregado localmente'),
        findsOneWidget,
      );
    },
  );
}

final class _InMemoryLocalParticipantRepository
    implements LocalParticipantRepository {
  final Map<int, List<LocalParticipant>> _byEvent =
      <int, List<LocalParticipant>>{};
  final Map<int, String> _etags = <int, String>{};
  PortariaSession? _session;
  CachedBoralaCatalog? _catalog;

  @override
  Future<void> clearSession() async => _session = null;

  @override
  Future<void> close() async {}

  @override
  Future<int> countByContext({required int eventoId, int? subeventoId}) async =>
      (await listByContext(eventoId: eventoId, subeventoId: subeventoId)).length;

  @override
  Future<LocalParticipant?> findByCpf(
    String cpf, {
    required int eventoId,
  }) async {
    final clean = ParticipanteQrPayload.sanitizarCpf(cpf);
    final list = _byEvent[eventoId] ?? const <LocalParticipant>[];
    return list.where((p) => p.cpf == clean).firstOrNull;
  }

  @override
  Future<LocalParticipant?> findById(String id, {int? eventoId}) async {
    final pools = eventoId != null
        ? <List<LocalParticipant>>[_byEvent[eventoId] ?? const <LocalParticipant>[]]
        : _byEvent.values;
    for (final list in pools) {
      final found = list.where((p) => p.id == id).firstOrNull;
      if (found != null) {
        return found;
      }
    }
    return null;
  }

  @override
  Future<LocalParticipant?> findByIdPessoa(
    int idPessoa, {
    required int eventoId,
  }) async {
    final list = _byEvent[eventoId] ?? const <LocalParticipant>[];
    return list.where((p) => p.idPessoa == idPessoa).firstOrNull;
  }

  @override
  Future<List<LocalParticipant>> listByContext({
    required int eventoId,
    int? subeventoId,
  }) async {
    final list = _byEvent[eventoId] ?? const <LocalParticipant>[];
    if (subeventoId == null) {
      return List<LocalParticipant>.from(list);
    }
    return list.where((p) => p.inscritoNoSubevento(subeventoId)).toList();
  }

  @override
  Future<CachedBoralaCatalog?> loadCachedCatalog() async => _catalog;

  @override
  Future<String?> loadEventEtag(int eventoId) async => _etags[eventoId];

  @override
  Future<PortariaSession?> loadSession() async => _session;

  @override
  Future<int> replaceAllForEvent({
    required int eventoId,
    required List<LocalParticipant> participants,
  }) async {
    _byEvent[eventoId] = List<LocalParticipant>.from(participants);
    return participants.length;
  }

  @override
  Future<void> saveCachedCatalog(CachedBoralaCatalog catalog) async =>
      _catalog = catalog;

  @override
  Future<void> saveEventEtag({required int eventoId, String? etag}) async {
    if (etag == null) {
      _etags.remove(eventoId);
    } else {
      _etags[eventoId] = etag;
    }
  }

  @override
  Future<void> saveSession(PortariaSession session) async => _session = session;
}

final class _FakeBoralaApi implements BoralaApiClient {
  bool returnNotModified = false;

  @override
  Future<BoralaSessaoToken> autenticar({
    required String baseUrl,
    required String usuario,
    required String senha,
  }) async {
    return const BoralaSessaoToken(accessToken: 'fake-token-123');
  }

  @override
  Future<BoralaSessaoToken> renovarSessao({
    required String baseUrl,
    required String refreshToken,
  }) async {
    return const BoralaSessaoToken(
      accessToken: 'fake-token-refreshed',
      refreshToken: 'fake-refresh-2',
      expiresIn: 28800,
    );
  }

  @override
  Future<List<BoralaEvento>> listarEventos({
    required String baseUrl,
    required String accessToken,
  }) async {
    return const <BoralaEvento>[
      BoralaEvento(idEvento: 15, nome: 'Seminfo 2026'),
      BoralaEvento(idEvento: 16, nome: 'Encontro Tech'),
    ];
  }

  @override
  Future<List<BoralaSubevento>> listarSubeventos({
    required String baseUrl,
    required String accessToken,
    required int eventoId,
  }) async {
    if (eventoId == 15) {
      return const <BoralaSubevento>[
        BoralaSubevento(
          idSubEvento: 42,
          idEvento: 15,
          nome: 'Flutter Avançado - Sala 102',
        ),
        BoralaSubevento(
          idSubEvento: 43,
          idEvento: 15,
          nome: 'Curso de Python',
        ),
      ];
    }
    return const <BoralaSubevento>[];
  }

  @override
  Future<BoralaParticipantesResult> listarParticipantes({
    required String baseUrl,
    required String accessToken,
    required int eventoId,
    int? subeventoId,
    String? ifNoneMatch,
  }) async {
    if (returnNotModified && ifNoneMatch != null) {
      return BoralaParticipantesResult(
        participantes: const <BoralaParticipante>[],
        notModified: true,
        etag: ifNoneMatch,
      );
    }

    return BoralaParticipantesResult(
      participantes: <BoralaParticipante>[
        BoralaParticipante(
          idPessoa: 1042,
          nome: 'Maria Santos',
          nomeCracha: 'Maria Santos',
          cpf: '12345678901',
          subeventosInscritos: const <int>[42, 43],
        ),
        BoralaParticipante(
          idPessoa: 1043,
          nome: 'Carlos Pereira',
          cpf: '52998224725',
          subeventosInscritos: const <int>[42],
        ),
        BoralaParticipante(
          idPessoa: 1044,
          nome: 'Joao Somente Geral',
          cpf: '11144477735',
          subeventosInscritos: const <int>[],
        ),
      ],
      notModified: false,
      etag: 'W/"etag-v1"',
    );
  }

  @override
  Future<BoralaCargaOffline> baixarCargaOffline({
    required String baseUrl,
    required String accessToken,
    required int eventoId,
  }) {
    throw UnimplementedError();
  }
}

final class _FakeCredenciamento implements CredenciamentoGateway {
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
  Future<List<ParticipanteResponse>> listarParticipantes() async =>
      const <ParticipanteResponse>[];

  @override
  Future<OperationResult<RegistroResponse>> registrarLeitura({
    required String participanteId,
    required String participanteNome,
    required TipoAcao tipoAcao,
    required String dispositivoId,
    int? idPessoa,
    int? idEvento,
    int? idSubEvento,
    int? eventoId,
    int? subeventoId,
    String? operadorUsuario,
  }) {
    throw UnimplementedError();
  }

  @override
  Future<void> verificarConexao() async {}
}

final class _FakeAuditExporter implements AuditLogExporter {
  @override
  Future<void> export() async {}
}

final class _FakeQrShare implements QrCodeShareGateway {
  @override
  Future<void> share({
    required Uint8List pngBytes,
    required String participanteNome,
  }) async {}
}
