final class CpfValidator {
  CpfValidator._();

  static bool isValid(String value) {
    final cpf = value.trim();
    if (!RegExp(r'^\d{11}$').hasMatch(cpf)) {
      return false;
    }
    if (RegExp(r'^(\d)\1{10}$').hasMatch(cpf)) {
      return false;
    }

    final primeiroDigito = _calculateDigit(cpf, 9, 10);
    final segundoDigito = _calculateDigit(cpf, 10, 11);
    return primeiroDigito == int.parse(cpf[9]) &&
        segundoDigito == int.parse(cpf[10]);
  }

  static int _calculateDigit(String cpf, int quantidade, int pesoInicial) {
    var soma = 0;
    for (var indice = 0; indice < quantidade; indice++) {
      soma += int.parse(cpf[indice]) * (pesoInicial - indice);
    }
    final resto = soma % 11;
    return resto < 2 ? 0 : 11 - resto;
  }
}
