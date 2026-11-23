import 'package:bipou_frontend/core/config/api_config.dart';
import 'package:bipou_frontend/models/borala_models.dart';
import 'package:bipou_frontend/models/local_participant.dart';
import 'package:bipou_frontend/models/portaria_session.dart';
import 'package:bipou_frontend/services/api_service.dart';
import 'package:bipou_frontend/services/borala_api_service.dart';
import 'package:bipou_frontend/services/local_participant_repository.dart';
import 'package:flutter/foundation.dart';

enum PortariaEscopoModo { eventoGeral, subeventoEspecifico }

typedef SessionClock = DateTime Function();

final class EventSelectionViewModel extends ChangeNotifier {
  EventSelectionViewModel({
    required this._repository,
    required this._boralaApi,
    String? initialBoralaBaseUrl,
    SessionClock? clock,
  }) : _boralaBaseUrl = initialBoralaBaseUrl ?? ApiConfig.boralaBaseUrl ?? '',
       _clock = clock ?? DateTime.now;

  final LocalParticipantRepository _repository;
  final BoralaApiClient _boralaApi;
  final SessionClock _clock;

  bool _disposed = false;
  bool _carregandoInicial = false;
  bool _autenticando = false;
  bool _carregandoSubeventos = false;
  bool _sincronizando = false;

  String _boralaBaseUrl;
  String? _operadorUsuario;
  String? _accessToken;

  List<BoralaEvento> _eventos = const <BoralaEvento>[];
  Map<int, List<BoralaSubevento>> _subeventosPorEvento =
      <int, List<BoralaSubevento>>{};
  BoralaEvento? _eventoSelecionado;
  PortariaEscopoModo _modo = PortariaEscopoModo.eventoGeral;
  BoralaSubevento? _subeventoSelecionado;

  PortariaSession? _sessaoSalva;
  int _participantesNoContexto = 0;
  int _participantesNoEvento = 0;
  DateTime? _ultimaSincronizacao;

  String? _errorMessage;
  String? _statusMessage;

  bool get carregandoInicial => _carregandoInicial;
  bool get autenticando => _autenticando;
  bool get carregandoSubeventos => _carregandoSubeventos;
  bool get sincronizando => _sincronizando;
  bool get ocupado =>
      _carregandoInicial ||
      _autenticando ||
      _carregandoSubeventos ||
      _sincronizando;

  String get boralaBaseUrl => _boralaBaseUrl;
  String? get operadorUsuario => _operadorUsuario;
  bool get autenticado => _accessToken != null && _accessToken!.isNotEmpty;

  List<BoralaEvento> get eventos => _eventos;
  BoralaEvento? get eventoSelecionado => _eventoSelecionado;
  PortariaEscopoModo get modo => _modo;
  List<BoralaSubevento> get subeventosDisponiveis {
    final evento = _eventoSelecionado;
    if (evento == null) {
      return const <BoralaSubevento>[];
    }
    return _subeventosPorEvento[evento.idEvento] ?? const <BoralaSubevento>[];
  }

  BoralaSubevento? get subeventoSelecionado => _subeventoSelecionado;
  PortariaSession? get sessaoSalva => _sessaoSalva;
  int get participantesNoContexto => _participantesNoContexto;
  int get participantesNoEvento => _participantesNoEvento;
  DateTime? get ultimaSincronizacao => _ultimaSincronizacao;
  String? get errorMessage => _errorMessage;
  String? get statusMessage => _statusMessage;

  bool get podeSincronizarViaApi => autenticado && _eventoSelecionado != null;

  bool get podeConfirmarSessao {
    if (_eventoSelecionado == null) {
      return false;
    }
    if (_modo == PortariaEscopoModo.subeventoEspecifico &&
        _subeventoSelecionado == null) {
      return false;
    }
    return true;
  }

  String? get resumoContextoAtual {
    final evento = _eventoSelecionado;
    if (evento == null) {
      return null;
    }
    if (_modo == PortariaEscopoModo.subeventoEspecifico &&
        _subeventoSelecionado != null) {
      return '[Evento: ${evento.nome}] » [Curso: ${_subeventoSelecionado!.nome}]';
    }
    return '[Evento: ${evento.nome}] » [Evento Geral]';
  }

