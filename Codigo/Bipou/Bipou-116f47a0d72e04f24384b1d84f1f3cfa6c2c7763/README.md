# Bipou

Bipou e um sistema simples para credenciamento de evento universitario.

A ideia principal e acelerar a portaria sem perder rastreabilidade: o app Android le QR Codes dos participantes, registra entrada e saida, e envia os dados para um backend Spring Boot na rede local/Tailscale.

Mesmo que a rede caia, o app continua operando porque cada leitura e gravada primeiro em um arquivo local de auditoria. O backend fica responsavel por validar participantes, aplicar as regras de transicao dos registros e persistir tudo no PostgreSQL.

## Estrutura

- `backend/`: API Spring Boot, PostgreSQL, Flyway e Docker Compose.
- `frontend/`: aplicativo Flutter Android para leitura continua de QR Code e cadastro manual.

## Fluxo esperado no evento

1. O operador abre o app no Android.
2. Escolhe o modo: entrada ou saida.
3. O app registra a auditoria local antes de chamar a API.
4. A API valida e salva no banco.
5. Em falha de rede, a portaria continua andando e o log local fica como fonte de recuperacao.

## Documentacao por modulo

- Backend: veja [backend/README.md](backend/README.md).
- Frontend: veja [frontend/README.md](frontend/README.md).
- Importação por CSV e impressão de QR Codes: veja [frontend/tool/README.md](frontend/tool/README.md).
