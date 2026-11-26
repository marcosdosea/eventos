import 'package:bipou_frontend/core/validation/email_validator.dart';
import 'package:flutter_test/flutter_test.dart';

void main() {
  test('aceita e-mails validos e normaliza espacos e maiusculas', () {
    expect(EmailValidator.isValid('maria.silva@ufs.br'), isTrue);
    expect(EmailValidator.isValid('  Joao+Teste@Exemplo.Com.Br  '), isTrue);
    expect(
      EmailValidator.normalize('  Joao+Teste@Exemplo.Com.Br  '),
      'joao+teste@exemplo.com.br',
    );
  });

  test('rejeita e-mails vazios ou com formato invalido', () {
    expect(EmailValidator.isValid(''), isFalse);
    expect(EmailValidator.isValid('   '), isFalse);
    expect(EmailValidator.isValid('sem-arroba.com'), isFalse);
    expect(EmailValidator.isValid('usuario@'), isFalse);
    expect(EmailValidator.isValid('usuario@dominio'), isFalse);
    expect(EmailValidator.isValid('usuario com espaco@ufs.br'), isFalse);
    expect(EmailValidator.normalize('   '), isNull);
  });
}