  String? get resumoParticipantesAtual {
    final evento = _eventoSelecionado;
    if (evento == null) {
      return null;
    }
    final count = _participantesNoContexto;
    final unidade = count == 1 ? 'participante' : 'participantes';
    final carregado = count == 1 ? 'carregado' : 'carregados';
    if (_modo == PortariaEscopoModo.subeventoEspecifico &&
        _subeventoSelecionado != null) {
      return '$count $unidade de ${_subeventoSelecionado!.nome} $carregado localmente';
    }
    return '$count $unidade do Evento Geral $carregado localmente';
  }

  Future<void> inicializar() async {
    _carregandoInicial = true;
    _errorMessage = null;
    _notify();

    try {
      final sessao = await _repository.loadSession();
      final catalog = await _repository.loadCachedCatalog();

      _sessaoSalva = sessao;
      if (sessao != null) {
        if (sessao.boralaBaseUrl != null &&
            sessao.boralaBaseUrl!.trim().isNotEmpty) {
          _boralaBaseUrl = sessao.boralaBaseUrl!;
        }
        _operadorUsuario = sessao.operadorUsuario;
        _ultimaSincronizacao = sessao.sincronizadoEm;
      }

      if (catalog != null && catalog.eventos.isNotEmpty) {
        _eventos = catalog.eventos;
        _subeventosPorEvento = Map<int, List<BoralaSubevento>>.from(
          catalog.subeventosPorEvento,
        );
      }

      if (sessao != null) {
        final eventoEncontrado = _eventos
            .where((e) => e.idEvento == sessao.eventoId)
            .firstOrNull;
        if (eventoEncontrado != null) {
          _eventoSelecionado = eventoEncontrado;
        } else {
          final fallbackEvento = BoralaEvento(
            idEvento: sessao.eventoId,
            nome: sessao.eventoNome,
          );
          _eventos = List<BoralaEvento>.unmodifiable(<BoralaEvento>[
            fallbackEvento,
            ..._eventos,
          ]);
          _eventoSelecionado = fallbackEvento;
        }

        if (sessao.subeventoId != null) {
          _modo = PortariaEscopoModo.subeventoEspecifico;
          final subs =
              _subeventosPorEvento[sessao.eventoId] ??
              const <BoralaSubevento>[];
          final subEncontrado = subs
              .where((s) => s.idSubEvento == sessao.subeventoId)
              .firstOrNull;
          if (subEncontrado != null) {
            _subeventoSelecionado = subEncontrado;
          } else {
            final fallbackSub = BoralaSubevento(
              idSubEvento: sessao.subeventoId!,
              idEvento: sessao.eventoId,
              nome: sessao.subeventoNome ?? 'Subevento #${sessao.subeventoId}',
            );
            _subeventosPorEvento[sessao.eventoId] =
                List<BoralaSubevento>.unmodifiable(<BoralaSubevento>[
                  fallbackSub,
                  ...subs,
                ]);
            _subeventoSelecionado = fallbackSub;
          }
        } else {
          _modo = PortariaEscopoModo.eventoGeral;
          _subeventoSelecionado = null;
        }

        await _atualizarContagensLocais();
      } else if (_eventos.isNotEmpty) {
        _eventoSelecionado = _eventos.first;
        await _atualizarContagensLocais();
      }
    } on Object {
      _errorMessage = 'Nao foi possivel carregar a sessao local salva.';
    } finally {
      _carregandoInicial = false;
      _notify();
    }
  }

