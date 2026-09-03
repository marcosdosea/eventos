#!/bin/zsh

set -euo pipefail

diretorio_script="${0:A:h}"
diretorio_frontend="${diretorio_script:h}"
configuracao_pacotes="$diretorio_frontend/.dart_tool/package_config.json"
dart_comando="${commands[dart]:-}"
api_base_url="http://localhost:8080"

for argumento in "$@"; do
  if [[ "$argumento" == --api=* ]]; then
    api_base_url="${argumento#--api=}"
  fi
done

if [[ ! -f "$configuracao_pacotes" ]]; then
  echo "Dependências ausentes. Execute 'flutter pub get' dentro de frontend/." >&2
  exit 1
fi

if [[ -z "$dart_comando" ]]; then
  echo "Dart não encontrado no PATH." >&2
  exit 1
fi

echo "Verificando API em $api_base_url..."
if ! curl --fail --silent --show-error --max-time 8 \
  "$api_base_url/api/participantes" >/dev/null; then
  echo "A API não está acessível. Inicie o backend e tente novamente." >&2
  exit 1
fi

dart_sdk_direto="${dart_comando:h}/cache/dart-sdk/bin/dart"
if [[ -x "$dart_sdk_direto" ]]; then
  dart_comando="$dart_sdk_direto"
fi

cd "$diretorio_frontend"

exec "$dart_comando" \
  --packages="$configuracao_pacotes" \
  "$diretorio_frontend/../scripts/exportar_qrcodes.dart" \
  "$@"
