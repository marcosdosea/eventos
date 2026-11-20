import 'dart:async';

import 'package:bipou_frontend/core/validation/cpf_validator.dart';
import 'package:bipou_frontend/features/scanner/models/recent_scan_record.dart';
import 'package:bipou_frontend/features/scanner/models/scanner_context.dart';
import 'package:bipou_frontend/features/scanner/models/scanner_feedback.dart';
import 'package:bipou_frontend/models/operation_result.dart';
import 'package:bipou_frontend/models/participante_qr_payload.dart';
import 'package:bipou_frontend/models/tipo_acao.dart';
import 'package:bipou_frontend/services/api_service.dart';
import 'package:bipou_frontend/services/audit_log_service.dart';
import 'package:bipou_frontend/services/credenciamento_service.dart';
import 'package:flutter/foundation.dart';

typedef ScannerClock = DateTime Function();

final class ScannerViewModel extends ChangeNotifier {
  ScannerViewModel({
    required this._credenciamento,
    required this.tipoAcao,
    required this.dispositivoId,
    this.contextoAtivo,
    this.cooldown = const Duration(seconds: 5),
    ScannerClock? clock,
  }) : _clock = clock ?? DateTime.now;

  static const int maxHistoricoRecente = 5;

  final CredenciamentoGateway _credenciamento;
  final ScannerClock _clock;
  final TipoAcao tipoAcao;
  final String dispositivoId;
  final ScannerSessionContext? contextoAtivo;
  final Duration cooldown;

  final StreamController<ScannerFeedback> _feedbackController =
      StreamController<ScannerFeedback>.broadcast(sync: true);
  final Map<String, DateTime> _ultimaLeituraPorParticipante =
      <String, DateTime>{};
  final Set<String> _participantesEmProcessamento = <String>{};
  final List<RecentScanRecord> _historicoRecente = <RecentScanRecord>[];

  bool _disposed = false;
  bool _leituraEmAndamento = false;

  Stream<ScannerFeedback> get feedbacks => _feedbackController.stream;
  int get leiturasEmProcessamento => _participantesEmProcessamento.length;
  bool get processando => _participantesEmProcessamento.isNotEmpty;
  List<RecentScanRecord> get historicoRecente =>
      List<RecentScanRecord>.unmodifiable(_historicoRecente);

  Future<void> processarPayload(String? rawPayload) async {
    if (_leituraEmAndamento) {
      return;
    }

    final stopwatch = Stopwatch()..start();
    final extracao = _extrairParticipanteDetalhado(rawPayload);
    final participante = extracao.participante;
    if (participante == null) {
      _emitir(
        ScannerFeedback(
          message: 'QR Code invalido',
          type: ScannerFeedbackType.erro,
          statusLabel: 'QR CODE INVÁLIDO',
          detail: extracao.errorDetail,
          processingDuration: stopwatch.elapsed,
        ),
      );
      return;
    }

    final contexto = contextoAtivo;
    if (contexto != null) {
      if (contexto.eventoId != null &&
          participante.eid != null &&
          participante.eid != contexto.eventoId) {
        _emitir(
          ScannerFeedback(
            message: contexto.isSubevento
                ? 'Não inscrito neste curso!'
                : 'Não inscrito neste evento!',
            type: ScannerFeedbackType.naoInscrito,
            participantName: participante.nome,
            statusLabel: 'ACESSO NEGADO',
            detail: 'Crachá pertence a outro evento',
            processingDuration: stopwatch.elapsed,
          ),
        );
        return;
      }

      if (contexto.subeventoId != null &&
          !participante.sid.contains(contexto.subeventoId!)) {
        _emitir(
          ScannerFeedback(
            message: 'Não inscrito neste curso!',
            type: ScannerFeedbackType.naoInscrito,
            participantName: participante.nome,
            statusLabel: 'ACESSO NEGADO',
            detail: 'Curso ativo: ${contexto.destinoLabel}',
            processingDuration: stopwatch.elapsed,
          ),
        );
        return;
      }
    }

    await _registrarParticipanteValidado(participante, stopwatch);
  }

