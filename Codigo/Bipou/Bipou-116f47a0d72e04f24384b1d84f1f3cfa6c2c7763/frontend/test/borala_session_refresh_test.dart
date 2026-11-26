import 'dart:convert';
import 'dart:typed_data';

import 'package:bipou_frontend/models/borala_models.dart';
import 'package:bipou_frontend/models/frequencia_pendente.dart';
import 'package:bipou_frontend/models/portaria_session.dart';
import 'package:bipou_frontend/models/tipo_acao.dart';
import 'package:bipou_frontend/services/api_service.dart';
import 'package:bipou_frontend/services/borala_api_service.dart';
import 'package:bipou_frontend/services/borala_session_manager.dart';
import 'package:bipou_frontend/services/local_participant_repository.dart';
import 'package:dio/dio.dart';
import 'package:flutter_test/flutter_test.dart';

void main() {
  group('PortariaSession token metadata', () {
    test('serializes and deserializes refreshToken and expiresAt', () {
      final exp = DateTime.utc(2026, 11, 25, 20, 0, 0);
      final session = PortariaSession(
        eventoId: 1,
        eventoNome: 'Evento Teste',
        totalParticipantesCarregados: 10,
        sincronizadoEm: DateTime.utc(2026, 11, 25, 12, 0, 0),
        boralaBaseUrl: 'https://borala.example.test',
        operadorUsuario: '11144477735',
        accessToken: 'access-1',
        refreshToken: 'refresh-1',
        expiresAt: exp,
      );

      final decoded = PortariaSession.fromJson(session.toJson());
      expect(decoded.refreshToken, 'refresh-1');
      expect(decoded.expiresAt, exp);
      expect(
        decoded.isTokenExpirandoOuExpirado(
          agora: DateTime.utc(2026, 11, 25, 19, 50, 0),
        ),
        isFalse,
      );
      expect(
        decoded.isTokenExpirandoOuExpirado(
          agora: DateTime.utc(2026, 11, 25, 19, 56, 0),
        ),
        isTrue,
      );
    });
  });

  group('BoralaSessionManager', () {
    test(
      'proactively refreshes expiring token and deduplicates concurrent refreshes',
      () async {
        final now = DateTime.utc(2026, 11, 25, 19, 57, 0);
        final repo = _FakeLocalParticipantRepository(
          PortariaSession(
            eventoId: 1,
            eventoNome: 'Evento Teste',
            totalParticipantesCarregados: 5,
            sincronizadoEm: DateTime.utc(2026, 11, 25, 12, 0, 0),
            boralaBaseUrl: 'https://borala.example.test',
            operadorUsuario: '11144477735',
            accessToken: 'old-access',
            refreshToken: 'old-refresh',
            expiresAt: DateTime.utc(2026, 11, 25, 20, 0, 0),
          ),
        );
        final api = _FakeBoralaApiClient();
        final manager = BoralaSessionManager(
          repository: repo,
          boralaApi: api,
          clock: () => now,
        );

        final results = await Future.wait([
          manager.getValidCredentials(),
          manager.refreshActiveSession(),
        ]);

        expect(api.refreshCalls, 1);
        expect(results[0]?.accessToken, 'new-access');
        expect(results[1]?.accessToken, 'new-access');

        final saved = await repo.loadSession();
        expect(saved?.accessToken, 'new-access');
        expect(saved?.refreshToken, 'new-refresh');
        expect(saved?.expiresAt, now.add(const Duration(hours: 8)));
      },
    );
  });

  group('BoralaApiService error messages and refresh', () {
    test('surfaces 423 lockout server message on autenticar', () async {
      final dio = Dio()
        ..httpClientAdapter = _CallbackAdapter((options) async {
          return ResponseBody.fromString(
            jsonEncode(<String, String>{
              'codigo': 'CONTA_BLOQUEADA',
              'mensagem': 'Conta temporariamente bloqueada.',
            }),
            423,
            headers: <String, List<String>>{
              Headers.contentTypeHeader: <String>[Headers.jsonContentType],
            },
          );
        });
      final service = BoralaApiService(dio: dio);

      await expectLater(
        service.autenticar(
          baseUrl: 'https://borala.example.test',
          usuario: '11144477735',
          senha: 'errada',
        ),
        throwsA(
          isA<ApiRequestException>()
              .having((e) => e.statusCode, 'statusCode', 423)
              .having(
                (e) => e.message,
                'message',
                'Conta temporariamente bloqueada.',
              ),
        ),
      );
    });

    test('renovarSessao returns new BoralaSessaoToken', () async {
      final dio = Dio()
        ..httpClientAdapter = _CallbackAdapter((options) async {
          expect(options.path, 'https://borala.example.test/api/bipou/v1/sessao/refresh');
          return ResponseBody.fromString(
            jsonEncode(<String, Object?>{
              'accessToken': 'fresh-access',
              'refreshToken': 'fresh-refresh',
              'expiresIn': 28800,
            }),
            200,
            headers: <String, List<String>>{
              Headers.contentTypeHeader: <String>[Headers.jsonContentType],
            },
          );
        });
      final service = BoralaApiService(dio: dio);

      final token = await service.renovarSessao(
        baseUrl: 'https://borala.example.test',
        refreshToken: 'old-refresh',
      );

      expect(token.accessToken, 'fresh-access');
      expect(token.refreshToken, 'fresh-refresh');
      expect(token.expiresIn, 28800);
    });
  });

  group('ApiService batch sync 401 refresh', () {
    test('refreshes credentials once on 401 and retries lote', () async {
      final authHeaders = <String>[];
      final dio = Dio()
        ..httpClientAdapter = _CallbackAdapter((options) async {
          final auth = options.headers['Authorization']?.toString() ?? '';
          authHeaders.add(auth);
          if (auth == 'Bearer expired-token') {
            return ResponseBody.fromString(
              '{}',
              401,
              headers: <String, List<String>>{
                Headers.contentTypeHeader: <String>[Headers.jsonContentType],
              },
            );
          }
          return ResponseBody.fromString(
            jsonEncode(<String, Object?>{
              'confirmados': <String>['00000000-0000-4000-8000-000000000001'],
              'rejeitados': <Object?>[],
            }),
            200,
            headers: <String, List<String>>{
              Headers.contentTypeHeader: <String>[Headers.jsonContentType],
            },
          );
        });

      var refreshCalled = 0;
      final apiService = ApiService(
        dio: dio,
        boralaCredentialsProvider: () async => const BoralaSessionCredentials(
          baseUrl: 'https://borala.example.test',
          accessToken: 'expired-token',
        ),
        boralaSessionRefresher: () async {
          refreshCalled++;
          return const BoralaSessionCredentials(
            baseUrl: 'https://borala.example.test',
            accessToken: 'refreshed-token',
          );
        },
      );

      final response = await apiService.sincronizarFrequenciasLote([
        FrequenciaPendente(
          leituraId: '00000000-0000-4000-8000-000000000001',
          idPessoa: 10,
          participanteId: '00000000-0000-4000-8000-000000000002',
          participanteNome: 'Maria Teste',
          idEvento: 1,
          tipoAcao: TipoAcao.entrada,
          dataHoraLida: DateTime.utc(2026, 11, 25, 18, 0, 0),
          dispositivoId: 'device-1',
        ),
      ]);

      expect(refreshCalled, 1);
      expect(authHeaders, ['Bearer expired-token', 'Bearer refreshed-token']);
      expect(response.confirmados, ['00000000-0000-4000-8000-000000000001']);
    });
  });
}

