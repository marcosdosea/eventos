final class ApiConfig {
  ApiConfig._();

  static const String _configuredBaseUrl = String.fromEnvironment(
    'API_BASE_URL',
  );

  static const String _configuredBoralaBaseUrl = String.fromEnvironment(
    'BORALA_API_BASE_URL',
  );

  static String get baseUrl {
    if (_configuredBaseUrl.isEmpty) {
      throw StateError(
        'API_BASE_URL nao configurada. Informe o IP da Tailnet com '
        '--dart-define=API_BASE_URL=http://100.x.y.z:8080',
      );
    }

    return normalizeHttpUrl(_configuredBaseUrl, fieldName: 'API_BASE_URL');
  }

  static String? get boralaBaseUrl {
    final trimmed = _configuredBoralaBaseUrl.trim();
    if (trimmed.isEmpty) {
      return null;
    }

    return normalizeHttpUrl(trimmed, fieldName: 'BORALA_API_BASE_URL');
  }

  static String normalizeHttpUrl(
    String rawUrl, {
    String fieldName = 'URL da API',
  }) {
    final trimmed = rawUrl.trim();
    final uri = Uri.tryParse(trimmed);
    final isHttp = uri?.scheme == 'http' || uri?.scheme == 'https';
    if (uri == null || !uri.hasAuthority || !isHttp) {
      throw StateError('$fieldName deve ser uma URL HTTP(S) absoluta.');
    }

    return trimmed.endsWith('/')
        ? trimmed.substring(0, trimmed.length - 1)
        : trimmed;
  }
}