  Future<void> processarCpf(String cpfInformado) async {
    if (_leituraEmAndamento) {
      return;
    }

    final stopwatch = Stopwatch()..start();
    final cpf = cpfInformado.trim();
    if (!CpfValidator.isValid(cpf)) {
      _emitir(
        ScannerFeedback(
          message: 'CPF invalido',
          type: ScannerFeedbackType.erro,
          statusLabel: 'CPF INVÁLIDO',
          processingDuration: stopwatch.elapsed,
        ),
      );
      return;
    }

    final chaveConsulta = 'cpf:$cpf';
    _leituraEmAndamento = true;
    _participantesEmProcessamento.add(chaveConsulta);
    _notificarMudanca();

    ParticipanteQrPayload? participante;
    try {
      final encontrado = await _credenciamento.buscarParticipantePorCpf(cpf);
      participante = ParticipanteQrPayload(
        id: encontrado.id,
        nome: encontrado.nome,
      );
    } on ApiUnavailableException {
      _emitir(
        ScannerFeedback(
          message: 'API indisponivel para consultar o CPF',
          type: ScannerFeedbackType.erro,
          statusLabel: 'API INDISPONÍVEL',
          processingDuration: stopwatch.elapsed,
        ),
      );
    } on ApiRequestException catch (error) {
      _emitir(
        ScannerFeedback(
          message: error.message,
          type: ScannerFeedbackType.erro,
          statusLabel: 'CPF NÃO ENCONTRADO',
          processingDuration: stopwatch.elapsed,
        ),
      );
    } on Object {
      _emitir(
        ScannerFeedback(
          message: 'Nao foi possivel consultar o CPF',
          type: ScannerFeedbackType.erro,
          statusLabel: 'ERRO NA CONSULTA',
          processingDuration: stopwatch.elapsed,
        ),
      );
    } finally {
      _participantesEmProcessamento.remove(chaveConsulta);
      _leituraEmAndamento = false;
      _notificarMudanca();
    }

    if (participante != null) {
      await _registrarParticipanteValidado(participante, stopwatch);
    }
  }

  void reportarErroDaCamera() {
    _emitir(
      const ScannerFeedback(
        message: 'Falha na camera',
        type: ScannerFeedbackType.erro,
        statusLabel: 'FALHA NA CÂMERA',
      ),
    );
  }

