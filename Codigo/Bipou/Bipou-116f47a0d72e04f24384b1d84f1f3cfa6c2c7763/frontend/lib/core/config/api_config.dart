final class ApiConfig {
  ApiConfig._();

  static const String _configuredBaseUrl = String.fromEnvironment(
    'API_BASE_URL',
  );

  static String get baseUrl {
    if (_configuredBaseUrl.isEmpty) {
      throw StateError(
        'API_BASE_URL nao configurada. Informe o IP da Tailnet com '
        '--dart-define=API_BASE_URL=http://100.x.y.z:8080',
      );
    }

    final uri = Uri.tryParse(_configuredBaseUrl);
    final isHttp = uri?.scheme == 'http' || uri?.scheme == 'https';
    if (uri == null || !uri.hasAuthority || !isHttp) {
      throw StateError('API_BASE_URL deve ser uma URL HTTP(S) absoluta.');
    }

    return _configuredBaseUrl.endsWith('/')
        ? _configuredBaseUrl.substring(0, _configuredBaseUrl.length - 1)
        : _configuredBaseUrl;
  }
}
