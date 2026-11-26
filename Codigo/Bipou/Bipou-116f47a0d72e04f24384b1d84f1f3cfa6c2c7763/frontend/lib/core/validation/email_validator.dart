final class EmailValidator {
  EmailValidator._();

  static const int maxLength = 254;

  static final RegExp _emailPattern = RegExp(
    r"^[a-zA-Z0-9.!#$%&'*+/=?^_`{|}~-]+@[a-zA-Z0-9](?:[a-zA-Z0-9-]{0,61}[a-zA-Z0-9])?(?:\.[a-zA-Z0-9](?:[a-zA-Z0-9-]{0,61}[a-zA-Z0-9])?)+$",
  );

  static String? normalize(String? value) {
    if (value == null) {
      return null;
    }
    final trimmed = value.trim();
    if (trimmed.isEmpty) {
      return null;
    }
    return trimmed.toLowerCase();
  }

  static bool isValid(String value) {
    final normalized = normalize(value);
    if (normalized == null || normalized.length > maxLength) {
      return false;
    }
    return _emailPattern.hasMatch(normalized);
  }
}
