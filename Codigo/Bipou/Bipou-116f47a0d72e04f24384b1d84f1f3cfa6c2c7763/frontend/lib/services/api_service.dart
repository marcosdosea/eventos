import 'package:bipou_frontend/core/config/api_config.dart';
import 'package:bipou_frontend/models/frequencia_pendente.dart';
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

  Future<ParticipanteResponse> buscarParticipantePorCpf(
    String cpf, {
    int? eventoId,
  });

  Future<List<ParticipanteResponse>> listarParticipantes();
}

abstract interface class FrequenciaLoteApi {
  Future<FrequenciaLoteSyncResponse> sincronizarFrequenciasLote(
    List<FrequenciaPendente> lote,
  );
}

final class FrequenciaLoteRejeicao {
  const FrequenciaLoteRejeicao({
    required this.leituraId,
    required this.mensagem,
    this.codigo,
  });

  final String leituraId;
  final String? codigo;
  final String mensagem;
}

final class FrequenciaLoteSyncResponse {
  const FrequenciaLoteSyncResponse({
    required this.confirmados,
    this.rejeitados = const <FrequenciaLoteRejeicao>[],
  });

  factory FrequenciaLoteSyncResponse.fromJson(Map<String, dynamic> json) {
    final confirmadosRaw = json['confirmados'];
    final rejeitadosRaw = json['rejeitados'];

    final confirmados = <String>[];
    if (confirmadosRaw is List) {
      for (final item in confirmadosRaw) {
        if (item is String && item.trim().isNotEmpty) {
          confirmados.add(item.trim());
        }
      }
    }

    final rejeitados = <FrequenciaLoteRejeicao>[];
    if (rejeitadosRaw is List) {
      for (final item in rejeitadosRaw) {
        if (item is Map) {
          final map = Map<String, dynamic>.from(item);
          final leituraId = map['leituraId'];
          if (leituraId is String && leituraId.trim().isNotEmpty) {
            final codigo = map['codigo'];
            final mensagem = map['mensagem'];
            rejeitados.add(
              FrequenciaLoteRejeicao(
                leituraId: leituraId.trim(),
                codigo: codigo is String ? codigo : null,
                mensagem: mensagem is String && mensagem.trim().isNotEmpty
                    ? mensagem.trim()
                    : 'Registro recusado pela API.',
              ),
            );
          }
        }
      }
    }

    return FrequenciaLoteSyncResponse(
      confirmados: List<String>.unmodifiable(confirmados),
      rejeitados: List<FrequenciaLoteRejeicao>.unmodifiable(rejeitados),
    );
  }

  final List<String> confirmados;
  final List<FrequenciaLoteRejeicao> rejeitados;
}

final class BoralaSessionCredentials {
  const BoralaSessionCredentials({this.baseUrl, this.accessToken});

  final String? baseUrl;
  final String? accessToken;
}

typedef BoralaCredentialsProvider = Future<BoralaSessionCredentials?> Function();

