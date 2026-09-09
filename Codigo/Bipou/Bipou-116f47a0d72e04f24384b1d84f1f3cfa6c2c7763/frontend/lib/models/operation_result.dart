enum SyncStatus { enviado, salvoOffline }

final class OperationResult<T> {
  const OperationResult._({required this.status, this.data});

  const OperationResult.enviado(T data)
    : this._(status: SyncStatus.enviado, data: data);

  const OperationResult.salvoOffline()
    : this._(status: SyncStatus.salvoOffline);

  final SyncStatus status;
  final T? data;

  bool get foiEnviado => status == SyncStatus.enviado;
}
