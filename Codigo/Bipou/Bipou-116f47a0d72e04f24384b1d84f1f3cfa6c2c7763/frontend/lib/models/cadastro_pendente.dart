import 'package:bipou_frontend/core/validation/email_validator.dart';
import 'package:bipou_frontend/models/participante_request.dart';

final class CadastroPendente {
  CadastroPendente({
    required this.cadastroId,
    required this.nome,
    required this.cpf,
    String? email,
    required this.dispositivoId,
    required this.criadoEm,
  }) : email = EmailValidator.normalize(email) {
    ParticipanteRequest(
      nome: nome,
      cpf: cpf,
      email: this.email,
      cadastroId: cadastroId,
    );
    if (dispositivoId.trim().isEmpty) {
      throw ArgumentError.value(
        dispositivoId,
        'dispositivoId',
        'O dispositivo deve ser informado',
      );
    }
  }

  factory CadastroPendente.fromJson(Map<String, dynamic> json) {
    final cadastroId = json['cadastroId'];
    final nome = json['nome'];
    final cpf = json['cpf'];
    final email = json['email'];
    final dispositivoId = json['dispositivoId'];
    final criadoEm = json['criadoEm'];
    if (cadastroId is! String ||
        nome is! String ||
        cpf is! String ||
        (email != null && email is! String) ||
        dispositivoId is! String ||
        criadoEm is! String) {
      throw const FormatException('Cadastro pendente inválido.');
    }

    final dataHora = DateTime.tryParse(criadoEm);
    if (dataHora == null || !dataHora.isUtc) {
      throw const FormatException('Data do cadastro pendente inválida.');
    }

    try {
      return CadastroPendente(
        cadastroId: cadastroId,
        nome: nome,
        cpf: cpf,
        email: email as String?,
        dispositivoId: dispositivoId,
        criadoEm: dataHora,
      );
    } on ArgumentError catch (error) {
      throw FormatException('Cadastro pendente inválido.', error);
    }
  }

  final String cadastroId;
  final String nome;
  final String cpf;
  final String? email;
  final String dispositivoId;
  final DateTime criadoEm;

  ParticipanteRequest toRequest() => ParticipanteRequest(
    nome: nome,
    cpf: cpf,
    email: email,
    cadastroId: cadastroId,
  );

  Map<String, String> toJson() => <String, String>{
    'cadastroId': cadastroId,
    'nome': nome.trim(),
    'cpf': cpf.trim(),
    'email': ?email,
    'dispositivoId': dispositivoId.trim(),
    'criadoEm': criadoEm.toUtc().toIso8601String(),
  };
}
