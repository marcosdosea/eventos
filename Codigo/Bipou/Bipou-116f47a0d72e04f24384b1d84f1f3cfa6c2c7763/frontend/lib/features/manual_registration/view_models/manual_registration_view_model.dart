import 'package:bipou_frontend/models/operation_result.dart';
import 'package:bipou_frontend/models/participante_request.dart';
import 'package:bipou_frontend/models/participante_response.dart';
import 'package:bipou_frontend/services/api_service.dart';
import 'package:bipou_frontend/services/audit_log_service.dart';
import 'package:bipou_frontend/services/credenciamento_service.dart';
import 'package:flutter/foundation.dart';

final class ManualRegistrationViewModel extends ChangeNotifier {
  ManualRegistrationViewModel({
    required this._credenciamento,
    required this.dispositivoId,
  });

  final CredenciamentoGateway _credenciamento;
  final String dispositivoId;

  bool _salvando = false;
  bool _disposed = false;
  String? _errorMessage;

  bool get salvando => _salvando;
  String? get errorMessage => _errorMessage;

  Future<OperationResult<ParticipanteResponse>?> salvar({
    required String nome,
    required String cpf,
  }) async {
    if (_salvando) {
      return null;
    }

    _salvando = true;
    _errorMessage = null;
    _notify();

    try {
      final participante = ParticipanteRequest(nome: nome, cpf: cpf);
      final result = await _credenciamento.cadastrarParticipante(
        participante: participante,
        dispositivoId: dispositivoId,
      );

      return result;
    } on ApiRequestException catch (error) {
      _errorMessage = error.message;
      return null;
    } on AuditLogException {
      _errorMessage = 'Falha ao salvar a auditoria local';
      return null;
    } on ArgumentError {
      _errorMessage = 'Verifique os dados informados';
      return null;
    } on Object {
      _errorMessage = 'Nao foi possivel concluir o cadastro';
      return null;
    } finally {
      _salvando = false;
      _notify();
    }
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
