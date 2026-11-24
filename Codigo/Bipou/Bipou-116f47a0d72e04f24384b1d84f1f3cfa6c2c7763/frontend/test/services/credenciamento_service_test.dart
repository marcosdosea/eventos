import 'dart:async';
import 'dart:io';

import 'package:bipou_frontend/models/audit_log_entry.dart';
import 'package:bipou_frontend/models/cadastro_pendente.dart';
import 'package:bipou_frontend/models/frequencia_pendente.dart';
import 'package:bipou_frontend/models/leitura_qr_code_request.dart';
import 'package:bipou_frontend/models/operation_result.dart';
import 'package:bipou_frontend/models/participante_request.dart';
import 'package:bipou_frontend/models/participante_response.dart';
import 'package:bipou_frontend/models/registro_response.dart';
import 'package:bipou_frontend/models/tipo_acao.dart';
import 'package:bipou_frontend/services/api_service.dart';
import 'package:bipou_frontend/services/audit_log_service.dart';
import 'package:bipou_frontend/services/cadastro_pendente_store.dart';
import 'package:bipou_frontend/services/connectivity_monitor_contract.dart';
import 'package:bipou_frontend/services/credenciamento_service.dart';
import 'package:bipou_frontend/services/frequencia_pendente_store_contract.dart';
import 'package:flutter_test/flutter_test.dart';

