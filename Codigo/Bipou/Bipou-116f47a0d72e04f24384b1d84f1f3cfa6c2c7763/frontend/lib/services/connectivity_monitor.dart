import 'package:bipou_frontend/services/connectivity_monitor_contract.dart';
import 'package:connectivity_plus/connectivity_plus.dart';

export 'package:bipou_frontend/services/connectivity_monitor_contract.dart';

final class ConnectivityPlusMonitor implements ConnectivityMonitor {
  ConnectivityPlusMonitor({Connectivity? connectivity})
    : _connectivity = connectivity ?? Connectivity();

  final Connectivity _connectivity;

  @override
  Future<bool> get isConnected async {
    final results = await _connectivity.checkConnectivity();
    return hasActiveConnection(results);
  }

  @override
  Stream<bool> get onConnectivityChanged => _connectivity.onConnectivityChanged
      .map(hasActiveConnection)
      .distinct();

  static bool hasActiveConnection(List<ConnectivityResult> results) {
    return results.any(
      (result) =>
          result == ConnectivityResult.wifi ||
          result == ConnectivityResult.mobile ||
          result == ConnectivityResult.ethernet ||
          result == ConnectivityResult.vpn,
    );
  }
}
