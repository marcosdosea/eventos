import 'dart:io';

import 'package:bipou_frontend/services/device_identity_service.dart';
import 'package:flutter_test/flutter_test.dart';

void main() {
  test('cria e reutiliza a mesma identidade persistida', () async {
    final directory = await Directory.systemTemp.createTemp('bipou_device_');
    addTearDown(() => directory.delete(recursive: true));
    var generations = 0;

    final firstService = DeviceIdentityService(
      directoryProvider: () async => directory,
      uuidGenerator: () {
        generations++;
        return '550e8400-e29b-41d4-a716-446655440000';
      },
    );
    final firstIdentity = await firstService.getOrCreate();

    final secondService = DeviceIdentityService(
      directoryProvider: () async => directory,
      uuidGenerator: () => '6ba7b810-9dad-41d1-80b4-00c04fd430c8',
    );
    final secondIdentity = await secondService.getOrCreate();

    expect(firstIdentity, 'bipou-550e8400-e29b-41d4-a716-446655440000');
    expect(secondIdentity, firstIdentity);
    expect(generations, 1);
  });

  test('rejeita identidade persistida corrompida', () async {
    final directory = await Directory.systemTemp.createTemp('bipou_device_');
    addTearDown(() => directory.delete(recursive: true));
    final file = File(
      '${directory.path}${Platform.pathSeparator}${DeviceIdentityService.fileName}',
    );
    await file.writeAsString('identidade-invalida');

    final service = DeviceIdentityService(
      directoryProvider: () async => directory,
    );

    await expectLater(
      service.getOrCreate(),
      throwsA(isA<DeviceIdentityException>()),
    );
  });
}