void main() {
  const uuid = '550e8400-e29b-41d4-a716-446655440000';
  const participanteId = '6ba7b810-9dad-41d1-80b4-00c04fd430c8';
  final now = DateTime.utc(2026, 8, 22, 12, 30);

  test('grava a auditoria antes de enviar o registro', () async {
    final calls = <String>[];
    final audit = _AuditSpy(calls);
    final api = _ApiSpy(calls);
    final service = CredenciamentoService(
      auditLog: audit,
      api: api,
      cadastroPendenteStore: _CadastroPendenteStoreMemory(),
      clock: () => now,
      uuidGenerator: () => uuid,
    );

    final result = await service.registrarLeitura(
      participanteId: participanteId,
      participanteNome: 'Participante Teste',
      tipoAcao: TipoAcao.entrada,
      dispositivoId: 'portaria-01',
    );

    expect(calls, <String>['audit', 'api.registrar']);
    expect(result.status, SyncStatus.enviado);
    expect(audit.entries.single.toLine(), contains(';ENTRADA;portaria-01'));
    expect(api.lastRegistro?.participanteId, participanteId);
    expect(api.lastRegistro?.leituraId, uuid);
  });

  test('retorna salvoOffline quando a API esta indisponivel', () async {
    final calls = <String>[];
    final service = CredenciamentoService(
      auditLog: _AuditSpy(calls),
      api: _ApiSpy(calls, unavailable: true),
      cadastroPendenteStore: _CadastroPendenteStoreMemory(),
      clock: () => now,
      uuidGenerator: () => uuid,
    );

    final result = await service.registrarLeitura(
      participanteId: participanteId,
      participanteNome: 'Participante Teste',
      tipoAcao: TipoAcao.entrada,
      dispositivoId: 'portaria-01',
    );

    expect(calls, <String>['audit', 'api.registrar']);
    expect(result.status, SyncStatus.salvoOffline);
    expect(result.data, isNull);
  });

  test('nao chama a API quando a auditoria falha', () async {
    final calls = <String>[];
    final service = CredenciamentoService(
      auditLog: _AuditSpy(calls, shouldFail: true),
      api: _ApiSpy(calls),
      cadastroPendenteStore: _CadastroPendenteStoreMemory(),
      clock: () => now,
      uuidGenerator: () => uuid,
    );

    await expectLater(
      service.registrarLeitura(
        participanteId: participanteId,
        participanteNome: 'Participante Teste',
        tipoAcao: TipoAcao.entrada,
        dispositivoId: 'portaria-01',
      ),
      throwsA(isA<AuditLogException>()),
    );
    expect(calls, <String>['audit']);
  });

  test(
    'expõe URL da API e testa conexão pelo endpoint de participantes',
    () async {
      final calls = <String>[];
      final api = _ApiSpy(calls);
      final service = CredenciamentoService(
        auditLog: _AuditSpy(calls),
        api: api,
        cadastroPendenteStore: _CadastroPendenteStoreMemory(),
        clock: () => now,
        uuidGenerator: () => uuid,
      );

      expect(service.apiBaseUrl, 'http://localhost:8080');

      await service.verificarConexao();

      expect(calls, <String>['api.listarParticipantes']);
    },
  );

  test(
    'mantem cadastro manual em fila quando a API esta indisponivel',
    () async {
      final calls = <String>[];
      final store = _CadastroPendenteStoreMemory();
      final service = CredenciamentoService(
        auditLog: _AuditSpy(calls),
        api: _ApiSpy(calls, unavailable: true),
        cadastroPendenteStore: store,
        clock: () => now,
        uuidGenerator: () => uuid,
      );

      final result = await service.cadastrarParticipante(
        participante: ParticipanteRequest(
          nome: 'Participante Teste',
          cpf: '52998224725',
        ),
        dispositivoId: 'portaria-01',
      );

      expect(result.status, SyncStatus.salvoOffline);
      expect(calls, <String>['audit', 'api.cadastrarParticipante']);
      expect(store.items, hasLength(1));
      expect(store.items.single.cadastroId, uuid);
      expect(store.items.single.cpf, '52998224725');
    },
  );

  test(
    'sincroniza cadastro pendente com o mesmo id e remove da fila',
    () async {
      final store = _CadastroPendenteStoreMemory()
        ..items.add(
          CadastroPendente(
            cadastroId: uuid,
            nome: 'Participante Teste',
            cpf: '52998224725',
            dispositivoId: 'portaria-01',
            criadoEm: now,
          ),
        );
      final calls = <String>[];
      final api = _ApiSpy(calls);
      final service = CredenciamentoService(
        auditLog: _AuditSpy(calls),
        api: api,
        cadastroPendenteStore: store,
        clock: () => now,
        uuidGenerator: () => 'outro-id-nao-utilizado',
      );

      final result = await service.sincronizarCadastrosPendentes();

      expect(result.enviados, 1);
      expect(result.falhasPermanentes, 0);
      expect(result.restantes, 0);
      expect(store.items, isEmpty);
      expect(api.lastParticipante?.cadastroId, uuid);
    },
  );

  test('mantem na fila cadastro recusado para nao perder dados', () async {
    final store = _CadastroPendenteStoreMemory()
      ..items.add(
        CadastroPendente(
          cadastroId: uuid,
          nome: 'Participante Teste',
          cpf: '52998224725',
          dispositivoId: 'portaria-01',
          criadoEm: now,
        ),
      );
    final service = CredenciamentoService(
      auditLog: _AuditSpy(<String>[]),
      api: _ApiSpy(<String>[], rejectManual: true),
      cadastroPendenteStore: store,
      clock: () => now,
      uuidGenerator: () => uuid,
    );

    final result = await service.sincronizarCadastrosPendentes();

    expect(result.enviados, 0);
    expect(result.falhasPermanentes, 1);
    expect(result.restantes, 1);
    expect(store.items.single.cadastroId, uuid);
  });

  test(
    'persiste leitura offline na fila local sem chamar a API quando sem conexao',
    () async {
      final calls = <String>[];
      final frequenciaStore = InMemoryFrequenciaPendenteStore();
      final connectivity = _ConnectivityMonitorFake(connected: false);
      final service = CredenciamentoService(
        auditLog: _AuditSpy(calls),
        api: _BatchApiSpy(calls),
        cadastroPendenteStore: _CadastroPendenteStoreMemory(),
        frequenciaPendenteStore: frequenciaStore,
        connectivityMonitor: connectivity,
        clock: () => now,
        uuidGenerator: () => uuid,
      );

      try {
        final result = await service.registrarLeitura(
          participanteId: participanteId,
          participanteNome: 'Maria Santos',
          tipoAcao: TipoAcao.entrada,
          dispositivoId: 'portaria-01',
          idPessoa: 1042,
          idEvento: 15,
          idSubEvento: 42,
        );

        expect(result.status, SyncStatus.salvoOffline);
        expect(calls, <String>['audit']);
        expect(service.frequenciasPendentesCount, 1);

        final salva = await frequenciaStore.buscarPorLeituraId(uuid);
        expect(salva, isNotNull);
        expect(salva?.idPessoa, 1042);
        expect(salva?.idEvento, 15);
        expect(salva?.idSubEvento, 42);
        expect(salva?.statusEnvio, StatusEnvioFrequencia.pendente);
        expect(salva?.tentativasEnvio, 0);
      } finally {
        await service.dispose();
        await connectivity.dispose();
      }
    },
  );

  test(
    'envia 120 presencas pendentes em 3 lotes de ate 50 registros (50 + 50 + 20)',
    () async {
      final calls = <String>[];
      final api = _BatchApiSpy(calls);
      final frequenciaStore = InMemoryFrequenciaPendenteStore();
      final connectivity = _ConnectivityMonitorFake(connected: false);
      var seq = 0;
      final service = CredenciamentoService(
        auditLog: _AuditSpy(calls),
        api: api,
        cadastroPendenteStore: _CadastroPendenteStoreMemory(),
        frequenciaPendenteStore: frequenciaStore,
        connectivityMonitor: connectivity,
        clock: () => now.add(Duration(seconds: seq)),
        uuidGenerator: () {
          seq++;
          final sufixo = seq.toString().padLeft(4, '0');
          return '550e8400-e29b-41d4-a716-44665544$sufixo';
        },
      );

      try {
        for (var i = 1; i <= 120; i++) {
          final result = await service.registrarLeitura(
            participanteId: participanteId,
            participanteNome: 'Participante $i',
            tipoAcao: TipoAcao.entrada,
            dispositivoId: 'portaria-01',
            idPessoa: 1000 + i,
            idEvento: 15,
          );
          expect(result.status, SyncStatus.salvoOffline);
        }

        expect(service.frequenciasPendentesCount, 120);
        expect(api.batchSizes, isEmpty);

        connectivity.connected = true;
        final syncResult = await service.sincronizarFrequenciasPendentes();

        expect(syncResult.offline, isFalse);
        expect(syncResult.enviados, 120);
        expect(syncResult.rejeitados, 0);
        expect(syncResult.restantes, 0);
        expect(syncResult.lotesEnviados, 3);
        expect(api.batchSizes, <int>[50, 50, 20]);
        expect(service.frequenciasPendentesCount, 0);
        expect(await frequenciaStore.contarPendentes(), 0);
      } finally {
        await service.dispose();
        await connectivity.dispose();
      }
    },
  );

  test(
    'dispara sincronizacao automatica quando o monitor detecta retorno de rede',
    () async {
      final calls = <String>[];
      final api = _BatchApiSpy(calls);
      final frequenciaStore = InMemoryFrequenciaPendenteStore();
      final connectivity = _ConnectivityMonitorFake(connected: false);
      var seq = 0;
      final service = CredenciamentoService(
        auditLog: _AuditSpy(calls),
        api: api,
        cadastroPendenteStore: _CadastroPendenteStoreMemory(),
        frequenciaPendenteStore: frequenciaStore,
        connectivityMonitor: connectivity,
        clock: () => now.add(Duration(seconds: seq)),
        uuidGenerator: () {
          seq++;
          final sufixo = seq.toString().padLeft(4, '0');
          return '550e8400-e29b-41d4-a716-44665544$sufixo';
        },
      );

      try {
        for (var i = 0; i < 3; i++) {
          await service.registrarLeitura(
            participanteId: participanteId,
            participanteNome: 'Participante $i',
            tipoAcao: TipoAcao.entrada,
            dispositivoId: 'portaria-01',
            idPessoa: 2000 + i,
            idEvento: 15,
          );
        }
        expect(service.frequenciasPendentesCount, 3);

        final zerouFila = service.onFrequenciasPendentesChanged.firstWhere(
          (count) => count == 0,
        );
        connectivity.emitConnected(true);
        await zerouFila;

        expect(api.batchSizes, <int>[3]);
        expect(await frequenciaStore.contarPendentes(), 0);
      } finally {
        await service.dispose();
        await connectivity.dispose();
      }
    },
  );

  test(
    'garante idempotencia preservando o mesmo leituraId sem duplicar registros',
    () async {
      final calls = <String>[];
      final api = _BatchApiSpy(calls);
      final frequenciaStore = InMemoryFrequenciaPendenteStore();
      final connectivity = _ConnectivityMonitorFake(connected: false);
      final service = CredenciamentoService(
        auditLog: _AuditSpy(calls),
        api: api,
        cadastroPendenteStore: _CadastroPendenteStoreMemory(),
        frequenciaPendenteStore: frequenciaStore,
        connectivityMonitor: connectivity,
        clock: () => now,
        uuidGenerator: () => uuid,
      );

      try {
        await service.registrarLeitura(
          participanteId: participanteId,
          participanteNome: 'Maria Santos',
          tipoAcao: TipoAcao.entrada,
          dispositivoId: 'portaria-01',
          idPessoa: 1042,
          idEvento: 15,
        );
        await service.registrarLeitura(
          participanteId: participanteId,
          participanteNome: 'Maria Santos',
          tipoAcao: TipoAcao.entrada,
          dispositivoId: 'portaria-01',
          idPessoa: 1042,
          idEvento: 15,
        );

        expect(await frequenciaStore.listarTodas(), hasLength(1));

        connectivity.connected = true;
        final syncResult = await service.sincronizarFrequenciasPendentes();

        expect(syncResult.enviados, 1);
        expect(api.sentLeituraIds, <String>[uuid]);
      } finally {
        await service.dispose();
        await connectivity.dispose();
      }
    },
  );

  test(
    'preserva registros pendentes quando sincronizacao em lote sofre falha de rede ou erro 401',
    () async {
      final calls = <String>[];
      final api = _BatchApiSpy(calls, unavailable: true);
      final frequenciaStore = InMemoryFrequenciaPendenteStore();
      final connectivity = _ConnectivityMonitorFake(connected: false);
      final service = CredenciamentoService(
        auditLog: _AuditSpy(calls),
        api: api,
        cadastroPendenteStore: _CadastroPendenteStoreMemory(),
        frequenciaPendenteStore: frequenciaStore,
        connectivityMonitor: connectivity,
        clock: () => now,
        uuidGenerator: () => uuid,
      );

      try {
        await service.registrarLeitura(
          participanteId: participanteId,
          participanteNome: 'Maria Santos',
          tipoAcao: TipoAcao.entrada,
          dispositivoId: 'portaria-01',
          idPessoa: 1042,
          idEvento: 15,
        );

        connectivity.connected = true;
        final falhaRede = await service.sincronizarFrequenciasPendentes();
        expect(falhaRede.offline, isTrue);
        expect(falhaRede.restantes, 1);
        expect(
          (await frequenciaStore.buscarPorLeituraId(uuid))?.statusEnvio,
          StatusEnvioFrequencia.pendente,
        );

        api
          ..batchUnavailable = false
          ..batchErrorStatusCode = 401;
        final falhaAuth = await service.sincronizarFrequenciasPendentes();
        expect(falhaAuth.offline, isFalse);
        expect(falhaAuth.rejeitados, 0);
        expect(falhaAuth.restantes, 1);
        final aposAuth = await frequenciaStore.buscarPorLeituraId(uuid);
        expect(aposAuth?.statusEnvio, StatusEnvioFrequencia.pendente);
        expect(aposAuth?.tentativasEnvio, 2);
      } finally {
        await service.dispose();
        await connectivity.dispose();
      }
    },
  );

  test(
    'envia leitura online com contexto Borala diretamente pelo endpoint de lote',
    () async {
      final calls = <String>[];
      final api = _BatchApiSpy(calls);
      final frequenciaStore = InMemoryFrequenciaPendenteStore();
      final connectivity = _ConnectivityMonitorFake(connected: true);
      final service = CredenciamentoService(
        auditLog: _AuditSpy(calls),
        api: api,
        cadastroPendenteStore: _CadastroPendenteStoreMemory(),
        frequenciaPendenteStore: frequenciaStore,
        connectivityMonitor: connectivity,
        clock: () => now,
        uuidGenerator: () => uuid,
      );

      try {
        final result = await service.registrarLeitura(
          participanteId: participanteId,
          participanteNome: 'Maria Santos',
          tipoAcao: TipoAcao.entrada,
          dispositivoId: 'portaria-01',
          idPessoa: 1042,
          idEvento: 15,
          idSubEvento: 42,
        );

        expect(result.status, SyncStatus.enviado);
        expect(result.data?.participanteNome, 'Maria Santos');
        expect(calls, <String>['audit', 'api.sincronizarFrequenciasLote']);
        expect(api.batchSizes, <int>[1]);
        expect(api.sentLeituraIds, <String>[uuid]);
        expect(await frequenciaStore.contarPendentes(), 0);
        expect(
          (await frequenciaStore.buscarPorLeituraId(uuid))?.statusEnvio,
          StatusEnvioFrequencia.enviado,
        );
      } finally {
        await service.dispose();
        await connectivity.dispose();
      }
    },
  );

  test(
    'preserva leitura pendente quando envio imediato online retorna HTTP 401',
    () async {
      final calls = <String>[];
      final api = _BatchApiSpy(calls)..batchErrorStatusCode = 401;
      final frequenciaStore = InMemoryFrequenciaPendenteStore();
      final connectivity = _ConnectivityMonitorFake(connected: true);
      final service = CredenciamentoService(
        auditLog: _AuditSpy(calls),
        api: api,
        cadastroPendenteStore: _CadastroPendenteStoreMemory(),
        frequenciaPendenteStore: frequenciaStore,
        connectivityMonitor: connectivity,
        clock: () => now,
        uuidGenerator: () => uuid,
      );

      try {
        await expectLater(
          service.registrarLeitura(
            participanteId: participanteId,
            participanteNome: 'Maria Santos',
            tipoAcao: TipoAcao.entrada,
            dispositivoId: 'portaria-01',
            idPessoa: 1042,
            idEvento: 15,
          ),
          throwsA(
            isA<ApiRequestException>().having(
              (e) => e.statusCode,
              'statusCode',
              401,
            ),
          ),
        );

        expect(await frequenciaStore.contarPendentes(), 1);
        final salva = await frequenciaStore.buscarPorLeituraId(uuid);
        expect(salva?.statusEnvio, StatusEnvioFrequencia.pendente);
        expect(salva?.tentativasEnvio, 1);
      } finally {
        await service.dispose();
        await connectivity.dispose();
      }
    },
  );
}

