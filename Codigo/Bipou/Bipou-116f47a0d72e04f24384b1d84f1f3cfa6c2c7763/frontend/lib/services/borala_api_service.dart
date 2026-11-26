import 'dart:typed_data';

import 'package:bipou_frontend/core/config/api_config.dart';
import 'package:bipou_frontend/models/borala_models.dart';
import 'package:bipou_frontend/services/api_service.dart';
import 'package:dio/dio.dart';

final class BoralaParticipantesResult {
  const BoralaParticipantesResult({
    required this.participantes,
    required this.notModified,
    this.etag,
  });

  final List<BoralaParticipante> participantes;
  final bool notModified;
  final String? etag;
}

abstract interface class BoralaApiClient {
  Future<BoralaSessaoToken> autenticar({
    required String baseUrl,
    required String usuario,
    required String senha,
  });

  Future<BoralaSessaoToken> renovarSessao({
    required String baseUrl,
    required String refreshToken,
  });

  Future<List<BoralaEvento>> listarEventos({
    required String baseUrl,
    required String accessToken,
  });

  Future<List<BoralaSubevento>> listarSubeventos({
    required String baseUrl,
    required String accessToken,
    required int eventoId,
  });

  Future<BoralaParticipantesResult> listarParticipantes({
    required String baseUrl,
    required String accessToken,
    required int eventoId,
    int? subeventoId,
    String? ifNoneMatch,
  });

  Future<BoralaCargaOffline> baixarCargaOffline({
    required String baseUrl,
    required String accessToken,
    required int eventoId,
  });
}

final class BoralaApiService implements BoralaApiClient {
  BoralaApiService({Dio? dio})
    : _dio =
          dio ??
          Dio(
            BaseOptions(
              connectTimeout: const Duration(seconds: 8),
              sendTimeout: const Duration(seconds: 8),
              receiveTimeout: const Duration(seconds: 15),
              headers: const <String, Object>{'Accept': 'application/json'},
            ),
          );

  final Dio _dio;

  @override
  Future<BoralaSessaoToken> autenticar({
    required String baseUrl,
    required String usuario,
    required String senha,
  }) async {
    final root = ApiConfig.normalizeHttpUrl(
      baseUrl,
      fieldName: 'URL da API do Boralá',
    );

    try {
      final response = await _dio.post<Object?>(
        '$root/api/bipou/v1/sessao',
        data: <String, String>{'usuario': usuario.trim(), 'senha': senha},
        options: Options(contentType: Headers.jsonContentType),
      );
      if (response.data is! Map) {
        throw const ApiRequestException(
          'Resposta invalida ao autenticar no Borala.',
        );
      }
      return BoralaSessaoToken.fromJson(
        Map<String, dynamic>.from(response.data! as Map),
      );
    } on DioException catch (error, stackTrace) {
      _rethrowDioError(
        error,
        stackTrace,
        unauthorizedMessage: 'Usuario ou senha invalidos no Borala.',
        forbiddenMessage:
            'Esta conta nao possui perfil de Gestor ou Colaborador.',
      );
    } on FormatException catch (error, stackTrace) {
      Error.throwWithStackTrace(
        ApiRequestException(
          'Resposta invalida ao autenticar no Borala.',
          cause: error,
        ),
        stackTrace,
      );
    }
  }

  @override
  Future<BoralaSessaoToken> renovarSessao({
    required String baseUrl,
    required String refreshToken,
  }) async {
    final root = ApiConfig.normalizeHttpUrl(
      baseUrl,
      fieldName: 'URL da API do Borala',
    );

    try {
      final response = await _dio.post<Object?>(
        '$root/api/bipou/v1/sessao/refresh',
        data: <String, String>{'refreshToken': refreshToken},
        options: Options(contentType: Headers.jsonContentType),
      );
      if (response.data is! Map) {
        throw const ApiRequestException(
          'Resposta invalida ao renovar sessao no Borala.',
        );
      }
      return BoralaSessaoToken.fromJson(
        Map<String, dynamic>.from(response.data! as Map),
      );
    } on DioException catch (error, stackTrace) {
      _rethrowDioError(
        error,
        stackTrace,
        unauthorizedMessage: 'Sessao expirada. Faca login novamente.',
        forbiddenMessage:
            'Esta conta nao possui perfil de Gestor ou Colaborador.',
      );
    } on FormatException catch (error, stackTrace) {
      Error.throwWithStackTrace(
        ApiRequestException(
          'Resposta invalida ao renovar sessao no Borala.',
          cause: error,
        ),
        stackTrace,
      );
    }
  }