  Future<bool> conectarECarregarEventos({
    required String baseUrl,
    required String usuario,
    required String senha,
  }) async {
    if (_autenticando) {
      return false;
    }

    final urlLimpa = baseUrl.trim();
    final usuarioLimpo = usuario.trim();
    if (urlLimpa.isEmpty) {
      _errorMessage = 'Informe a URL da API do Borala.';
      _notify();
      return false;
    }
    if (usuarioLimpo.isEmpty || senha.isEmpty) {
      _errorMessage = 'Informe usuario (CPF ou e-mail) e senha.';
      _notify();
      return false;
    }

    _autenticando = true;
    _errorMessage = null;
    _statusMessage = null;
    _notify();

    try {
      final normalizedUrl = ApiConfig.normalizeHttpUrl(
        urlLimpa,
        fieldName: 'URL da API do Borala',
      );
      final token = await _boralaApi.autenticar(
        baseUrl: normalizedUrl,
        usuario: usuarioLimpo,
        senha: senha,
      );
      final eventos = await _boralaApi.listarEventos(
        baseUrl: normalizedUrl,
        accessToken: token.accessToken,
      );

      _boralaBaseUrl = normalizedUrl;
      _operadorUsuario = usuarioLimpo;
      _accessToken = token.accessToken;
      _eventos = List<BoralaEvento>.unmodifiable(eventos);

      if (eventos.isEmpty) {
        _eventoSelecionado = null;
        _subeventoSelecionado = null;
        _participantesNoEvento = 0;
        _participantesNoContexto = 0;
        _statusMessage =
            'Conectado! Nenhum evento disponivel para este operador.';
      } else {
        final anteriorId = _eventoSelecionado?.idEvento;
        _eventoSelecionado =
            eventos.where((e) => e.idEvento == anteriorId).firstOrNull ??
            eventos.first;
        await _carregarSubeventosDoEventoSelecionado(silencioso: true);
        await _atualizarContagensLocais();
        _statusMessage =
            'Conectado ao Borala (${eventos.length} evento(s) disponivel(is)).';
      }

      await _salvarCatalogoEmCache();
      return true;
    } on StateError catch (error) {
      _errorMessage = error.message;
      return false;
    } on ApiUnavailableException catch (error) {
      _errorMessage = error.message;
      return false;
    } on ApiRequestException catch (error) {
      _errorMessage = error.message;
      return false;
    } on Object {
      _errorMessage = 'Nao foi possivel conectar a API do Borala.';
      return false;
    } finally {
      _autenticando = false;
      _notify();
    }
  }

  Future<void> selecionarEvento(int eventoId) async {
    final encontrado = _eventos
        .where((e) => e.idEvento == eventoId)
        .firstOrNull;
    if (encontrado == null ||
        _eventoSelecionado?.idEvento == encontrado.idEvento) {
      return;
    }

    _eventoSelecionado = encontrado;
    _subeventoSelecionado = null;
    _errorMessage = null;
    _statusMessage = null;
    _notify();

    if (autenticado) {
      await _carregarSubeventosDoEventoSelecionado(silencioso: false);
    } else if (_modo == PortariaEscopoModo.subeventoEspecifico) {
      final subs = subeventosDisponiveis;
      _subeventoSelecionado = subs.isEmpty ? null : subs.first;
    }

    await _atualizarContagensLocais();
    _notify();
  }

  Future<void> selecionarModo(PortariaEscopoModo novoModo) async {
    if (_modo == novoModo) {
      return;
    }

    _modo = novoModo;
    _errorMessage = null;
    if (_modo == PortariaEscopoModo.eventoGeral) {
      _subeventoSelecionado = null;
    } else {
      final subs = subeventosDisponiveis;
      if (_subeventoSelecionado == null && subs.isNotEmpty) {
        _subeventoSelecionado = subs.first;
      }
    }

    await _atualizarContagensLocais();
    _notify();
  }

  Future<void> selecionarSubevento(int? subeventoId) async {
    if (subeventoId == null) {
      _subeventoSelecionado = null;
    } else {
      _subeventoSelecionado = subeventosDisponiveis
          .where((s) => s.idSubEvento == subeventoId)
          .firstOrNull;
    }
    _errorMessage = null;
    await _atualizarContagensLocais();
    _notify();
  }