final class _AuditSpy implements AuditLogWriter {
  _AuditSpy(this.calls, {this.shouldFail = false});

  final List<String> calls;
  final bool shouldFail;
  final List<AuditLogEntry> entries = <AuditLogEntry>[];

  @override
  Future<File> append(AuditLogEntry entry) async {
    calls.add('audit');
    if (shouldFail) {
      throw const AuditLogException('Falha simulada.');
    }
    entries.add(entry);
    return File('audit-spy.txt');
  }
}

final class _ApiSpy implements CredenciamentoApi {
  _ApiSpy(this.calls, {this.unavailable = false, this.rejectManual = false});

  final List<String> calls;
  final bool unavailable;
  final bool rejectManual;
  LeituraQrCodeRequest? lastRegistro;
  ParticipanteRequest? lastParticipante;

  @override
  String get baseUrl => 'http://localhost:8080';

  @override
  Future<ParticipanteResponse> cadastrarParticipante(
    ParticipanteRequest request,
  ) async {
    calls.add('api.cadastrarParticipante');
    lastParticipante = request;
    if (unavailable) {
      throw const ApiUnavailableException('Falha simulada.');
    }
    if (rejectManual) {
      throw const ApiRequestException('Cadastro recusado.', statusCode: 409);
    }
    return ParticipanteResponse(
      id: '6ba7b810-9dad-41d1-80b4-00c04fd430c8',
      nome: request.nome,
      cpf: request.cpf,
    );
  }

