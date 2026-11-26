import 'dart:convert';
import 'dart:typed_data';

import 'package:bipou_frontend/core/config/api_config.dart';
import 'package:bipou_frontend/services/api_service.dart';
import 'package:dio/dio.dart';
import 'package:flutter_test/flutter_test.dart';

void main() {
  test(
    'listarParticipantes consulta endpoint do Borala com Bearer quando sessao esta ativa',
    () async {
      RequestOptions? capturedRequest;
      final dio = Dio(BaseOptions(baseUrl: ApiConfig.defaultBoralaApiBaseUrl))
        ..httpClientAdapter = _FakeHttpClientAdapter((options) {
          capturedRequest = options;
          return ResponseBody.fromString(
            jsonEncode(<Map<String, Object?>>[
              <String, Object?>{
                'idPessoa': 101,
                'nome': 'Ana Silva',
                'nomeCracha': 'Ana',
                'cpf': '52998224725',
                'statusInscricao': 'A',
                'subeventosInscritos': <int>[42],
              },
            ]),
            200,
            headers: <String, List<String>>{
              Headers.contentTypeHeader: <String>[Headers.jsonContentType],
            },
          );
        });

      final api = ApiService(
        dio: dio,
        baseUrl: ApiConfig.defaultBoralaApiBaseUrl,
        boralaCredentialsProvider: () async => const BoralaSessionCredentials(
          baseUrl: 'https://borala-api.itatechjr.com.br',
          accessToken: 'token-secreto',
          eventoId: 15,
          subeventoId: 42,
        ),
      );

      final participantes = await api.listarParticipantes();

      expect(capturedRequest, isNotNull);
      expect(
        capturedRequest!.path,
        'https://borala-api.itatechjr.com.br/api/bipou/v1/eventos/15/participantes',
      );
      expect(capturedRequest!.queryParameters['subEventoId'], 42);
      expect(
        capturedRequest!.headers['Authorization'],
        'Bearer token-secreto',
      );
      expect(participantes, hasLength(1));
      expect(participantes.single.nome, 'Ana Silva');
      expect(participantes.single.cpf, '52998224725');
      expect(participantes.single.idPessoaBorala, 101);
      expect(participantes.single.eventoId, 15);
      expect(participantes.single.subeventoIds, <int>[42]);
    },
  );

  test(
    'listarParticipantes solicita sessao de portaria ao usar API Borala sem autenticacao',
    () async {
      var requestsFeitas = 0;
      final dio = Dio(BaseOptions(baseUrl: ApiConfig.defaultBoralaApiBaseUrl))
        ..httpClientAdapter = _FakeHttpClientAdapter((_) {
          requestsFeitas++;
          return ResponseBody.fromString('', 404);
        });

      final api = ApiService(
        dio: dio,
        baseUrl: ApiConfig.defaultBoralaApiBaseUrl,
        boralaCredentialsProvider: () async => null,
      );

      await expectLater(
        api.listarParticipantes(),
        throwsA(
          isA<ApiRequestException>()
              .having((e) => e.statusCode, 'statusCode', 401)
              .having(
                (e) => e.message,
                'message',
                contains('Sessao de Portaria'),
              ),
        ),
      );
      expect(requestsFeitas, 0);
    },
  );

  test(
    'listarParticipantes mantem compatibilidade com endpoint legado /api/participantes',
    () async {
      RequestOptions? capturedRequest;
      final dio = Dio(BaseOptions(baseUrl: 'http://localhost:8080'))
        ..httpClientAdapter = _FakeHttpClientAdapter((options) {
          capturedRequest = options;
          return ResponseBody.fromString(
            jsonEncode(<Map<String, Object?>>[
              <String, Object?>{
                'id': '6ba7b810-9dad-41d1-80b4-00c04fd430c8',
                'nome': 'Carlos Legado',
                'cpf': '11144477735',
              },
            ]),
            200,
            headers: <String, List<String>>{
              Headers.contentTypeHeader: <String>[Headers.jsonContentType],
            },
          );
        });

      final api = ApiService(dio: dio, baseUrl: 'http://localhost:8080');

      final participantes = await api.listarParticipantes();

      expect(capturedRequest?.path, '/api/participantes');
      expect(participantes, hasLength(1));
      expect(participantes.single.nome, 'Carlos Legado');
    },
  );
}

final class _FakeHttpClientAdapter implements HttpClientAdapter {
  _FakeHttpClientAdapter(this._handler);

  final ResponseBody Function(RequestOptions options) _handler;

  @override
  Future<ResponseBody> fetch(
    RequestOptions options,
    Stream<Uint8List>? requestStream,
    Future<void>? cancelFuture,
  ) async {
    return _handler(options);
  }

  @override
  void close({bool force = false}) {}
}
