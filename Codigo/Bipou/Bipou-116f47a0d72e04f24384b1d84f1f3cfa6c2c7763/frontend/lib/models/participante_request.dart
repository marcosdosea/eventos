import 'package:bipou_frontend/core/validation/cpf_validator.dart';

final class ParticipanteRequest {
  ParticipanteRequest({
    required this.nome,
    required this.cpf,
    this.cadastroId,
  }) {
    if (nome.trim().isEmpty || nome.length > 150) {
      throw ArgumentError.value(
        nome,
        'nome',
        'O nome deve ter entre 1 e 150 caracteres',
      );
    }
    if (!RegExp(r'^\d{11}$').hasMatch(cpf.trim())) {
      throw ArgumentError.value(
        cpf,
        'cpf',
        'O CPF deve conter exatamente 11 digitos',
      );
    }
    if (!CpfValidator.isValid(cpf)) {
      throw ArgumentError.value(cpf, 'cpf', 'O CPF informado e invalido');
    }
    if (cadastroId != null && !_uuidPattern.hasMatch(cadastroId!)) {
      throw ArgumentError.value(cadastroId, 'cadastroId', 'UUID invalido');
    }
  }

  final String nome;
  final String cpf;
  final String? cadastroId;

  static final RegExp _uuidPattern = RegExp(
    r'^[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[1-8][0-9a-fA-F]{3}-[89abAB][0-9a-fA-F]{3}-[0-9a-fA-F]{12}$',
  );

  Map<String, Object?> toJson() => <String, Object?>{
    'nome': nome.trim(),
    'cpf': cpf.trim(),
    if (cadastroId != null) 'cadastroId': cadastroId,
  };

  ParticipanteRequest withCadastroId(String id) =>
      ParticipanteRequest(nome: nome, cpf: cpf, cadastroId: id);
}
