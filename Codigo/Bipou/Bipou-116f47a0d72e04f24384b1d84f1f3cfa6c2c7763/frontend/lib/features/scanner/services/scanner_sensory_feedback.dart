import 'dart:async';

import 'package:flutter/services.dart';

abstract interface class ScannerSensoryFeedback {
  Future<void> playSuccess();

  Future<void> playOffline();

  Future<void> playNotEnrolled();

  Future<void> playDuplicate();

  Future<void> playError();
}

final class PlatformScannerSensoryFeedback implements ScannerSensoryFeedback {
  const PlatformScannerSensoryFeedback();

  static const MethodChannel _channel = MethodChannel(
    'com.itatechjr.bipou_frontend/scanner_feedback',
  );

  @override
  Future<void> playSuccess() => _invokeOrFallback('playSuccess', _fallbackSuccess);

  @override
  Future<void> playOffline() => _invokeOrFallback('playSuccess', _fallbackSuccess);

  @override
  Future<void> playNotEnrolled() => _invokeOrFallback('playAlert', _fallbackAlert);

  @override
  Future<void> playDuplicate() =>
      _invokeOrFallback('playDuplicate', _fallbackDuplicate);

  @override
  Future<void> playError() => _invokeOrFallback('playAlert', _fallbackAlert);

  Future<void> _invokeOrFallback(
    String method,
    Future<void> Function() fallback,
  ) async {
    try {
      await _channel.invokeMethod<void>(method);
    } on MissingPluginException {
      await fallback();
    } on PlatformException {
      await fallback();
    }
  }

  static Future<void> _fallbackSuccess() async {
    await SystemSound.play(SystemSoundType.click);
    await HapticFeedback.lightImpact();
  }

  static Future<void> _fallbackDuplicate() async {
    await SystemSound.play(SystemSoundType.click);
    await HapticFeedback.selectionClick();
  }

  static Future<void> _fallbackAlert() async {
    await SystemSound.play(SystemSoundType.alert);
    await HapticFeedback.heavyImpact();
    await Future<void>.delayed(const Duration(milliseconds: 120));
    await HapticFeedback.heavyImpact();
  }
}