  @override
  Future<List<BoralaEvento>> listarEventos({
    required String baseUrl,
    required String accessToken,
  }) async {
    final root = ApiConfig.normalizeHttpUrl(
      baseUrl,
      fieldName: 'URL da API do Boralá',
    );
    try {
      final response = await _dio.get<Object?>(
        '$root/api/bipou/v1/eventos',
        options: _authOptions(accessToken),
      );
      if (response.data is! List) {
        throw const ApiRequestException(
          'Resposta invalida ao listar eventos do Borala.',
        );
      }
      return (response.data! as List).map((item) {
        if (item is! Map) {
          throw const FormatException('Evento invalido na resposta.');
        }
        return BoralaEvento.fromJson(Map<String, dynamic>.from(item));
      }).toList();
    } on DioException catch (error, stackTrace) {
      _rethrowDioError(error, stackTrace);
    } on FormatException catch (error, stackTrace) {
      Error.throwWithStackTrace(
        ApiRequestException(
          'Resposta invalida ao listar eventos do Borala.',
          cause: error,
        ),
        stackTrace,
      );
    }
  }

  @override
  Future<List<BoralaSubevento>> listarSubeventos({
    required String baseUrl,
    required String accessToken,
    required int eventoId,
  }) async {
    final root = ApiConfig.normalizeHttpUrl(
      baseUrl,
      fieldName: 'URL da API do Boralá',
    );
    try {
      final response = await _dio.get<Object?>(
        '$root/api/bipou/v1/eventos/$eventoId/subeventos',
        options: _authOptions(accessToken),
      );
      if (response.data is! List) {
        throw const ApiRequestException(
          'Resposta invalida ao listar subeventos do Borala.',
        );
      }
      return (response.data! as List).map((item) {
        if (item is! Map) {
          throw const FormatException('Subevento invalido na resposta.');
        }
        return BoralaSubevento.fromJson(Map<String, dynamic>.from(item));
      }).toList();
    } on DioException catch (error, stackTrace) {
      _rethrowDioError(error, stackTrace);
    } on FormatException catch (error, stackTrace) {
      Error.throwWithStackTrace(
        ApiRequestException(
          'Resposta invalida ao listar subeventos do Borala.',
          cause: error,
        ),
        stackTrace,
      );
    }
  }

  @override
  Future<BoralaParticipantesResult> listarParticipantes({
    required String baseUrl,
    required String accessToken,
    required int eventoId,
    int? subeventoId,
    String? ifNoneMatch,
  }) async {
    final root = ApiConfig.normalizeHttpUrl(
      baseUrl,
      fieldName: 'URL da API do Boralá',
    );
    final headers = <String, Object>{'Authorization': 'Bearer $accessToken'};
    if (ifNoneMatch != null && ifNoneMatch.trim().isNotEmpty) {
      headers['If-None-Match'] = ifNoneMatch.trim();
    }

    try {
      final response = await _dio.get<Object?>(
        '$root/api/bipou/v1/eventos/$eventoId/participantes',
        queryParameters: <String, Object>{'subEventoId': ?subeventoId},
        options: Options(
          headers: headers,
          validateStatus: (status) =>
              status != null && ((status >= 200 && status < 300) || status == 304),
        ),
      );

      final etag = response.headers.value('etag');
      if (response.statusCode == 304) {
        return BoralaParticipantesResult(
          participantes: const <BoralaParticipante>[],
          notModified: true,
          etag: etag ?? ifNoneMatch,
        );
      }

      if (response.data is! List) {
        throw const ApiRequestException(
          'Resposta invalida ao baixar participantes do Borala.',
        );
      }
      final participantes = (response.data! as List).map((item) {
        if (item is! Map) {
          throw const FormatException('Participante invalido na resposta.');
        }
        return BoralaParticipante.fromJson(Map<String, dynamic>.from(item));
      }).toList();

      return BoralaParticipantesResult(
        participantes: participantes,
        notModified: false,
        etag: etag,
      );
    } on DioException catch (error, stackTrace) {
      _rethrowDioError(error, stackTrace);
    } on FormatException catch (error, stackTrace) {
      Error.throwWithStackTrace(
        ApiRequestException(
          'Resposta invalida ao baixar participantes do Borala.',
          cause: error,
        ),
        stackTrace,
      );
    }
  }

