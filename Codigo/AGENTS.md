# C# / ASP.NET Core 8 (Codigo/Evento)

Este documento é o guia operacional e arquitetural do **Boralá** para agentes de IA que atuam neste repositório. O estilo e a profundidade seguem o padrão operacional do Codex (`AGENTS.md`).

---

## 1. Regras Fundamentais e Fronteira de Escopo

Na pasta `Codigo/Evento` onde reside a solução ativa (`Evento.sln`):

- **Fronteira Estrita do Repositório**:
  - Todo o código-fonte ativo reside exclusivamente em `Codigo/Evento/`.
  - A documentação e modelagem oficiais residem em `AnaliseProjeto/`, `Requisitos/`, `ProcessosNegocio/` e `Gerenciamento/`.
  - O diretório `Codigo/Bipou/` pertence a outro projeto paralelo/legado: **NUNCA leia, referencie ou altere arquivos dentro de `Codigo/Bipou/`**.
- **Idioma Obrigatório**: Todos os textos voltados ao usuário (labels, mensagens de erro, alertas de validação, títulos de página e placeholders) devem estar obrigatoriamente em **Português (pt-BR)**.
- **Normalização de CPF**: Sempre limpe máscaras e pontuações com `Util.Methods.RemoveNaoNumericos(cpf)` antes de persistir ou consultar em qualquer camada. CPFs no sistema são estritamente 11 dígitos numéricos (`"12345678901"`).
- **Tratamento de Strings e Nullability**:
  - Habilite e respeite os avisos de tipos de referência anuláveis (`#nullable enable`).
  - Use interpolação de strings `$"{var}"` em vez de concatenação com `+`.
  - Prefira `string.IsNullOrWhiteSpace(str)` para checagens de campos de texto opcionais.
- **Degradação Graciosa de Ambiente (`.env`)**:
  - Variáveis SMTP (`EMAIL_SMTP`, `EMAIL_PORT`, `EMAIL_USER`, `EMAIL_PASS`) carregadas via `DotNetEnv.Env.Load()` podem ser nulas em ambientes locais de desenvolvimento/revisão.
  - Implementações de `IEmailSender` / `IEmailService` **NUNCA** devem lançar exceções durante o construtor ou durante injeção de dependência na inicialização de rotas (ex.: `/Identity/Account/Register`). Se as variáveis não existirem, registre `_logger.LogWarning(...)` e prossiga com segurança.
- **Processos em Execução no Windows**: Ao iniciar comandos de servidor (`dotnet run`) ou testes, aguarde a finalização ou utilize tarefas em segundo plano apropriadas. Nunca tente encerrar processos .NET abruptamente de forma que bloqueie executáveis em `bin/Debug/net8.0/` (ex.: erro MSB3027 / travamento de `EventoWeb.exe`).

---

## 2. Camadas e Organização da Solução (`Evento.sln`)

A solução adota uma arquitetura em camadas N-Tier com separação estrita de responsabilidades:

```text
EventoWeb (MVC + Razor Identity + ViewModels + Mappers)
   ├──► Service (Lógica de negócio e transações EF Core)
   │       ├──► Core (Entidades, DbContexts, DTOs, Contratos I*Service)
   │       └──► Util (Validações customizadas e helpers)
   ├──► Core
   └──► Util
```

### Responsabilidades por Projeto:

1. **`Core` (`Codigo/Evento/Core`)**:
   - Contém as entidades de domínio mapeadas do MySQL (`Evento`, `Subevento`, `Pessoa`, `Papel`, `Participacaopessoaevento`, `Inscricaopessoaevento`, `Tipoinscricao`, etc.).
   - Contém os dois contextos: `EventoContext` e `IdentityContext`.
   - Contém os contratos das interfaces de serviço (`IEventoService`, `IPessoaService`, `IInscricaoService`, etc.) em `Core/Service/`.
   - Contém Data Transfer Objects em `Core/DTO/` (`EventoDTO`, `PessoaDTO`, `ColaboradorDTO`, etc.).
   - Contém exceções de negócio em `Core/ServiceException.cs`.
2. **`Service` (`Codigo/Evento/Service`)**:
   - Implementa a lógica de negócio e consultas ao banco em `*Service.cs` utilizando `EventoContext`.
   - Lança `ServiceException` quando uma regra de negócio ou integridade for violada (ex.: validação de datas, dependências ativas ao excluir evento, duplicidade de inscrição).
   - **Regra de Isolamento**: O projeto `Service` **JAMAIS** deve referenciar tipos da camada Web (`EventoWeb`, `Microsoft.AspNetCore.Mvc`, ViewModels, etc.).
