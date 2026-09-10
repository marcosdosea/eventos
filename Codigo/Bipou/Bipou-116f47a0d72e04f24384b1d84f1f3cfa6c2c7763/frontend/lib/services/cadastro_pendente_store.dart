import 'dart:async';
import 'dart:convert';
import 'dart:io';

import 'package:bipou_frontend/models/cadastro_pendente.dart';
import 'package:bipou_frontend/services/audit_log_service.dart';
import 'package:bipou_frontend/services/platform/audit_directory_provider.dart';

abstract interface class CadastroPendenteStore {
  Future<void> salvar(CadastroPendente cadastro);

  Future<List<CadastroPendente>> listar();

  Future<void> remover(String cadastroId);
}

final class CadastroPendenteFileStore implements CadastroPendenteStore {
  CadastroPendenteFileStore({
    DocumentsDirectoryProvider? documentsDirectoryProvider,
  }) : _documentsDirectoryProvider =
           documentsDirectoryProvider ?? defaultDocumentsDirectoryProvider;

  static const String fileName = 'cadastros_pendentes.json';
  static const int fileVersion = 1;

  final DocumentsDirectoryProvider _documentsDirectoryProvider;
  Future<void> _operationQueue = Future<void>.value();

  @override
  Future<void> salvar(CadastroPendente cadastro) {
    return _exclusive(() async {
      final cadastros = await _readAll();
      final indice = cadastros.indexWhere(
        (item) => item.cadastroId == cadastro.cadastroId,
      );
      if (indice >= 0) {
        if (!_mesmosDados(cadastros[indice], cadastro)) {
          throw const CadastroPendenteStoreException(
            'O identificador do cadastro pendente já possui outros dados.',
          );
        }
        return;
      }

      await _writeAll(<CadastroPendente>[...cadastros, cadastro]);
    });
  }

  @override
  Future<List<CadastroPendente>> listar() {
    return _exclusive(
      () async => List<CadastroPendente>.unmodifiable(await _readAll()),
    );
  }

  @override
  Future<void> remover(String cadastroId) {
    return _exclusive(() async {
      final cadastros = await _readAll();
      final restantes = cadastros
          .where((item) => item.cadastroId != cadastroId)
          .toList();
      if (restantes.length == cadastros.length) {
        return;
      }
      await _writeAll(restantes);
    });
  }

  Future<List<CadastroPendente>> _readAll() async {
    try {
      final file = await _queueFile();
      await _recoverTemporaryFile(file);
      if (!await file.exists()) {
        return <CadastroPendente>[];
      }

      final content = await file.readAsString();
      if (content.trim().isEmpty) {
        throw const FormatException('Arquivo de cadastros pendentes vazio.');
      }
      final decoded = jsonDecode(content);
      if (decoded is! Map<String, dynamic> ||
          decoded['version'] != fileVersion ||
          decoded['cadastros'] is! List) {
        throw const FormatException(
          'Formato do arquivo de cadastros pendentes inválido.',
        );
      }

      return (decoded['cadastros'] as List).map((item) {
        if (item is! Map) {
          throw const FormatException('Cadastro pendente inválido.');
        }
        return CadastroPendente.fromJson(Map<String, dynamic>.from(item));
      }).toList();
    } on CadastroPendenteStoreException {
      rethrow;
    } on Object catch (error, stackTrace) {
      Error.throwWithStackTrace(
        CadastroPendenteStoreException(
          'Não foi possível ler os cadastros pendentes.',
          error,
        ),
        stackTrace,
      );
    }
  }

  Future<void> _writeAll(List<CadastroPendente> cadastros) async {
    try {
      final file = await _queueFile();
      await file.parent.create(recursive: true);
      final temporary = File('${file.path}.tmp');
      final content = jsonEncode(<String, Object>{
        'version': fileVersion,
        'cadastros': cadastros.map((item) => item.toJson()).toList(),
      });
      await temporary.writeAsString(content, flush: true);
      await temporary.rename(file.path);
    } on Object catch (error, stackTrace) {
      Error.throwWithStackTrace(
        CadastroPendenteStoreException(
          'Não foi possível salvar os cadastros pendentes.',
          error,
        ),
        stackTrace,
      );
    }
  }

  Future<void> _recoverTemporaryFile(File file) async {
    final temporary = File('${file.path}.tmp');
    if (!await temporary.exists()) {
      return;
    }
    if (await file.exists()) {
      await temporary.delete();
      return;
    }
    await temporary.rename(file.path);
  }

  Future<File> _queueFile() async {
    final directory = await _documentsDirectoryProvider();
    return File('${directory.path}${Platform.pathSeparator}$fileName');
  }

  Future<T> _exclusive<T>(Future<T> Function() operation) {
    final completer = Completer<T>();
    _operationQueue = _operationQueue.then((_) async {
      try {
        completer.complete(await operation());
      } on Object catch (error, stackTrace) {
        completer.completeError(error, stackTrace);
      }
    });
    return completer.future;
  }

  bool _mesmosDados(CadastroPendente primeiro, CadastroPendente segundo) {
    return primeiro.nome.trim() == segundo.nome.trim() &&
        primeiro.cpf.trim() == segundo.cpf.trim() &&
        primeiro.dispositivoId.trim() == segundo.dispositivoId.trim();
  }
}

final class CadastroPendenteStoreException implements Exception {
  const CadastroPendenteStoreException(this.message, [this.cause]);

  final String message;
  final Object? cause;

  @override
  String toString() => 'CadastroPendenteStoreException: $message';
}