  @override
  Future<BoralaCargaOffline> baixarCargaOffline({
    required String baseUrl,
    required String accessToken,
    required int eventoId,
  }) async {
    final root = ApiConfig.normalizeHttpUrl(
      baseUrl,
      fieldName: 'URL da API do Boralá',
    );

    try {
      final response = await _dio.get<List<int>>(
        '$root/api/bipou/v1/eventos/$eventoId/participantes/export-carga',
        options: Options(
          headers: <String, Object>{'Authorization': 'Bearer $accessToken'},
          responseType: ResponseType.bytes,
        ),
      );
      final bytes = response.data;
      if (bytes == null || bytes.isEmpty) {
        throw const ApiRequestException('Pacote de carga offline vazio.');
      }
      return BoralaCargaOffline.fromRawBytes(Uint8List.fromList(bytes));
    } on DioException catch (error, stackTrace) {
      _rethrowDioError(error, stackTrace);
    } on FormatException catch (error, stackTrace) {
      Error.throwWithStackTrace(
        ApiRequestException(
          'Pacote de carga offline em formato invalido.',
          cause: error,
        ),
        stackTrace,
      );
    }
  }

  Options _authOptions(String accessToken) {
    return Options(
      headers: <String, Object>{'Authorization': 'Bearer $accessToken'},
    );
  }

  Never _rethrowDioError(
    DioException error,
    StackTrace stackTrace, {
    String? unauthorizedMessage,
    String? forbiddenMessage,
  }) {
    final statusCode = error.response?.statusCode;
    final unavailable =
        (statusCode != null && statusCode >= 500) ||
        switch (error.type) {
          DioExceptionType.connectionTimeout ||
          DioExceptionType.sendTimeout ||
          DioExceptionType.receiveTimeout ||
          DioExceptionType.connectionError => true,
          _ => false,
        };

    if (unavailable) {
      Error.throwWithStackTrace(
        ApiUnavailableException('API do Borala indisponivel.', error),
        stackTrace,
      );
    }

    final data = error.response?.data;
    final serverMessage = switch (data) {
      {'mensagem': final String msg} when msg.trim().isNotEmpty => msg.trim(),
      {'message': final String msg} when msg.trim().isNotEmpty => msg.trim(),
      _ => null,
    };

    if (statusCode == 401) {
      Error.throwWithStackTrace(
        ApiRequestException(
          serverMessage ??
              unauthorizedMessage ??
              'Sessao expirada ou nao autorizada. Faca login novamente.',
          statusCode: 401,
          cause: error,
        ),
        stackTrace,
      );
    }

    if (statusCode == 403) {
      Error.throwWithStackTrace(
        ApiRequestException(
          serverMessage ??
              forbiddenMessage ??
              'Voce nao tem permissao de portaria para este evento.',
          statusCode: 403,
          cause: error,
        ),
        stackTrace,
      );
    }

    if (statusCode == 423) {
      Error.throwWithStackTrace(
        ApiRequestException(
          serverMessage ??
              'Conta temporariamente bloqueada por excesso de tentativas. Aguarde alguns minutos e tente novamente.',
          statusCode: 423,
          cause: error,
        ),
        stackTrace,
      );
    }

    if (statusCode == 429) {
      Error.throwWithStackTrace(
        ApiRequestException(
          serverMessage ??
              'Muitas tentativas de autenticacao. Aguarde um minuto e tente novamente.',
          statusCode: 429,
          cause: error,
        ),
        stackTrace,
      );
    }

    final message = serverMessage ?? 'A API do Borala recusou a requisicao.';

    Error.throwWithStackTrace(
      ApiRequestException(message, statusCode: statusCode, cause: error),
      stackTrace,
    );
  }
}
