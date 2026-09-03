import 'package:bipou_frontend/core/validation/cpf_validator.dart';
import 'package:flutter_test/flutter_test.dart';

void main() {
  test('aceita CPF com dígitos verificadores válidos', () {
    expect(CpfValidator.isValid('52998224725'), isTrue);
    expect(CpfValidator.isValid('11144477735'), isTrue);
  });

  test('rejeita CPF com cálculo inválido ou dígitos repetidos', () {
    expect(CpfValidator.isValid('12345678900'), isFalse);
    expect(CpfValidator.isValid('11111111111'), isFalse);
  });

  test('rejeita CPF incompleto ou com caracteres não numéricos', () {
    expect(CpfValidator.isValid('5299822472'), isFalse);
    expect(CpfValidator.isValid('529.982.247-25'), isFalse);
  });
}