3. **`Util` (`Codigo/Evento/Util`)**:
   - Helper compartilhado com atributos customizados de validação (`CpfAttribute`, `TelefoneAttribute`, `CepAttribute`) e utilitários (`Methods.RemoveNaoNumericos`, formatações).
4. **`EventoWeb` (`Codigo/Evento/EventoWeb`)**:
   - Camada de apresentação contendo `Controllers/`, `Models/` (*ViewModels*), `Mappers/` (*AutoMapper Profiles*), `Views/`, `Areas/Identity/` e `Filters/` (`NomeUsuarioFilter`).
   - **Regra de Ouro**: **É ESTRITAMENTE PROIBIDO injetar `EventoContext` ou `IdentityContext` diretamente em Controllers ou Views**. Toda comunicação com dados deve passar pelas interfaces `I*Service` ou pelos serviços do Identity (`UserManager`, `SignInManager`).
5. **`ServiceTests` e `EventoWebTests`**:
   - Testes unitários com `MSTest`, `Moq` e `EF Core InMemory / SQLite`.

---

## 3. Banco de Dados Duplo (`Dual DbContext`) e Autenticação

O sistema opera com **dois esquemas de banco de dados independentes** (configurados no `Program.cs`):

1. **`EventoContext` (`ConnectionStrings:EventoDatabase` $\rightarrow$ schema `evento`)**:
   - Tabelas de negócio: `evento`, `subevento`, `pessoa`, `papel`, `participacaopessoaevento`, `participacaopessoasubevento`, `inscricaopessoaevento`, `inscricaopessoasubevento`, `tipoinscricao`, `modelocracha`, `modelocertificado`, `areainteresse`, `estadosbrasil`.
   - Scripts oficiais: `AnaliseProjeto/evento-database-create.sql` e `AnaliseProjeto/dump-evento-database.sql`.
2. **`IdentityContext` (`ConnectionStrings:ItatechUsersDatabase` $\rightarrow$ schema `itatechusers`)**:
   - Tabelas do ASP.NET Core Identity: `AspNetUsers`, `AspNetRoles`, `AspNetUserRoles`, `AspNetUserClaims`, etc., com a entidade `UsuarioIdentity`.
   - Script oficial: `AnaliseProjeto/itatechusers-database-create.sql`.

### Chave Lógica de Integração entre os Bancos
- O campo **`UsuarioIdentity.UserName`** armazena **obrigatoriamente o CPF sem máscara (11 dígitos)**.
- O usuário autenticado é identificado por `User.Identity.Name` (que retorna o CPF numérico).
- Para obter a entidade de negócio `Pessoa`, injeta-se `IPessoaService` e chama-se `_pessoaService.GetByCpf(User.Identity.Name)`.
- Ao cadastrar novo usuário, deve-se criar atomicamente o registro no `IdentityContext` (`_userManager.CreateAsync`) e a entidade correspondente em `pessoa` (`_pessoaService.Create`).

### Diferença Crítica de Domínio: Inscrição vs. Participação
- **`InscricaoPessoaEvento` / `InscricaoPessoaSubEvento`**: Representa a **matrícula / inscrição** (`status`: `'A'` - Ativa, `'C'` - Cancelada, `'S'` - Solicitada, `valorTotal`, `frequenciaFinal`, `nomeCracha`).
- **`ParticipacaoPessoaEvento` / `ParticipacaoPessoaSubEvento`**: Representa o **registro físico de presença / credenciamento** (`entrada` DATETIME, `saida` DATETIME NULL).
- Nunca confunda "inscrição" com "participação" ao nomear métodos ou implementar serviços.

### Usuários de Teste Disponíveis no Dump Oficial (`dump-evento-database.sql`):
Para testes locais e validações de rotas com autenticação, utilize os logins pré-cadastrados (todos com senha padrão `123456`):
- **Administrador**: Dosea (`Login = 65535216038`), Icaro (`Login = 27382886000`)
- **Gestor**: Sodre (`Login = 92023338077`)
- **Colaborador**: Valdir (`Login = 63499583062`)
- **Usuário Normal**: Jordan (`Login = 40201971054`)

---

## 4. Matriz de Papéis, Autorização e Governança de Eventos