  @override
  Future<ParticipanteResponse> buscarParticipantePorCpf(
    String cpf, {
    int? eventoId,
  }) async {
    calls.add('api.buscarParticipantePorCpf');
    if (unavailable) {
      throw const ApiUnavailableException('Falha simulada.');
    }
    return ParticipanteResponse(
      id: '6ba7b810-9dad-41d1-80b4-00c04fd430c8',
      nome: 'Participante Teste',
      cpf: cpf,
    );
  }

  @override
  Future<RegistroResponse> registrar(LeituraQrCodeRequest request) async {
    calls.add('api.registrar');
    lastRegistro = request;
    if (unavailable) {
      throw const ApiUnavailableException('Falha simulada.');
    }
    return RegistroResponse(
      id: 'registro-01',
      leituraId: request.leituraId,
      participanteId: 'participante-01',
      participanteNome: 'Participante',
      tipoAcao: request.tipoAcao,
      dataHora: request.dataHoraLidaNoCelular,
      dataHoraLidaNoCelular: request.dataHoraLidaNoCelular,
      dispositivoId: request.dispositivoId,
    );
  }

  @override
  Future<List<ParticipanteResponse>> listarParticipantes() {
    calls.add('api.listarParticipantes');
    return Future<List<ParticipanteResponse>>.value(
      const <ParticipanteResponse>[],
    );
  }
}

