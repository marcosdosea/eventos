import 'package:bipou_frontend/models/frequencia_pendente.dart';

abstract interface class FrequenciaPendenteStore {
  Future<void> salvarTransacional(FrequenciaPendente frequencia);

  Future<List<FrequenciaPendente>> listarParaEnvio({int limit = 50});

  Future<List<FrequenciaPendente>> listarTodas();

  Future<FrequenciaPendente?> buscarPorLeituraId(String leituraId);

  Future<int> contarPendentes();

  Future<void> marcarEnviadosEmLote(List<String> leituraIds);

  Future<void> marcarErroEmLote(Map<String, String> errosPorLeituraId);

  Future<void> registrarTentativaFalhaRede(List<String> leituraIds);

  Future<void> close();
}

final class InMemoryFrequenciaPendenteStore implements FrequenciaPendenteStore {
  final Map<String, FrequenciaPendente> _items = <String, FrequenciaPendente>{};

  @override
  Future<void> salvarTransacional(FrequenciaPendente frequencia) async {
    _items.putIfAbsent(frequencia.leituraId, () => frequencia);
  }

  @override
  Future<List<FrequenciaPendente>> listarParaEnvio({int limit = 50}) async {
    if (limit <= 0) {
      return const <FrequenciaPendente>[];
    }

    final pendentes =
        _items.values
            .where((item) => item.statusEnvio == StatusEnvioFrequencia.pendente)
            .toList()
          ..sort((a, b) {
            final cmp = a.dataHoraLida.compareTo(b.dataHoraLida);
            return cmp != 0 ? cmp : a.leituraId.compareTo(b.leituraId);
          });

    return pendentes.take(limit).toList(growable: false);
  }

  @override
  Future<List<FrequenciaPendente>> listarTodas() async {
    final todos = _items.values.toList()
      ..sort((a, b) {
        final cmp = a.dataHoraLida.compareTo(b.dataHoraLida);
        return cmp != 0 ? cmp : a.leituraId.compareTo(b.leituraId);
      });
    return List<FrequenciaPendente>.unmodifiable(todos);
  }

  @override
  Future<FrequenciaPendente?> buscarPorLeituraId(String leituraId) async {
    return _items[leituraId];
  }

  @override
  Future<int> contarPendentes() async {
    return _items.values
        .where((item) => item.statusEnvio == StatusEnvioFrequencia.pendente)
        .length;
  }

  @override
  Future<void> marcarEnviadosEmLote(List<String> leituraIds) async {
    for (final leituraId in leituraIds) {
      final item = _items[leituraId];
      if (item != null) {
        _items[leituraId] = item.copyWith(
          statusEnvio: StatusEnvioFrequencia.enviado,
          tentativasEnvio: item.tentativasEnvio + 1,
          limparUltimoErro: true,
        );
      }
    }
  }

  @override
  Future<void> marcarErroEmLote(Map<String, String> errosPorLeituraId) async {
    for (final entry in errosPorLeituraId.entries) {
      final item = _items[entry.key];
      if (item != null) {
        _items[entry.key] = item.copyWith(
          statusEnvio: StatusEnvioFrequencia.erro,
          tentativasEnvio: item.tentativasEnvio + 1,
          ultimoErro: entry.value,
        );
      }
    }
  }

  @override
  Future<void> registrarTentativaFalhaRede(List<String> leituraIds) async {
    for (final leituraId in leituraIds) {
      final item = _items[leituraId];
      if (item != null && item.statusEnvio != StatusEnvioFrequencia.enviado) {
        _items[leituraId] = item.copyWith(
          statusEnvio: StatusEnvioFrequencia.pendente,
          tentativasEnvio: item.tentativasEnvio + 1,
        );
      }
    }
  }

  @override
  Future<void> close() async {}
}

final class FrequenciaPendenteStoreException implements Exception {
  const FrequenciaPendenteStoreException(this.message, [this.cause]);

  final String message;
  final Object? cause;

  @override
  String toString() => 'FrequenciaPendenteStoreException: $message';
}