A modelagem oficial de atores e casos de uso (`AnaliseProjeto/eventos.mdj`) define regras estritas de escopo:

### Papéis do Identity (`AspNetRoles`) vs. Tabela `papel` (`EventoContext`)

| Papel no Identity | `papel.Id` | Layout Automático (`_ViewStart.cshtml`) | Escopo de Atuação e Limites |
| :--- | :---: | :--- | :--- |
| `ADMINISTRADOR` | `1` | `_LayoutAdministrativo.cshtml` | **Governança Global (`UC01`–`UC04`)**: cria o registro do Evento, associa/remove o Gestor, cadastra Áreas de Interesse e Administradores. **NÃO herda** de Gestor ou Colaborador na gestão interna do evento. |
| `GESTOR` | `2` | `_LayoutGestor.cshtml` | **Herda `COLABORADOR` e `USUARIO`**: configura detalhes do evento, lotes/tipos de inscrição, subeventos, modelos de crachá/certificado e gerencia a equipe de colaboradores e participantes. |
| `COLABORADOR` | `3` | `_LayoutColaborador.cshtml` | **Herda `USUARIO`**: credenciamento, consulta de inscritos e controle de presença/frequência (`entrada`/`saida`) de participantes no evento/subevento. |
| `USUARIO` | `4` | `_LayoutParticipante.cshtml` | Participante: inscrição em eventos e subeventos, consulta de inscrições ativas e emissão de certificados. |
| *(Anônimo)* | — | `_Layout.cshtml` | Vitrine pública (`HomeController`), listagem de eventos com filtro por localização e autenticação/cadastro. |

### Invariantes de Autorização:
1. **Isolamento de Gestão por Evento**:
   - A role `GESTOR` ou `COLABORADOR` no Identity habilita o layout, mas o vínculo com um evento específico é verificado via `participacaopessoaevento` (`IdPessoa`, `IdEvento`, `IdPapel`: `2` = Gestor, `3` = Colaborador, `4` = Participante).
2. **Independência de Frequência (Evento Principal vs. Subevento)**:
   - A presença no evento principal (`ParticipacaoPessoaEventoService`) é dissociada da presença nos subeventos (`ParticipacaoPessoaSubEventoService`).
3. **Seleção Automática de Layout**:
   - O arquivo `Codigo/Evento/EventoWeb/Views/_ViewStart.cshtml` seleciona o layout de acordo com `User.IsInRole(...)` na ordem: `ADMINISTRADOR` $\rightarrow$ `GESTOR` $\rightarrow$ `COLABORADOR` $\rightarrow$ `USUARIO` $\rightarrow$ `_Layout`. Não force `Layout = ...` manualmente nas Views, a menos que a tela exija layout público fixo.

---

## 5. Regras de Code Review e Padrões de Código

### Superfície de API e Contratos (`Core`)
- Ao adicionar novas operações de negócio, declare o método na interface `I*Service.cs` dentro de `Core/Service/` antes de implementar em `Service/`.
- Qualquer DTO novo deve residir em `Core/DTO/` e ser mapeado via `AutoMapper` nos perfis correspondentes em `EventoWeb/Mappers/` (ex.: `EventoProfile.cs`, `PessoaProfile.cs`).

### Resistência a Bloat nos Controllers (`EventoWeb`)
- Mantenha os Controllers enxutos: eles devem atuar como orquestradores (validação de `ModelState`, chamada ao serviço, mapeamento via `IMapper` e retorno de `View` ou `RedirectToAction`).
- Regras de negócio complexas, cálculos de frequência ou validações de dependências pertencem a `Service/`, nunca à camada Web.

### Checklist de Breaking Changes
Ao propor ou revisar PRs, verifique:
1. Mudanças em assinaturas de métodos nas interfaces `I*Service` (requer atualizar o serviço e os testes correspondentes).
2. Alterações em ViewModels ou entidades que afetem o mapeamento do `AutoMapper`.
3. Adição de rotas ou actions em Controllers sem a anotação `[Authorize(Roles = "...")]` correta.
4. Qualquer alteração em tabelas MySQL que demande ajuste manual nos scripts `.sql` de `AnaliseProjeto/`.

---

## 6. Diretrizes para Autoria de Testes (MSTest + Moq)

O repositório possui duas suítes principais de testes: `ServiceTests` e `EventoWebTests`.