final class _CadastroPendenteStoreMemory implements CadastroPendenteStore {
  final List<CadastroPendente> items = <CadastroPendente>[];

  @override
  Future<List<CadastroPendente>> listar() async => List.of(items);

  @override
  Future<void> remover(String cadastroId) async {
    items.removeWhere((item) => item.cadastroId == cadastroId);
  }

  @override
  Future<void> salvar(CadastroPendente cadastro) async {
    final existente = items.any(
      (item) => item.cadastroId == cadastro.cadastroId,
    );
    if (!existente) {
      items.add(cadastro);
    }
  }
}

final class _BatchApiSpy extends _ApiSpy implements FrequenciaLoteApi {
  _BatchApiSpy(super.calls, {super.unavailable})
    : batchUnavailable = unavailable;

  bool batchUnavailable;
  int? batchErrorStatusCode;
  final List<int> batchSizes = <int>[];
  final List<String> sentLeituraIds = <String>[];

  @override
  Future<FrequenciaLoteSyncResponse> sincronizarFrequenciasLote(
    List<FrequenciaPendente> lote,
  ) async {
    calls.add('api.sincronizarFrequenciasLote');
    if (batchUnavailable) {
      throw const ApiUnavailableException('Falha simulada.');
    }
    if (batchErrorStatusCode != null) {
      throw ApiRequestException(
        'Erro simulado.',
        statusCode: batchErrorStatusCode,
      );
    }
    batchSizes.add(lote.length);
    final confirmados = lote.map((item) => item.leituraId).toList();
    sentLeituraIds.addAll(confirmados);
    return FrequenciaLoteSyncResponse(confirmados: confirmados);
  }
}

final class _ConnectivityMonitorFake implements ConnectivityMonitor {
  _ConnectivityMonitorFake({required this.connected});

  bool connected;
  final StreamController<bool> _controller = StreamController<bool>.broadcast();

  @override
  Future<bool> get isConnected async => connected;

  @override
  Stream<bool> get onConnectivityChanged => _controller.stream;

  void emitConnected(bool value) {
    connected = value;
    _controller.add(value);
  }

  Future<void> dispose() => _controller.close();
}
