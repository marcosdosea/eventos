import 'package:bipou_frontend/core/config/api_config.dart';
import 'package:bipou_frontend/models/leitura_qr_code_request.dart';
import 'package:bipou_frontend/models/participante_request.dart';
import 'package:bipou_frontend/models/participante_response.dart';
import 'package:bipou_frontend/models/registro_response.dart';
import 'package:dio/dio.dart';

abstract interface class CredenciamentoApi {
  String get baseUrl;

  Future<RegistroResponse> registrar(LeituraQrCodeRequest request);

  Future<ParticipanteResponse> cadastrarParticipante(
    ParticipanteRequest request,
  );

  Future<ParticipanteResponse> buscarParticipantePorCpf(String cpf);

  Future<List<ParticipanteResponse>> listarParticipantes();
}

final class ApiService implements CredenciamentoApi {
  ApiService({Dio? dio, String? baseUrl})
    : _baseUrl = baseUrl ?? ApiConfig.baseUrl,
      _dio =
          dio ??
          Dio(
            BaseOptions(
              baseUrl: baseUrl ?? ApiConfig.baseUrl,
              connectTimeout: const Duration(seconds: 5),
              sendTimeout: const Duration(seconds: 5),
              receiveTimeout: const Duration(seconds: 8),
              contentType: Headers.jsonContentType,
              responseType: ResponseType.json,
              headers: const <String, Object>{'Accept': 'application/json'},
            ),
          );

  final String _baseUrl;
  final Dio _dio;

  @override
  String get baseUrl => _baseUrl;

  @override
  Future<RegistroResponse> registrar(LeituraQrCodeRequest request) async {
    final json = await _post('/api/registros', request.toJson());
    return _parseResponse(json, RegistroResponse.fromJson);
  }

  @override
  Future<ParticipanteResponse> cadastrarParticipante(
    ParticipanteRequest request,
  ) async {
    final json = await _post('/api/participantes', request.toJson());
    return _parseResponse(json, ParticipanteResponse.fromJson);
  }

  @override
  Future<ParticipanteResponse> buscarParticipantePorCpf(String cpf) async {
    final json = await _post(
      '/api/participantes/consultar-por-cpf',
      <String, Object?>{'cpf': cpf},
    );
    return _parseResponse(json, ParticipanteResponse.fromJson);
  }

  @override
  Future<List<ParticipanteResponse>> listarParticipantes() async {
    final json = await _get('/api/participantes');
    if (json is! List) {
      throw const ApiRequestException('Resposta invalida recebida da API.');
    }
    return json
        .map((item) => _parseResponse(item, ParticipanteResponse.fromJson))
        .toList();
  }

  Future<Object?> _get(String path) async {
    try {
      final response = await _dio.get<Object?>(path);
      return response.data;
    } on DioException catch (error, stackTrace) {
      if (_isUnavailable(error)) {
        Error.throwWithStackTrace(
          ApiUnavailableException('API local indisponivel.', error),
          stackTrace,
        );
      }

      Error.throwWithStackTrace(
        ApiRequestException(
          _extractServerMessage(error.response?.data),
          statusCode: error.response?.statusCode,
          cause: error,
        ),
        stackTrace,
      );
    }
  }

  Future<Object?> _post(String path, Map<String, Object?> body) async {
    try {
      final response = await _dio.post<Object?>(path, data: body);
      return response.data;
    } on DioException catch (error, stackTrace) {
      if (_isUnavailable(error)) {
        Error.throwWithStackTrace(
          ApiUnavailableException('API local indisponivel.', error),
          stackTrace,
        );
      }

      Error.throwWithStackTrace(
        ApiRequestException(
          _extractServerMessage(error.response?.data),
          statusCode: error.response?.statusCode,
          cause: error,
        ),
        stackTrace,
      );
    }
  }

  T _parseResponse<T>(Object? data, T Function(Map<String, dynamic>) parser) {
    if (data is! Map) {
      throw const ApiRequestException('Resposta invalida recebida da API.');
    }

    try {
      return parser(Map<String, dynamic>.from(data));
    } on FormatException catch (error, stackTrace) {
      Error.throwWithStackTrace(
        ApiRequestException('Resposta invalida recebida da API.', cause: error),
        stackTrace,
      );
    }
  }

  bool _isUnavailable(DioException error) {
    final statusCode = error.response?.statusCode;
    if (statusCode != null && statusCode >= 500) {
      return true;
    }

    return switch (error.type) {
      DioExceptionType.connectionTimeout ||
      DioExceptionType.sendTimeout ||
      DioExceptionType.receiveTimeout ||
      DioExceptionType.connectionError => true,
      _ => false,
    };
  }

  String _extractServerMessage(Object? data) {
    if (data case {
      'mensagem': final String mensagem,
    } when mensagem.isNotEmpty) {
      return mensagem;
    }
    return 'A API recusou a operacao.';
  }
}

final class ApiUnavailableException implements Exception {
  const ApiUnavailableException(this.message, [this.cause]);

  final String message;
  final Object? cause;

  @override
  String toString() => 'ApiUnavailableException: $message';
}

final class ApiRequestException implements Exception {
  const ApiRequestException(this.message, {this.statusCode, this.cause});

  final String message;
  final int? statusCode;
  final Object? cause;

  @override
  String toString() => 'ApiRequestException($statusCode): $message';
}