### Testes da Camada Service (`ServiceTests`)
- **Padrão**: Usar banco de dados em memória (`Microsoft.EntityFrameworkCore.InMemory`) no método `[TestInitialize]`:
  ```csharp
  var builder = new DbContextOptionsBuilder<EventoContext>();
  builder.UseInMemoryDatabase("NomeBancoTeste");
  _context = new EventoContext(builder.Options);
  _context.Database.EnsureDeleted();
  _context.Database.EnsureCreated();
  ```
- **Foco dos Testes**: Criar, buscar, atualizar, deletar, validações de regra de negócio e checagem de lançamento de `ServiceException`.

### Testes da Camada Web / Controllers (`EventoWebTests`)
- **Padrão**: Simular dependências externas (`I*Service`, `UserManager<UsuarioIdentity>`) com **`Moq`** e instanciar um `IMapper` real utilizando os perfis do projeto (`MapperConfiguration`):
  ```csharp
  var mockService = new Mock<IEventoService>();
  IMapper mapper = new MapperConfiguration(cfg => cfg.AddProfile(new EventoProfile())).CreateMapper();
  var controller = new EventoController(..., mockService.Object, mapper, ...);
  ```
- **Contexto do Usuário nos Testes**: Ao testar actions que utilizam o usuário logado (`User.Identity.Name` ou claims de roles), injete um `ClaimsPrincipal` no `ControllerContext`:
  ```csharp
  var user = new ClaimsPrincipal(new ClaimsIdentity(new[]
  {
      new Claim(ClaimTypes.Name, "12345678901"),
      new Claim(ClaimTypes.Role, "GESTOR")
  }, "mock"));
  controller.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext { User = user } };
  ```
- **Asserts**: Verifique o tipo do retorno (`ViewResult`, `RedirectToActionResult`), passagem correta do ViewModel para a View e erros em `ModelState`.

---

## 7. Comandos de Desenvolvimento e Testes

Execute todos os comandos a partir da raiz do repositório (`c:\Users\Dede\Desktop\Boralá`):

### Compilação e Verificação Geral
```powershell
# Compilar toda a solução (0 erros esperados)
dotnet build Codigo\Evento\Evento.sln --nologo --verbosity minimal

# Executar todos os testes unitários da solução
dotnet test Codigo\Evento\Evento.sln --nologo --verbosity minimal
```

### Execução de Testes com Escopo Reduzido (Recomendado para desenvolvimento rápido)
```powershell
# Executar apenas testes de um Controller específico
dotnet test Codigo\Evento\EventoWebTests\EventoWebTests.csproj --filter "FullyQualifiedName~EventoControllerTests"

# Executar apenas testes de um Service específico
dotnet test Codigo\Evento\ServiceTests\ServiceTests.csproj --filter "FullyQualifiedName~EventoServiceTests"
```

### Execução da Aplicação Localmente
```powershell
# Iniciar o servidor de desenvolvimento
dotnet run --project Codigo\Evento\EventoWeb\EventoWeb.csproj --urls http://localhost:5198
```

---

## 8. Diretrizes Operacionais (Always / Ask First / Never)

### Always (Sempre)
- **Sempre** rode `dotnet test Codigo\Evento\Evento.sln` antes de concluir uma tarefa ou enviar uma PR.
- **Sempre** mantenha todas as mensagens visíveis, validações e mensagens de erro em **Português (pt-BR)**.
- **Sempre** limpe e normalize CPFs (`Util.Methods.RemoveNaoNumericos`) antes de qualquer consulta ou persistência.
- **Sempre** certifique-se de que qualquer código dependente de `IEmailSender` funciona normalmente sem variáveis de ambiente SMTP.

### Ask First (Pergunte Antes)
- **Pergunte antes** de criar novas migrações ou alterar scripts SQL de `AnaliseProjeto/`.
- **Pergunte antes** de adicionar novos pacotes NuGet aos arquivos `.csproj`.
- **Pergunte antes** de alterar a hierarquia de layouts em `_ViewStart.cshtml` ou papéis em `IdentityInitializer.cs`.

### Never (Nunca)
- **Nunca** modifique, leia ou faça referência a qualquer arquivo em `Codigo/Bipou/`.
- **Nunca** injete `EventoContext` ou `IdentityContext` diretamente em Controllers ou Views.
- **Nunca** crie métodos auxiliares pequenos e de uso único em Controllers se eles representam lógica de negócio do domínio.
- **Nunca** remova ou ignore testes existentes sem justificativa explícita e aprovação.