  Future<PortariaSession?> sincronizarParticipantes() async {
    final evento = _eventoSelecionado;
    if (evento == null) {
      _errorMessage = 'Selecione um evento antes de sincronizar.';
      _notify();
      return null;
    }
    if (!autenticado) {
      _errorMessage =
          'Conecte-se ao Borala ou importe um arquivo JSON offline para carregar os participantes.';
      _notify();
      return null;
    }
    if (_sincronizando) {
      return null;
    }

    _sincronizando = true;
    _errorMessage = null;
    _statusMessage = null;
    _notify();

    try {
      // Sempre baixamos a lista completa do evento para permitir trocar de
      // subevento ou alternar para Evento Geral mesmo depois de ficar offline.
      final etagAtual = await _repository.loadEventEtag(evento.idEvento);
      final localCountAntes = await _repository.countByContext(
        eventoId: evento.idEvento,
      );
      final usarEtag = localCountAntes > 0 ? etagAtual : null;

      final result = await _boralaApi.listarParticipantes(
        baseUrl: _boralaBaseUrl,
        accessToken: _accessToken!,
        eventoId: evento.idEvento,
        ifNoneMatch: usarEtag,
      );

      if (!result.notModified) {
        final locais = result.participantes
            .map(
              (p) => LocalParticipant.fromBorala(
                eventoId: evento.idEvento,
                participante: p,
              ),
            )
            .toList();
        await _repository.replaceAllForEvent(
          eventoId: evento.idEvento,
          participants: locais,
        );
        await _repository.saveEventEtag(
          eventoId: evento.idEvento,
          etag: result.etag,
        );
      }

      _ultimaSincronizacao = _clock().toUtc();
      await _atualizarContagensLocais();
      final sessao = await _persistirSessaoAtual();
      _statusMessage = result.notModified
          ? 'Lista ja atualizada (cache local mantido). ${sessao.resumoParticipantes}.'
          : 'Sincronizacao concluida! ${sessao.resumoParticipantes}.';
      return sessao;
    } on ApiUnavailableException catch (error) {
      _errorMessage = error.message;
      return null;
    } on ApiRequestException catch (error) {
      _errorMessage = error.message;
      return null;
    } on Object {
      _errorMessage = 'Nao foi possivel sincronizar a lista de participantes.';
      return null;
    } finally {
      _sincronizando = false;
      _notify();
    }
  }

  Future<PortariaSession?> importarCargaOfflineBytes(Uint8List bytes) async {
    if (_sincronizando) {
      return null;
    }

    _sincronizando = true;
    _errorMessage = null;
    _statusMessage = null;
    _notify();

    try {
      final carga = BoralaCargaOffline.fromRawBytes(bytes);
      return await _aplicarCargaOffline(carga);
    } on FormatException catch (error) {
      _errorMessage = error.message;
      return null;
    } on Object {
      _errorMessage = 'Nao foi possivel importar o arquivo JSON do Borala.';
      return null;
    } finally {
      _sincronizando = false;
      _notify();
    }
  }

  Future<PortariaSession?> importarCargaOfflineJson(String rawJson) async {
    if (_sincronizando) {
      return null;
    }

    _sincronizando = true;
    _errorMessage = null;
    _statusMessage = null;
    _notify();

    try {
      final carga = BoralaCargaOffline.fromRawJson(rawJson);
      return await _aplicarCargaOffline(carga);
    } on FormatException catch (error) {
      _errorMessage = error.message;
      return null;
    } on Object {
      _errorMessage = 'Nao foi possivel importar o JSON informado.';
      return null;
    } finally {
      _sincronizando = false;
      _notify();
    }
  }

  Future<PortariaSession?> confirmarSessao() async {
    if (!podeConfirmarSessao) {
      _errorMessage = _eventoSelecionado == null
          ? 'Selecione um evento para iniciar a sessao.'
          : 'Selecione um subevento especifico ou escolha Evento Geral.';
      _notify();
      return null;
    }

    _errorMessage = null;
    await _atualizarContagensLocais();
    final sessao = await _persistirSessaoAtual();
    _notify();
    return sessao;
  }

