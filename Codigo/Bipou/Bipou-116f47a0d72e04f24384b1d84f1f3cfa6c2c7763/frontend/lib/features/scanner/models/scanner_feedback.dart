enum ScannerFeedbackType { sucesso, offline, naoInscrito, duplicado, erro }

final class ScannerFeedback {
  const ScannerFeedback({
    required this.message,
    required this.type,
    this.participantName,
    this.statusLabel,
    this.detail,
    this.processingDuration,
  });

  final String message;
  final ScannerFeedbackType type;
  final String? participantName;
  final String? statusLabel;
  final String? detail;
  final Duration? processingDuration;
}
