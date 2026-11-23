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

final class ApiService implements CredenciamentoApi, FrequenciaLoteApi {
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
        final json = await _post(
          '/api/bipou/v1/frequencias/lote',
          <String, Object?>{
            'leituras': comContextoBorala
                .map((item) => item.toBipouLoteJson())
                .toList(growable: false),
          },
        );
        final parsed = _parseResponse(
          json,
          FrequenciaLoteSyncResponse.fromJson,
        );
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
