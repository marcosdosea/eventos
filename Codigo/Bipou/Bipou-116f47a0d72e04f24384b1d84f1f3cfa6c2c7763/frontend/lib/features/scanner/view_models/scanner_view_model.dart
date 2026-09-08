import 'dart:async';

import 'package:bipou_frontend/core/validation/cpf_validator.dart';
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
    this.cooldown = const Duration(seconds: 5),
    ScannerClock? clock,
  }) : _clock = clock ?? DateTime.now;

  final CredenciamentoGateway _credenciamento;
  final ScannerClock _clock;
  final TipoAcao tipoAcao;
  final String dispositivoId;
  final Duration cooldown;

  final StreamController<ScannerFeedback> _feedbackController =
      StreamController<ScannerFeedback>.broadcast(sync: true);
  final Map<String, DateTime> _ultimaLeituraPorParticipante =
      <String, DateTime>{};
  final Set<String> _participantesEmProcessamento = <String>{};

  bool _disposed = false;
  bool _leituraEmAndamento = false;

  Stream<ScannerFeedback> get feedbacks => _feedbackController.stream;
  int get leiturasEmProcessamento => _participantesEmProcessamento.length;
  bool get processando => _participantesEmProcessamento.isNotEmpty;

  Future<void> processarPayload(String? payload) async {
    if (_leituraEmAndamento) {
      return;
    }

    final participante = _extrairParticipante(payload);
    if (participante == null) {
      _emitir(
        const ScannerFeedback(
          message: 'QR Code invalido',
          type: ScannerFeedbackType.erro,
        ),
      );
      return;
    }

    final agora = _clock();
    _removerLeiturasExpiradas(agora);

    final ultimaLeitura = _ultimaLeituraPorParticipante[participante.id];
    if (_participantesEmProcessamento.contains(participante.id) ||
        (ultimaLeitura != null && agora.difference(ultimaLeitura) < cooldown)) {
      return;
    }

    _ultimaLeituraPorParticipante[participante.id] = agora;
    _participantesEmProcessamento.add(participante.id);
    _leituraEmAndamento = true;
    _notificarMudanca();

    try {
      final result = await _credenciamento.registrarLeitura(
        participanteId: participante.id,
        participanteNome: participante.nome,
        tipoAcao: tipoAcao,
        dispositivoId: dispositivoId,
      );

      if (result.status == SyncStatus.salvoOffline) {
        _emitir(
          ScannerFeedback(
            message: 'Salvo Offline: ${participante.nome}',
            type: ScannerFeedbackType.offline,
          ),
        );
        return;
      }

      final nomeConfirmado = result.data?.participanteNome;
      _emitir(
        ScannerFeedback(
          message: nomeConfirmado == null
              ? 'Registro realizado'
              : 'Registro realizado: $nomeConfirmado',
          type: ScannerFeedbackType.sucesso,
        ),
      );
    } on ApiRequestException catch (error) {
      _emitir(
        ScannerFeedback(message: error.message, type: ScannerFeedbackType.erro),
      );
    } on AuditLogException {
      _emitir(
        const ScannerFeedback(
          message: 'Falha ao salvar a auditoria local',
          type: ScannerFeedbackType.erro,
        ),
      );
    } on ArgumentError {
      _emitir(
        const ScannerFeedback(
          message: 'Dados da leitura invalidos',
          type: ScannerFeedbackType.erro,
        ),
      );
    } on Object {
      _emitir(
        const ScannerFeedback(
          message: 'Nao foi possivel processar a leitura',
          type: ScannerFeedbackType.erro,
        ),
      );
    } finally {
      _participantesEmProcessamento.remove(participante.id);
      _leituraEmAndamento = false;
      _notificarMudanca();
    }
  }

  Future<void> processarCpf(String cpfInformado) async {
    if (_leituraEmAndamento) {
      return;
    }

    final cpf = cpfInformado.trim();
    if (!CpfValidator.isValid(cpf)) {
      _emitir(
        const ScannerFeedback(
          message: 'CPF invalido',
          type: ScannerFeedbackType.erro,
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
        const ScannerFeedback(
          message: 'API indisponivel para consultar o CPF',
          type: ScannerFeedbackType.erro,
        ),
      );
    } on ApiRequestException catch (error) {
      _emitir(
        ScannerFeedback(message: error.message, type: ScannerFeedbackType.erro),
      );
    } on Object {
      _emitir(
        const ScannerFeedback(
          message: 'Nao foi possivel consultar o CPF',
          type: ScannerFeedbackType.erro,
        ),
      );
    } finally {
      _participantesEmProcessamento.remove(chaveConsulta);
      _leituraEmAndamento = false;
      _notificarMudanca();
    }

    if (participante != null) {
      await processarPayload(participante.toRawJson());
    }
  }

  void reportarErroDaCamera() {
    _emitir(
      const ScannerFeedback(
        message: 'Falha na camera',
        type: ScannerFeedbackType.erro,
      ),
    );
  }

  ParticipanteQrPayload? _extrairParticipante(String? payload) {
    try {
      return ParticipanteQrPayload.fromRawJson(payload);
    } on FormatException {
      return null;
    }
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
