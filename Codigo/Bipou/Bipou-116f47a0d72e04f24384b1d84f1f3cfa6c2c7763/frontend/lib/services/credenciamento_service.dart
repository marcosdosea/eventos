import 'dart:async';

import 'package:bipou_frontend/models/audit_log_entry.dart';
import 'package:bipou_frontend/models/cadastro_pendente.dart';
import 'package:bipou_frontend/models/frequencia_pendente.dart';
import 'package:bipou_frontend/models/operation_result.dart';
import 'package:bipou_frontend/models/participante_request.dart';
import 'package:bipou_frontend/models/participante_response.dart';
import 'package:bipou_frontend/models/registro_response.dart';
import 'package:bipou_frontend/models/tipo_acao.dart';
import 'package:bipou_frontend/services/api_service.dart';
import 'package:bipou_frontend/services/audit_log_service.dart';
import 'package:bipou_frontend/services/cadastro_pendente_store.dart';
import 'package:bipou_frontend/services/connectivity_monitor_contract.dart';
import 'package:bipou_frontend/services/frequencia_pendente_store_contract.dart';
import 'package:uuid/uuid.dart';

typedef Clock = DateTime Function();
typedef UuidGenerator = String Function();

abstract interface class CredenciamentoGateway {
  String get apiBaseUrl;

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
    TipoAcao? ultimaAcaoLista,
  });

  Future<OperationResult<ParticipanteResponse>> cadastrarParticipante({
    required ParticipanteRequest participante,
    required String dispositivoId,
  });

  Future<List<ParticipanteResponse>> listarParticipantes();

  Future<ParticipanteResponse> buscarParticipantePorCpf(
    String cpf, {
    int? eventoId,
  });

  Future<void> verificarConexao();
}

abstract interface class CadastroPendenteSynchronizer {
  Future<CadastroSyncResult> sincronizarCadastrosPendentes();
}

final class CadastroSyncResult {
  const CadastroSyncResult({
    required this.enviados,
    required this.falhasPermanentes,
    required this.restantes,
  });

  final int enviados;
  final int falhasPermanentes;
  final int restantes;
}

abstract interface class FrequenciaPendenteSynchronizer {
  int get frequenciasPendentesCount;

  Stream<int> get onFrequenciasPendentesChanged;

  Future<int> atualizarContagemFrequenciasPendentes();

  Future<FrequenciaSyncResult> sincronizarFrequenciasPendentes();

  Future<void> dispose();
}

final class FrequenciaSyncResult {
  const FrequenciaSyncResult({
    required this.enviados,
    required this.rejeitados,
    required this.restantes,
    required this.lotesEnviados,
    this.offline = false,
  });

  final int enviados;
  final int rejeitados;
  final int restantes;
  final int lotesEnviados;
  final bool offline;
}

