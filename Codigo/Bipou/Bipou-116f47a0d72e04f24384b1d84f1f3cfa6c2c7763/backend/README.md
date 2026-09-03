# Backend Bipou

API Spring Boot do sistema Bipou. Ela recebe os registros do aplicativo Android, valida as regras de negocio e persiste os dados no PostgreSQL.

## Requisitos

- Java 25
- Maven Wrapper incluso no projeto
- Docker e Docker Compose, para rodar API e banco de forma simples

## Variaveis de ambiente

Crie um arquivo `.env` dentro da pasta `backend/` usando o exemplo:

```bash
cp .env.example .env
```

Edite os valores conforme seu ambiente:

```properties
POSTGRES_DB=bipou
POSTGRES_USER=admin
POSTGRES_PASSWORD=troque_essa_senha
POSTGRES_HOST=localhost
POSTGRES_PORT=5432
```

O arquivo `.env` real nao deve ser commitado.

## Rodar com Docker

Dentro de `backend/`:

```bash
docker compose up --build
```

Isso sobe:

- API em `http://localhost:8080`
- PostgreSQL em `localhost:5432`

Para parar:

```bash
docker compose down
```

Para apagar tambem os dados locais do banco:

```bash
docker compose down -v
```

## Rodar localmente com Maven

Suba um PostgreSQL antes, com as mesmas variaveis do `.env`.

Depois execute:

```bash
./mvnw spring-boot:run
```

## Testes

```bash
./mvnw test
```

Os testes de integracao usam PostgreSQL e Flyway. Garanta que o banco esteja acessivel com as credenciais configuradas.

## Endpoints principais

- `POST /api/participantes`: cadastra participante.
- `POST /api/participantes/lote`: cadastra até 5.000 participantes em uma única transação.
- `POST /api/participantes/consultar-por-cpf`: localiza um participante pelo CPF sem expor o documento na URL.
- `POST /api/registros`: registra leitura de credenciamento.

Exemplo de cadastro:

```bash
curl -X POST http://localhost:8080/api/participantes \
  -H "Content-Type: application/json" \
  -d '{"nome":"Maria Silva","cpf":"52998224725","cadastroId":"550e8400-e29b-41d4-a716-446655440000"}'
```

Nome e CPF são obrigatórios. O `cadastroId` é opcional para clientes comuns, mas o aplicativo o envia para que um cadastro pendente possa ser repetido com segurança após uma falha de rede. Reenviar o mesmo `cadastroId`, nome e CPF devolve o participante já criado; reutilizá-lo com outros dados retorna conflito. A resposta contem o UUID gerado para o participante; esse e o identificador usado no QR Code e nos registros de acesso.

O cadastro em lote recebe um objeto com uma lista de participantes:

```json
{
  "participantes": [
    {"nome": "Maria Silva", "cpf": "52998224725"},
    {"nome": "Joao Santos", "cpf": "11144477735"}
  ]
}
```

Se algum CPF estiver repetido no lote ou já existir no banco, toda a transação é cancelada.

Consulta usada pela entrada manual por CPF:

```bash
curl -X POST http://localhost:8080/api/participantes/consultar-por-cpf \
  -H "Content-Type: application/json" \
  -d '{"cpf":"52998224725"}'
```

O backend devolve o UUID, nome e CPF do participante. O aplicativo usa o UUID retornado para registrar a entrada ou saída pelo mesmo fluxo utilizado pelo QR Code.

Exemplo de registro:

```bash
curl -X POST http://localhost:8080/api/registros \
  -H "Content-Type: application/json" \
  -d '{"leituraId":"550e8400-e29b-41d4-a716-446655440000","participanteId":"6ba7b810-9dad-41d1-80b4-00c04fd430c8","tipoAcao":"ENTRADA","dispositivoId":"android-portaria-1","dataHoraLidaNoCelular":"2026-08-22T17:00:00-03:00"}'
```

Os unicos tipos de acao aceitos sao `ENTRADA` e `SAIDA`. O fluxo valido e primeira entrada seguida da saida.

## Acesso via Tailscale

Na maquina que roda o backend, execute:

```bash
tailscale ip -4
```

Use o IP retornado no app Flutter, por exemplo:

```text
http://100.x.y.z:8080
```

Nao e necessario informar node ID ou chave do Tailscale no app. O celular precisa estar conectado a mesma tailnet e ter acesso liberado a porta `8080`.