  Future<PortariaSession> _aplicarCargaOffline(BoralaCargaOffline carga) async {
    final evento = carga.evento;
    final eventosAtualizados = <BoralaEvento>[
      evento,
      ..._eventos.where((e) => e.idEvento != evento.idEvento),
    ];
    _eventos = List<BoralaEvento>.unmodifiable(eventosAtualizados);
    _subeventosPorEvento[evento.idEvento] = List<BoralaSubevento>.unmodifiable(
      carga.subeventos,
    );
    _eventoSelecionado = evento;

    if (_modo == PortariaEscopoModo.subeventoEspecifico) {
      final subAnterior = _subeventoSelecionado?.idSubEvento;
      _subeventoSelecionado =
          carga.subeventos
              .where((s) => s.idSubEvento == subAnterior)
              .firstOrNull ??
          (carga.subeventos.isEmpty ? null : carga.subeventos.first);
      if (_subeventoSelecionado == null) {
        _modo = PortariaEscopoModo.eventoGeral;
      }
    } else {
      _subeventoSelecionado = null;
    }

    final locais = carga.participantes
        .map(
          (p) => LocalParticipant.fromBorala(
            eventoId: evento.idEvento,
            participante: p,
          ),
        )
        .toList();

    await _repository.replaceAllForEvent(
      eventoId: evento.idEvento,
      participants: locais,
    );
    await _repository.saveEventEtag(eventoId: evento.idEvento, etag: null);
    await _salvarCatalogoEmCache();

    _ultimaSincronizacao = _clock().toUtc();
    await _atualizarContagensLocais();
    final sessao = await _persistirSessaoAtual();
    _statusMessage = 'Carga offline importada! ${sessao.resumoParticipantes}.';
    return sessao;
  }

  Future<void> _carregarSubeventosDoEventoSelecionado({
    required bool silencioso,
  }) async {
    final evento = _eventoSelecionado;
    if (evento == null || !autenticado) {
      return;
    }

    _carregandoSubeventos = true;
    if (!silencioso) {
      _notify();
    }

    try {
      final subeventos = await _boralaApi.listarSubeventos(
        baseUrl: _boralaBaseUrl,
        accessToken: _accessToken!,
        eventoId: evento.idEvento,
      );
      _subeventosPorEvento[evento.idEvento] =
          List<BoralaSubevento>.unmodifiable(subeventos);
      if (_modo == PortariaEscopoModo.subeventoEspecifico) {
        final subIdAtual = _subeventoSelecionado?.idSubEvento;
        _subeventoSelecionado =
            subeventos.where((s) => s.idSubEvento == subIdAtual).firstOrNull ??
            (subeventos.isEmpty ? null : subeventos.first);
      }
      await _salvarCatalogoEmCache();
    } on Object {
      if (!silencioso) {
        _errorMessage =
            'Nao foi possivel atualizar os subeventos. Usando cache local se disponivel.';
      }
    } finally {
      _carregandoSubeventos = false;
      if (!silencioso) {
        _notify();
      }
    }
  }

  Future<void> _atualizarContagensLocais() async {
    final evento = _eventoSelecionado;
    if (evento == null) {
      _participantesNoEvento = 0;
      _participantesNoContexto = 0;
      return;
    }

    _participantesNoEvento = await _repository.countByContext(
      eventoId: evento.idEvento,
    );
    final subId = _modo == PortariaEscopoModo.subeventoEspecifico
        ? _subeventoSelecionado?.idSubEvento
        : null;
    _participantesNoContexto = await _repository.countByContext(
      eventoId: evento.idEvento,
      subeventoId: subId,
    );
  }

  Future<PortariaSession> _persistirSessaoAtual() async {
    final evento = _eventoSelecionado!;
    final isSub =
        _modo == PortariaEscopoModo.subeventoEspecifico &&
        _subeventoSelecionado != null;
    final sessao = PortariaSession(
      eventoId: evento.idEvento,
      eventoNome: evento.nome,
      subeventoId: isSub ? _subeventoSelecionado!.idSubEvento : null,
      subeventoNome: isSub ? _subeventoSelecionado!.nome : null,
      totalParticipantesCarregados: _participantesNoContexto,
      sincronizadoEm: _ultimaSincronizacao ?? _clock().toUtc(),
      boralaBaseUrl: _boralaBaseUrl.trim().isEmpty ? null : _boralaBaseUrl,
      operadorUsuario: _operadorUsuario,
    );
    await _repository.saveSession(sessao);
    _sessaoSalva = sessao;
    return sessao;
  }

  Future<void> _salvarCatalogoEmCache() async {
    await _repository.saveCachedCatalog(
      CachedBoralaCatalog(
        eventos: _eventos,
        subeventosPorEvento: _subeventosPorEvento,
      ),
    );
  }

  void _notify() {
    if (!_disposed) {
      notifyListeners();
    }
  }

  @override
  void dispose() {
    _disposed = true;
    super.dispose();
  }
}
