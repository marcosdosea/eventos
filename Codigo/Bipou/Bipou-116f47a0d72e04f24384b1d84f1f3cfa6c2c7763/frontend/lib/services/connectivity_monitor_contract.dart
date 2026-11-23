abstract interface class ConnectivityMonitor {
  Future<bool> get isConnected;

  Stream<bool> get onConnectivityChanged;
}