final class CredenciamentoService
    implements
        CredenciamentoGateway,
        CadastroPendenteSynchronizer,
        FrequenciaPendenteSynchronizer {
  factory CredenciamentoService({
    required AuditLogWriter auditLog,
    required CredenciamentoApi api,
    required CadastroPendenteStore cadastroPendenteStore,
    FrequenciaPendenteStore? frequenciaPendenteStore,
    ConnectivityMonitor? connectivityMonitor,
    Clock? clock,
    UuidGenerator? uuidGenerator,
  }) {
    return CredenciamentoService._(
      auditLog: auditLog,
      api: api,
      cadastroPendenteStore: cadastroPendenteStore,
      frequenciaPendenteStore:
          frequenciaPendenteStore ?? InMemoryFrequenciaPendenteStore(),
      connectivityMonitor: connectivityMonitor,
      clock: clock ?? DateTime.now,
      uuidGenerator: uuidGenerator ?? const Uuid().v4,
    );
  }

  CredenciamentoService._({
    required this._auditLog,
    required this._api,
    required this._cadastroPendenteStore,
    required this._frequenciaPendenteStore,
    required this._connectivityMonitor,
    required this._clock,
    required this._uuidGenerator,
  }) {
    _connectivitySubscription = _connectivityMonitor?.onConnectivityChanged
        .listen(
          (connected) {
            if (connected && !_disposed) {
              _dispararSincronizacaoEmBackground();
            }
          },
          onError: (Object _) {},
        );
  }

  static const String cadastroManualAuditAction = 'CADASTRO_MANUAL';
  static const int tamanhoMaximoLoteFrequencia = 50;

  final AuditLogWriter _auditLog;
  final CredenciamentoApi _api;
  final CadastroPendenteStore _cadastroPendenteStore;
  final FrequenciaPendenteStore _frequenciaPendenteStore;
  final ConnectivityMonitor? _connectivityMonitor;
  final Clock _clock;
  final UuidGenerator _uuidGenerator;

  final StreamController<int> _frequenciasPendentesController =
      StreamController<int>.broadcast();

  StreamSubscription<bool>? _connectivitySubscription;
  Future<CadastroSyncResult>? _sincronizacaoAtiva;
  Future<FrequenciaSyncResult>? _sincronizacaoFrequenciaAtiva;
  int _frequenciasPendentesCount = 0;
  bool _disposed = false;

  @override
  String get apiBaseUrl => _api.baseUrl;

  @override
  int get frequenciasPendentesCount => _frequenciasPendentesCount;

  @override
  Stream<int> get onFrequenciasPendentesChanged =>
      _frequenciasPendentesController.stream;

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
    TipoAcao? ultimaAcaoLista,
  }) async {
    final dataHora = _clock().toUtc();
    final leituraId = _uuidGenerator();
    final eventoResolvido = eventoId ?? idEvento;
    final subeventoResolvido = subeventoId ?? idSubEvento;
    final existentePorId = await _frequenciaPendenteStore.buscarPorLeituraId(
      leituraId,
    );
    if (existentePorId == null) {
      final ultimaAcaoLocal = await _frequenciaPendenteStore
          .obterUltimaAcaoValida(
            participanteId: participanteId,
            idPessoa: idPessoa ?? 0,
            idEvento: eventoResolvido ?? 0,
            idSubEvento: subeventoResolvido,
          );
      final ultimaAcao = ultimaAcaoLocal ?? ultimaAcaoLista;
      if (tipoAcao == TipoAcao.entrada && ultimaAcao == TipoAcao.entrada) {
        throw const ApiRequestException(
          'Entrada duplicada: este participante já possui uma entrada registrada.',
          statusCode: 409,
        );
      }
      if (tipoAcao == TipoAcao.saida && ultimaAcao == TipoAcao.saida) {
        throw const ApiRequestException(
          'Saída duplicada: este participante já possui uma saída registrada.',
          statusCode: 409,
        );
      }
    }

    final frequencia = FrequenciaPendente(
      leituraId: leituraId,
      idPessoa: idPessoa ?? 0,
      participanteId: participanteId,
      participanteNome: participanteNome,
      idEvento: eventoResolvido ?? 0,
      idSubEvento: subeventoResolvido,
      operadorUsuario: operadorUsuario,
      tipoAcao: tipoAcao,
      dataHoraLida: dataHora,
      dispositivoId: dispositivoId,
    );
    final request = frequencia.toLeituraQrCodeRequest();

    await _auditLog.append(
      AuditLogEntry(
        dataHora: dataHora,
        participanteId: participanteId,
        participanteNome: participanteNome,
        tipoAcao: tipoAcao.apiValue,
        dispositivoId: dispositivoId,
      ),
    );

    await _frequenciaPendenteStore.salvarTransacional(frequencia);
    await atualizarContagemFrequenciasPendentes();

    final monitor = _connectivityMonitor;
    if (monitor != null) {
      final online = await monitor.isConnected;
      if (!online) {
        return const OperationResult<RegistroResponse>.salvoOffline();
      }
    }

    try {
      final api = _api;
      final RegistroResponse response;
      if (frequencia.possuiContextoBorala && api is FrequenciaLoteApi) {
        final loteApi = api as FrequenciaLoteApi;
        final loteResponse = await loteApi.sincronizarFrequenciasLote(
          <FrequenciaPendente>[frequencia],
        );
        if (loteResponse.confirmados.contains(leituraId)) {
          response = RegistroResponse(
            id: leituraId,
            leituraId: leituraId,
            participanteId: participanteId,
            participanteNome: participanteNome,
            tipoAcao: tipoAcao,
            dataHora: dataHora,
            dataHoraLidaNoCelular: dataHora,
            dispositivoId: dispositivoId,
          );
        } else {
          FrequenciaLoteRejeicao? rejeicao;
          for (final item in loteResponse.rejeitados) {
            if (item.leituraId == leituraId) {
              rejeicao = item;
              break;
            }
          }
          if (rejeicao != null) {
            await _frequenciaPendenteStore.marcarErroEmLote(<String, String>{
              leituraId: rejeicao.mensagem,
            });
            await atualizarContagemFrequenciasPendentes();
            throw ApiRequestException(rejeicao.mensagem);
          }
          await _registrarTentativaFalhaRede(<String>[leituraId]);
          await atualizarContagemFrequenciasPendentes();
          return const OperationResult<RegistroResponse>.salvoOffline();
        }
      } else {
        response = await api.registrar(request);
      }

      await _marcarFrequenciasEnviadas(<String>[leituraId]);
      await atualizarContagemFrequenciasPendentes();
      _dispararSincronizacaoEmBackground();
      return OperationResult<RegistroResponse>.enviado(response);
    } on ApiUnavailableException {
      await _registrarTentativaFalhaRede(<String>[leituraId]);
      await atualizarContagemFrequenciasPendentes();
      return const OperationResult<RegistroResponse>.salvoOffline();
    } on ApiRequestException catch (error) {
      if (error.statusCode == 401 ||
          error.statusCode == 403 ||
          error.statusCode == 408 ||
          error.statusCode == 429) {
        await _registrarTentativaFalhaRede(<String>[leituraId]);
      } else {
        await _marcarFrequenciasComErro(<String, String>{
          leituraId: error.message,
        });
      }
      await atualizarContagemFrequenciasPendentes();
      rethrow;
    }
  }

  @override
  Future<OperationResult<ParticipanteResponse>> cadastrarParticipante({
    required ParticipanteRequest participante,
    required String dispositivoId,
  }) async {
    final dataHora = _clock().toUtc();
    final request = participante.cadastroId == null
        ? participante.withCadastroId(_uuidGenerator())
        : participante;

    await _auditLog.append(
      AuditLogEntry(
        dataHora: dataHora,
        participanteId: null,
        participanteNome: request.nome.trim(),
        tipoAcao: cadastroManualAuditAction,
        dispositivoId: dispositivoId,
      ),
    );

    final pendente = CadastroPendente(
      cadastroId: request.cadastroId!,
      nome: request.nome,
      cpf: request.cpf,
      email: request.email,
      dispositivoId: dispositivoId,
      criadoEm: dataHora,
    );
    await _cadastroPendenteStore.salvar(pendente);

    try {
      final response = await _api.cadastrarParticipante(request);
      await _removerCadastroConfirmado(request.cadastroId!);
      unawaited(sincronizarCadastrosPendentes());
      return OperationResult<ParticipanteResponse>.enviado(response);
    } on ApiUnavailableException {
      return const OperationResult<ParticipanteResponse>.salvoOffline();
    } on ApiRequestException {
      await _removerCadastroConfirmado(request.cadastroId!);
      rethrow;
    }
  }

  @override
  Future<List<ParticipanteResponse>> listarParticipantes() async {
    final participantes = await _api.listarParticipantes();
    _dispararSincronizacaoEmBackground();
    return participantes;
  }

  @override
  Future<ParticipanteResponse> buscarParticipantePorCpf(
    String cpf, {
    int? eventoId,
  }) => _api.buscarParticipantePorCpf(cpf, eventoId: eventoId);

  @override
  Future<void> verificarConexao() async {
    await _api.listarParticipantes();
    await sincronizarCadastrosPendentes();
    await sincronizarFrequenciasPendentes();
  }

  @override
  Future<int> atualizarContagemFrequenciasPendentes() async {
    try {
      final total = await _frequenciaPendenteStore.contarPendentes();
      _atualizarContagemLocal(total);
      return total;
    } on FrequenciaPendenteStoreException {
      return _frequenciasPendentesCount;
    }
  }

  @override
  Future<FrequenciaSyncResult> sincronizarFrequenciasPendentes() async {
    final sincronizacaoExistente = _sincronizacaoFrequenciaAtiva;
    if (sincronizacaoExistente != null) {
      return sincronizacaoExistente;
    }

    final sincronizacao = _sincronizarFrequenciasAgora();
    _sincronizacaoFrequenciaAtiva = sincronizacao;
    try {
      return await sincronizacao;
    } finally {
      if (identical(_sincronizacaoFrequenciaAtiva, sincronizacao)) {
        _sincronizacaoFrequenciaAtiva = null;
      }
    }
  }

  Future<FrequenciaSyncResult> _sincronizarFrequenciasAgora() async {
    final monitor = _connectivityMonitor;
    if (monitor != null) {
      final online = await monitor.isConnected;
      if (!online) {
        final restantes = await atualizarContagemFrequenciasPendentes();
        return FrequenciaSyncResult(
          enviados: 0,
          rejeitados: 0,
          restantes: restantes,
          lotesEnviados: 0,
          offline: true,
        );
      }
    }

    var enviados = 0;
    var rejeitados = 0;
    var lotesEnviados = 0;
    var offline = false;

    while (true) {
      final lote = await _frequenciaPendenteStore.listarParaEnvio(
        limit: tamanhoMaximoLoteFrequencia,
      );
      if (lote.isEmpty) {
        break;
      }

      final leituraIdsDoLote = lote
          .map((item) => item.leituraId)
          .toList(growable: false);

      try {
        final resposta = await _enviarLoteFrequencias(lote);
        lotesEnviados++;

        if (resposta.confirmados.isNotEmpty) {
          await _marcarFrequenciasEnviadas(resposta.confirmados);
          enviados += resposta.confirmados.length;
        }

        if (resposta.rejeitados.isNotEmpty) {
          final erros = <String, String>{
            for (final item in resposta.rejeitados)
              item.leituraId: item.mensagem,
          };
          await _marcarFrequenciasComErro(erros);
          rejeitados += resposta.rejeitados.length;
        }

        final processados = <String>{
          ...resposta.confirmados,
          ...resposta.rejeitados.map((item) => item.leituraId),
        };
        final naoRespondidos = leituraIdsDoLote
            .where((id) => !processados.contains(id))
            .toList(growable: false);
        if (naoRespondidos.isNotEmpty) {
          await _registrarTentativaFalhaRede(naoRespondidos);
          break;
        }
      } on ApiUnavailableException {
        await _registrarTentativaFalhaRede(leituraIdsDoLote);
        offline = true;
        break;
      } on ApiRequestException catch (error) {
        if (error.statusCode == 401 ||
            error.statusCode == 403 ||
            error.statusCode == 408 ||
            error.statusCode == 429) {
          await _registrarTentativaFalhaRede(leituraIdsDoLote);
          break;
        }
        final erros = <String, String>{
          for (final id in leituraIdsDoLote) id: error.message,
        };
        await _marcarFrequenciasComErro(erros);
        rejeitados += leituraIdsDoLote.length;
      }
    }

    final restantes = await atualizarContagemFrequenciasPendentes();
    return FrequenciaSyncResult(
      enviados: enviados,
      rejeitados: rejeitados,
      restantes: restantes,
      lotesEnviados: lotesEnviados,
      offline: offline,
    );
  }

  Future<FrequenciaLoteSyncResponse> _enviarLoteFrequencias(
    List<FrequenciaPendente> lote,
  ) async {
    final api = _api;
    if (api is FrequenciaLoteApi) {
      return (api as FrequenciaLoteApi).sincronizarFrequenciasLote(lote);
    }

    final confirmados = <String>[];
    final rejeitados = <FrequenciaLoteRejeicao>[];
    for (final item in lote) {
      try {
        await api.registrar(item.toLeituraQrCodeRequest());
        confirmados.add(item.leituraId);
      } on ApiUnavailableException {
        if (confirmados.isEmpty && rejeitados.isEmpty) {
          rethrow;
        }
        break;
      } on ApiRequestException catch (error) {
        rejeitados.add(
          FrequenciaLoteRejeicao(
            leituraId: item.leituraId,
            codigo: error.statusCode?.toString(),
            mensagem: error.message,
          ),
        );
      }
    }

    return FrequenciaLoteSyncResponse(
      confirmados: List<String>.unmodifiable(confirmados),
      rejeitados: List<FrequenciaLoteRejeicao>.unmodifiable(rejeitados),
    );
  }

  @override
  Future<CadastroSyncResult> sincronizarCadastrosPendentes() async {
    final sincronizacaoExistente = _sincronizacaoAtiva;
    if (sincronizacaoExistente != null) {
      return sincronizacaoExistente;
    }

    final sincronizacao = _sincronizarAgora();
    _sincronizacaoAtiva = sincronizacao;
    try {
      return await sincronizacao;
    } finally {
      if (identical(_sincronizacaoAtiva, sincronizacao)) {
        _sincronizacaoAtiva = null;
      }
    }
  }

  Future<CadastroSyncResult> _sincronizarAgora() async {
    final pendentes = await _cadastroPendenteStore.listar();
    var enviados = 0;
    var falhasPermanentes = 0;

    for (final pendente in pendentes) {
      try {
        await _api.cadastrarParticipante(pendente.toRequest());
        await _removerCadastroConfirmado(pendente.cadastroId);
        enviados++;
      } on ApiUnavailableException {
        break;
      } on ApiRequestException {
        // Mantém o item para não perder silenciosamente um cadastro recusado.
        falhasPermanentes++;
      }
    }

    final restantes = (await _cadastroPendenteStore.listar()).length;
    return CadastroSyncResult(
      enviados: enviados,
      falhasPermanentes: falhasPermanentes,
      restantes: restantes,
    );
  }

  Future<void> _removerCadastroConfirmado(String cadastroId) async {
    try {
      await _cadastroPendenteStore.remover(cadastroId);
    } on CadastroPendenteStoreException {
      // O backend é idempotente: se a limpeza local falhar, o próximo reenvio
      // recupera a mesma resposta e tenta remover o item novamente.
    }
  }

  Future<void> _marcarFrequenciasEnviadas(List<String> leituraIds) async {
    try {
      await _frequenciaPendenteStore.marcarEnviadosEmLote(leituraIds);
    } on FrequenciaPendenteStoreException {
      // O backend é idempotente via leituraId: se a marcação local falhar,
      // o próximo ciclo reenvia sem duplicar registros no servidor.
    }
  }

  Future<void> _marcarFrequenciasComErro(
    Map<String, String> errosPorLeituraId,
  ) async {
    try {
      await _frequenciaPendenteStore.marcarErroEmLote(errosPorLeituraId);
    } on FrequenciaPendenteStoreException {
      // Mantém resiliência caso o banco local falhe ao atualizar o erro.
    }
  }

  Future<void> _registrarTentativaFalhaRede(List<String> leituraIds) async {
    try {
      await _frequenciaPendenteStore.registrarTentativaFalhaRede(leituraIds);
    } on FrequenciaPendenteStoreException {
      // A leitura já foi persistida como PENDENTE antes da tentativa de envio.
    }
  }

  void _dispararSincronizacaoEmBackground() {
    unawaited(
      sincronizarCadastrosPendentes().then<void>((_) {}, onError: (Object _) {}),
    );
    unawaited(
      sincronizarFrequenciasPendentes().then<void>(
        (_) {},
        onError: (Object _) {},
      ),
    );
  }

  void _atualizarContagemLocal(int total) {
    if (_disposed) {
      return;
    }
    _frequenciasPendentesCount = total;
    if (!_frequenciasPendentesController.isClosed) {
      _frequenciasPendentesController.add(total);
    }
  }

  @override
  Future<void> dispose() async {
    if (_disposed) {
      return;
    }
    _disposed = true;
    await _connectivitySubscription?.cancel();
    await _frequenciasPendentesController.close();
    await _frequenciaPendenteStore.close();
  }
}
