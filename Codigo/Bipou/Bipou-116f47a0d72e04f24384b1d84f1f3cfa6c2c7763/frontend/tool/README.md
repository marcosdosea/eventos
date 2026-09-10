# Operações em lote

Execute os comandos desta página dentro da pasta `frontend/`, com o backend já iniciado.

## Importar participantes

Preencha um CSV UTF-8 com as colunas `cpf`, `nome` e `sobrenome`. Durante a importação, `nome` e `sobrenome` são unidos e enviados ao backend como o nome completo. O separador pode ser ponto e vírgula ou vírgula; CPF com pontuação também é aceito. Use [participantes.exemplo.csv](participantes.exemplo.csv) como modelo. O formato anterior, com apenas `nome` e `cpf`, continua aceito.

Valide o arquivo sem acessar a API:

```bash
dart run tool/importar_participantes.dart meu-arquivo.csv --validar
```

Cadastre todos os participantes em um único lote:

```bash
dart run tool/importar_participantes.dart meu-arquivo.csv \
  --api=http://localhost:8080
```

O arquivo inteiro é validado antes do envio. O backend grava o lote em uma única transação: se um cadastro falhar, nenhum participante daquele lote é salvo. Ao executar novamente o mesmo arquivo, um CPF já associado ao mesmo nome é ignorado; um CPF associado a outro nome interrompe a importação.

O limite é de 5.000 participantes por arquivo.

## Exportar QR Codes para impressão

Gere um único PDF com todos os participantes cadastrados:

```bash
./tool/exportar_qrcodes.sh \
  --api=http://localhost:8080 \
  --saida=credenciais-bipou.pdf
```

O wrapper `.sh` executa o exportador diretamente e evita a demora dos build hooks do projeto Flutter. Durante a geração, o terminal informa quantas credenciais já foram processadas.

O PDF usa papel A4, três colunas e cinco linhas, totalizando 15 credenciais por página. Os participantes são ordenados por nome. Cada QR Code contém somente o UUID e o nome exigidos pelo aplicativo; o CPF não é impresso nem incorporado ao código.

Para exportar somente participantes específicos, informe os CPFs separados por
vírgula. Pontuação no CPF é aceita:

```bash
./tool/exportar_qrcodes.sh \
  --api=http://localhost:8080 \
  --saida=credenciais-novos.pdf \
  --cpfs=529.982.247-25,111.444.777-35
```

Se algum CPF informado não estiver cadastrado, nenhum PDF será gerado. Sem a
opção `--cpfs`, o comportamento permanece o mesmo: todos os participantes são
exportados.

Para uma API acessada por Tailscale, substitua `localhost` pelo IP correspondente, por exemplo `http://100.x.y.z:8080`.