final class _FakeLocalParticipantRepository implements LocalParticipantRepository {
  _FakeLocalParticipantRepository(this._session);

  PortariaSession? _session;

  @override
  Future<PortariaSession?> loadSession() async => _session;

  @override
  Future<void> saveSession(PortariaSession session) async {
    _session = session;
  }

  @override
  dynamic noSuchMethod(Invocation invocation) => super.noSuchMethod(invocation);
}

final class _FakeBoralaApiClient implements BoralaApiClient {
  int refreshCalls = 0;

  @override
  Future<BoralaSessaoToken> renovarSessao({
    required String baseUrl,
    required String refreshToken,
  }) async {
    refreshCalls++;
    await Future<void>.delayed(const Duration(milliseconds: 10));
    return const BoralaSessaoToken(
      accessToken: 'new-access',
      refreshToken: 'new-refresh',
      expiresIn: 28800,
    );
  }

  @override
  dynamic noSuchMethod(Invocation invocation) => super.noSuchMethod(invocation);
}

final class _CallbackAdapter implements HttpClientAdapter {
  _CallbackAdapter(this._handler);

  final Future<ResponseBody> Function(RequestOptions options) _handler;

  @override
  Future<ResponseBody> fetch(
    RequestOptions options,
    Stream<Uint8List>? requestStream,
    Future<void>? cancelFuture,
  ) => _handler(options);

  @override
  void close({bool force = false}) {}
}
