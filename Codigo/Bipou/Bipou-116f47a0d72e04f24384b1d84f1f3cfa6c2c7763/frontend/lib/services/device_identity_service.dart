import 'dart:io';

import 'package:bipou_frontend/services/platform/audit_directory_provider.dart';
import 'package:uuid/uuid.dart';

typedef DeviceDirectoryProvider = Future<Directory> Function();
typedef DeviceUuidGenerator = String Function();

final class DeviceIdentityService {
  DeviceIdentityService({
    DeviceDirectoryProvider? directoryProvider,
    DeviceUuidGenerator? uuidGenerator,
  }) : _directoryProvider =
           directoryProvider ?? defaultDocumentsDirectoryProvider,
       _uuidGenerator = uuidGenerator ?? const Uuid().v4;

  static const String fileName = 'dispositivo_id.txt';

  final DeviceDirectoryProvider _directoryProvider;
  final DeviceUuidGenerator _uuidGenerator;
  Future<String>? _identity;

  Future<String> getOrCreate() {
    return _identity ??= _loadOrCreate();
  }

  Future<String> _loadOrCreate() async {
    try {
      final directory = await _directoryProvider();
      final file = File('${directory.path}${Platform.pathSeparator}$fileName');

      if (await file.exists()) {
        final stored = (await file.readAsString()).trim();
        if (!_isValid(stored)) {
          throw const DeviceIdentityException(
            'O identificador persistido do dispositivo e invalido.',
          );
        }
        return stored;
      }

      final identity = 'bipou-${_uuidGenerator()}';
      if (!_isValid(identity)) {
        throw const DeviceIdentityException(
          'Nao foi possivel gerar um identificador valido.',
        );
      }

      await file.writeAsString(identity, mode: FileMode.write, flush: true);
      return identity;
    } on DeviceIdentityException {
      rethrow;
    } on Object catch (error, stackTrace) {
      Error.throwWithStackTrace(
        DeviceIdentityException(
          'Nao foi possivel acessar a identidade do dispositivo.',
          error,
        ),
        stackTrace,
      );
    }
  }

  bool _isValid(String value) {
    return RegExp(
      r'^bipou-[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[1-8][0-9a-fA-F]{3}-[89abAB][0-9a-fA-F]{3}-[0-9a-fA-F]{12}$',
    ).hasMatch(value);
  }
}

final class DeviceIdentityException implements Exception {
  const DeviceIdentityException(this.message, [this.cause]);

  final String message;
  final Object? cause;

  @override
  String toString() => 'DeviceIdentityException: $message';
}