  Future<void> _registrarParticipanteValidado(
    ParticipanteQrPayload participante,
    Stopwatch stopwatch,
  ) async {
    final agora = _clock();
    _removerLeiturasExpiradas(agora);

    if (_participantesEmProcessamento.contains(participante.id)) {
      return;
    }

    final ultimaLeitura = _ultimaLeituraPorParticipante[participante.id];
    if (ultimaLeitura != null && agora.difference(ultimaLeitura) < cooldown) {
      _emitir(
        ScannerFeedback(
          message: 'Aguarde para bipar novamente',
          type: ScannerFeedbackType.duplicado,
          participantName: participante.nome,
          statusLabel: 'LEITURA DUPLICADA',
          detail:
              'Crachá lido recentemente (aguarde ${cooldown.inSeconds}s)',
          processingDuration: stopwatch.elapsed,
        ),
      );
      return;
    }

    _ultimaLeituraPorParticipante[participante.id] = agora;
    _participantesEmProcessamento.add(participante.id);
    _leituraEmAndamento = true;
    _notificarMudanca();

    var registroEfetivado = false;
    try {
      final result = await _credenciamento.registrarLeitura(
        participanteId: participante.id,
        participanteNome: participante.nome,
        tipoAcao: tipoAcao,
        dispositivoId: dispositivoId,
      );

      final statusAcao = _statusAcaoRegistrada(tipoAcao);

      if (result.status == SyncStatus.salvoOffline) {
        registroEfetivado = true;
        _adicionarAoHistorico(
          RecentScanRecord(
            participanteId: participante.id,
            participanteNome: participante.nome,
            tipoAcao: tipoAcao,
            registradoEm: agora,
            offline: true,
          ),
        );
        _emitir(
          ScannerFeedback(
            message: 'Salvo Offline: ${participante.nome}',
            type: ScannerFeedbackType.offline,
            participantName: participante.nome,
            statusLabel: statusAcao,
            detail: 'Salvo offline na auditoria local',
            processingDuration: stopwatch.elapsed,
          ),
        );
        return;
      }

      final nomeRetornado = result.data?.participanteNome.trim();
      final nomeConfirmado =
          (nomeRetornado != null && nomeRetornado.isNotEmpty)
          ? nomeRetornado
          : participante.nome;
      registroEfetivado = true;
      _adicionarAoHistorico(
        RecentScanRecord(
          participanteId: participante.id,
          participanteNome: nomeConfirmado,
          tipoAcao: tipoAcao,
          registradoEm: agora,
          offline: false,
        ),
      );
      _emitir(
        ScannerFeedback(
          message: '$statusAcao: $nomeConfirmado',
          type: ScannerFeedbackType.sucesso,
          participantName: nomeConfirmado,
          statusLabel: statusAcao,
          processingDuration: stopwatch.elapsed,
        ),
      );
    } on ApiRequestException catch (error) {
      _emitir(
        ScannerFeedback(
          message: error.message,
          type: ScannerFeedbackType.erro,
          participantName: participante.nome,
          statusLabel: 'ERRO NO REGISTRO',
          processingDuration: stopwatch.elapsed,
        ),
      );
    } on AuditLogException {
      _emitir(
        ScannerFeedback(
          message: 'Falha ao salvar a auditoria local',
          type: ScannerFeedbackType.erro,
          participantName: participante.nome,
          statusLabel: 'ERRO DE AUDITORIA',
          processingDuration: stopwatch.elapsed,
        ),
      );
    } on ArgumentError {
      _emitir(
        ScannerFeedback(
          message: 'Dados da leitura invalidos',
          type: ScannerFeedbackType.erro,
          participantName: participante.nome,
          statusLabel: 'DADOS INVÁLIDOS',
          processingDuration: stopwatch.elapsed,
        ),
      );
    } on Object {
      _emitir(
        ScannerFeedback(
          message: 'Nao foi possivel processar a leitura',
          type: ScannerFeedbackType.erro,
          participantName: participante.nome,
          statusLabel: 'ERRO NO REGISTRO',
          processingDuration: stopwatch.elapsed,
        ),
      );
    } finally {
      if (!registroEfetivado) {
        _ultimaLeituraPorParticipante.remove(participante.id);
      }
      _participantesEmProcessamento.remove(participante.id);
      _leituraEmAndamento = false;
      _notificarMudanca();
    }
  }

  ({ParticipanteQrPayload? participante, String errorDetail})
  _extrairParticipanteDetalhado(String? payload) {
    try {
      final parsed = ParticipanteQrPayload.fromRawJson(
        payload,
        secretKey: contextoAtivo?.secretKey,
      );
      return (participante: parsed, errorDetail: '');
    } on FormatException catch (error) {
      final msg = error.message.toLowerCase();
      final isHashError = msg.contains('hash') || msg.contains('assinatura');
      return (
        participante: null,
        errorDetail: isHashError
            ? 'Assinatura hash divergente'
            : 'QR Code corrompido ou inválido',
      );
    }
  }

  void _adicionarAoHistorico(RecentScanRecord record) {
    _historicoRecente.insert(0, record);
    if (_historicoRecente.length > maxHistoricoRecente) {
      _historicoRecente.removeRange(
        maxHistoricoRecente,
        _historicoRecente.length,
      );
    }
  }

  String _statusAcaoRegistrada(TipoAcao acao) {
    return switch (acao) {
      TipoAcao.entrada => 'ENTRADA REGISTRADA',
      TipoAcao.saida => 'SAÍDA REGISTRADA',
    };
  }

  void _removerLeiturasExpiradas(DateTime agora) {
    _ultimaLeituraPorParticipante.removeWhere(
      (_, leitura) => agora.difference(leitura) >= cooldown,
    );
  }

  void _emitir(ScannerFeedback feedback) {
    if (!_disposed) {
      _feedbackController.add(feedback);
    }
  }

  void _notificarMudanca() {
    if (!_disposed) {
      notifyListeners();
    }
  }

  @override
  void dispose() {
    _disposed = true;
    unawaited(_feedbackController.close());
    super.dispose();
  }
}