final class ApiService implements CredenciamentoApi, FrequenciaLoteApi {
  ApiService({
    Dio? dio,
    String? baseUrl,
    this._boralaCredentialsProvider,
    this._boralaSessionRefresher,
  }) : _baseUrl = baseUrl ?? ApiConfig.baseUrl,
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
  final BoralaCredentialsProvider? _boralaCredentialsProvider;
  final BoralaCredentialsProvider? _boralaSessionRefresher;
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
  Future<ParticipanteResponse> buscarParticipantePorCpf(
    String cpf, {
    int? eventoId,
  }) async {
    final json = await _post(
      '/api/participantes/consultar-por-cpf',
      <String, Object?>{'cpf': cpf, 'eventoId': ?eventoId},
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

  @override
  Future<FrequenciaLoteSyncResponse> sincronizarFrequenciasLote(
    List<FrequenciaPendente> lote,
  ) async {
    if (lote.isEmpty) {
      return const FrequenciaLoteSyncResponse(confirmados: <String>[]);
    }

    final comContextoBorala = lote
        .where((item) => item.possuiContextoBorala)
        .toList(growable: false);
    final somenteLegado = lote
        .where((item) => !item.possuiContextoBorala)
        .toList(growable: false);

    final confirmados = <String>[];
    final rejeitados = <FrequenciaLoteRejeicao>[];

    if (comContextoBorala.isNotEmpty) {
      try {
        final credentials = await _boralaCredentialsProvider?.call();
        FrequenciaLoteSyncResponse parsed;
        try {
          parsed = await _enviarLoteBorala(comContextoBorala, credentials);
        } on ApiRequestException catch (error) {
          if (error.statusCode == 401 && _boralaSessionRefresher != null) {
            final refreshed = await _boralaSessionRefresher.call();
            final refreshedToken = refreshed?.accessToken?.trim();
            if (refreshedToken != null && refreshedToken.isNotEmpty) {
              parsed = await _enviarLoteBorala(comContextoBorala, refreshed);
            } else {
              rethrow;
            }
          } else {
            rethrow;
          }
        }
        confirmados.addAll(parsed.confirmados);
        rejeitados.addAll(parsed.rejeitados);
      } on ApiRequestException catch (error) {
        if (error.statusCode == 404 || error.statusCode == 405) {
          await _enviarIndividualmentePorRegistro(
            comContextoBorala,
            confirmados: confirmados,
            rejeitados: rejeitados,
          );
        } else {
          rethrow;
        }
      }
    }

    if (somenteLegado.isNotEmpty) {
      await _enviarIndividualmentePorRegistro(
        somenteLegado,
        confirmados: confirmados,
        rejeitados: rejeitados,
      );
    }

    return FrequenciaLoteSyncResponse(
      confirmados: List<String>.unmodifiable(confirmados),
      rejeitados: List<FrequenciaLoteRejeicao>.unmodifiable(rejeitados),
    );
  }

  Future<FrequenciaLoteSyncResponse> _enviarLoteBorala(
    List<FrequenciaPendente> lote,
    BoralaSessionCredentials? credentials,
  ) async {
    final boralaBaseUrl = credentials?.baseUrl?.trim();
    final endpoint = (boralaBaseUrl != null && boralaBaseUrl.isNotEmpty)
        ? '${ApiConfig.normalizeHttpUrl(boralaBaseUrl, fieldName: 'BORALA_API_BASE_URL')}/api/bipou/v1/frequencias/lote'
        : '/api/bipou/v1/frequencias/lote';
    final token = credentials?.accessToken?.trim();
    final options = (token != null && token.isNotEmpty)
        ? Options(headers: <String, Object>{'Authorization': 'Bearer $token'})
        : null;

    final json = await _post(
      endpoint,
      <String, Object?>{
        'leituras': lote
            .map((item) => item.toBipouLoteJson())
            .toList(growable: false),
      },
      options: options,
    );
    return _parseResponse(json, FrequenciaLoteSyncResponse.fromJson);
  }

  Future<void> _enviarIndividualmentePorRegistro(
    List<FrequenciaPendente> itens, {
    required List<String> confirmados,
    required List<FrequenciaLoteRejeicao> rejeitados,
  }) async {
    for (final item in itens) {
      try {
        await registrar(item.toLeituraQrCodeRequest());
        confirmados.add(item.leituraId);
      } on ApiUnavailableException {
        if (confirmados.isEmpty && rejeitados.isEmpty) {
          rethrow;
        }
        break;
      } on ApiRequestException catch (error) {
        rejeitados.add(
          FrequenciaLoteRejeicao(
            leituraId: item.leituraId,
            codigo: error.statusCode?.toString(),
            mensagem: error.message,
          ),
        );
      }
    }
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
          _extractServerMessage(
            error.response?.data,
            statusCode: error.response?.statusCode,
          ),
          statusCode: error.response?.statusCode,
          cause: error,
        ),
        stackTrace,
      );
    }
  }

  Future<Object?> _post(
    String path,
    Map<String, Object?> body, {
    Options? options,
  }) async {
    try {
      final response = await _dio.post<Object?>(
        path,
        data: body,
        options: options,
      );
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
          _extractServerMessage(
            error.response?.data,
            statusCode: error.response?.statusCode,
          ),
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

  String _extractServerMessage(Object? data, {int? statusCode}) {
    if (data is Map) {
      for (final key in const <String>[
        'mensagem',
        'message',
        'detail',
        'title',
      ]) {
        final value = data[key];
        if (value is String && value.trim().isNotEmpty) {
          return value.trim();
        }
      }
    }

    return switch (statusCode) {
      401 =>
        'Sessao expirada ou nao autenticada. Conecte-se novamente na Sessao de Portaria.',
      403 => 'Operador sem permissao para registrar frequencia neste evento.',
      404 =>
        'Endpoint nao encontrado na API (HTTP 404). Verifique a URL configurada.',
      final int code => 'A API recusou a operacao (HTTP $code).',
      null => 'A API recusou a operacao.',
    };
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
