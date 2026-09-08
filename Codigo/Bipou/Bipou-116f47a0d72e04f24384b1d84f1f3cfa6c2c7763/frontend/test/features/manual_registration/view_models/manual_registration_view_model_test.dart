import 'package:bipou_frontend/features/manual_registration/view_models/manual_registration_view_model.dart';
import 'package:bipou_frontend/models/operation_result.dart';
import 'package:bipou_frontend/models/participante_request.dart';
import 'package:bipou_frontend/models/participante_response.dart';
import 'package:bipou_frontend/models/registro_response.dart';
import 'package:bipou_frontend/models/tipo_acao.dart';
import 'package:bipou_frontend/services/api_service.dart';
import 'package:bipou_frontend/services/credenciamento_service.dart';
import 'package:flutter_test/flutter_test.dart';

void main() {
  late _CredenciamentoManualSpy credenciamento;
  late ManualRegistrationViewModel viewModel;

  setUp(() {
    credenciamento = _CredenciamentoManualSpy();
    viewModel = ManualRegistrationViewModel(
      credenciamento: credenciamento,
      dispositivoId: 'portaria-01',
    );
  });

  tearDown(() => viewModel.dispose());

  test('cadastra participante com os dados informados', () async {
    final result = await viewModel.salvar(
      nome: 'Participante Teste',
      cpf: '52998224725',
    );

    expect(result?.status, SyncStatus.enviado);
    expect(result?.data?.id, '6ba7b810-9dad-41d1-80b4-00c04fd430c8');
    expect(credenciamento.lastParticipante?.nome, 'Participante Teste');
    expect(credenciamento.lastParticipante?.cpf, '52998224725');
    expect(credenciamento.lastDispositivoId, 'portaria-01');
    expect(viewModel.salvando, isFalse);
  });

  test('retorna salvoOffline sem tratar indisponibilidade como erro', () async {
    credenciamento.offline = true;

    final result = await viewModel.salvar(
      nome: 'Participante Teste',
      cpf: '52998224725',
    );

    expect(result?.status, SyncStatus.salvoOffline);
    expect(viewModel.errorMessage, isNull);
  });

  test('mantem erro do backend para exibicao no formulario', () async {
    credenciamento.apiError = true;

    final result = await viewModel.salvar(
      nome: 'Participante Teste',
      cpf: '52998224725',
    );

    expect(result, isNull);
    expect(viewModel.errorMessage, 'CPF já cadastrado');
  });
}

final class _CredenciamentoManualSpy implements CredenciamentoGateway {
  bool offline = false;
  bool apiError = false;
  ParticipanteRequest? lastParticipante;
  String? lastDispositivoId;

  @override
  String get apiBaseUrl => 'http://localhost:8080';

  @override
  Future<OperationResult<ParticipanteResponse>> cadastrarParticipante({
    required ParticipanteRequest participante,
    required String dispositivoId,
  }) async {
    lastParticipante = participante;
    lastDispositivoId = dispositivoId;
    if (apiError) {
      throw const ApiRequestException('CPF já cadastrado', statusCode: 409);
    }
    if (offline) {
      return const OperationResult<ParticipanteResponse>.salvoOffline();
    }
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
  Future<List<ParticipanteResponse>> listarParticipantes() {
    throw UnimplementedError();
  }

  @override
  Future<void> verificarConexao() {
    throw UnimplementedError();
  }
}
