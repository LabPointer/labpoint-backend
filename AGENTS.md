# AGENTS.md --- Diretrizes para ASP.NET Core, Entity Framework Core e Identity

Este arquivo define os padrões obrigatórios para desenvolver e manter
aplicações C# com ASP.NET Core, Entity Framework Core, ASP.NET Core
Identity e documentação de API com OpenAPI, Scalar e/ou Swagger.

O objetivo é produzir código **seguro, coeso, testável e fácil de
manter**, respeitando a arquitetura existente e evitando complexidade
desnecessária.

------------------------------------------------------------------------

## 1. Princípios gerais

1.  Antes de implementar, entenda a estrutura da solução, os projetos,
    as dependências, os padrões existentes e o fluxo da funcionalidade.
2.  **Preserve a arquitetura existente.** Não reorganize projetos,
    namespaces ou camadas sem necessidade real e sem explicar o impacto.
3.  Prefira soluções simples, explícitas e idiomáticas em C#. Não
    introduza abstrações, interfaces, factories, mediators ou padrões
    apenas por preferência.
4.  Aplique responsabilidade única, baixo acoplamento, alta coesão e
    inversão de dependência onde isso trouxer benefício concreto.
5.  Não duplique regras de negócio, validações, consultas, mapeamentos
    ou configurações.
6.  Use nomes claros e consistentes. Evite abreviações obscuras, métodos
    extensos e classes com responsabilidades diferentes.
7.  Não esconda falhas com `catch` genérico, valores padrão silenciosos
    ou respostas de sucesso artificiais.
8.  Não altere contratos públicos, formatos de resposta, autenticação ou
    comportamento existente sem verificar os consumidores e justificar a
    mudança.
9.  Não adicione pacotes NuGet sem necessidade. Verifique primeiro se a
    plataforma ou a solução já oferece o recurso.
10. Ao terminar, execute as verificações disponíveis: build, testes,
    análise/formatadores e validações relevantes.

## 2. Descoberta antes de alterar

Antes de editar:

-   Inspecione o `.sln`/`.slnx`, os `.csproj`, `Program.cs`,
    configurações e a organização das pastas.
-   Identifique a versão do .NET e as versões efetivas dos pacotes
    NuGet. **Não atualize versões por conta própria** nem misture
    versões incompatíveis.
-   Identifique se a API utiliza Controllers, Minimal APIs ou ambos.
    Siga o padrão predominante.
-   Localize `DbContext`, entidades, configurações EF Core, migrations,
    serviços, repositórios (se existirem), Identity e políticas de
    autorização.
-   Verifique como OpenAPI, Scalar e Swagger estão registrados e
    publicados.
-   Consulte as convenções locais e os testes existentes antes de criar
    uma nova convenção.

Se a solução tiver arquitetura própria, adapte estas diretrizes a ela em
vez de impor uma estrutura nova.

## 3. Arquitetura e organização

### 3.1 Direção das dependências

Quando a solução adotar Clean Architecture ou arquitetura em camadas,
respeite esta direção:

-   **Domain:** regras e conceitos centrais do negócio. Não depende de
    ASP.NET Core, EF Core, Identity ou infraestrutura.
-   **Application:** casos de uso, contratos, validações e orquestração.
    Depende do Domain, quando houver essa camada.
-   **Infrastructure:** persistência, EF Core, Identity, serviços
    externos e implementações de contratos.
-   **Api / Presentation:** endpoints, autenticação HTTP, autorização,
    filtros, configuração e composição da aplicação.

As dependências devem apontar para dentro. Domain não deve conhecer
Application, Infrastructure ou Api. Application não deve depender de
Infrastructure ou Api. Infrastructure implementa contratos definidos nas
camadas internas.

Não crie essas quatro camadas obrigatoriamente em todo projeto. Para uma
API pequena, uma organização mais simples pode ser melhor. O critério é
manter limites claros sem criar projetos vazios ou camadas artificiais.

### 3.2 Organização por funcionalidade

Quando houver várias funcionalidades, prefira agrupar os elementos
relacionados por domínio/feature, mantendo as responsabilidades técnicas
claras. Exemplo ilustrativo:

``` text
src/
  MyApp.Api/
    Endpoints/                 # ou Controllers/
    OpenApi/
    Middleware/
    Extensions/
    Program.cs
  MyApp.Application/
    Features/
      Users/
        CreateUser/
        GetUser/
    Abstractions/
    Behaviors/                 # somente se houver necessidade real
  MyApp.Domain/
    Entities/
    ValueObjects/
    Enums/
    Exceptions/
  MyApp.Infrastructure/
    Persistence/
      AppDbContext.cs
      Configurations/
      Migrations/
    Identity/
    Services/
    DependencyInjection.cs
tests/
  MyApp.UnitTests/
  MyApp.IntegrationTests/
```

Ajuste nomes e localização ao padrão real do repositório. Não mova
arquivos só para fazer a árvore ficar idêntica ao exemplo.

### 3.3 Limites de responsabilidade

-   Endpoints/controllers recebem HTTP, validam a entrada no limite
    apropriado, chamam o caso de uso e traduzem o resultado para HTTP.
-   Application coordena casos de uso e regras de aplicação; não deve
    conhecer `HttpContext`, `IActionResult` ou tipos de resposta HTTP.
-   Domain concentra invariantes e regras essenciais do negócio.
-   Infrastructure implementa acesso a dados e integrações externas.
-   DTOs de entrada/saída da API não devem ser usados automaticamente
    como entidades do banco ou entidades de domínio.
-   Evite retornar entidades EF Core diretamente pela API.

## 4. ASP.NET Core e endpoints

-   Siga o estilo existente: Controllers ou Minimal APIs. Não misture os
    dois sem uma razão arquitetural clara.
-   Mantenha endpoints pequenos e focados. Extraia a lógica para casos
    de uso/serviços quando ela deixar de ser simples coordenação HTTP.
-   Use verbos e códigos HTTP semanticamente corretos: `200`, `201`,
    `202`, `204`, `400`, `401`, `403`, `404`, `409`, `422` quando
    apropriado e `500` apenas para falhas inesperadas.
-   Em criação, considere `CreatedAtAction`/`CreatedAtRoute` ou o
    equivalente em Minimal APIs quando houver URI para consultar o
    recurso.
-   Não retorne `200 OK` para qualquer situação. Não exponha exceções,
    stack traces, SQL, tokens ou detalhes internos ao cliente.
-   Padronize erros com `ProblemDetails`/`IProblemDetailsService` ou o
    mecanismo já adotado. Inclua detalhes de validação de forma
    consistente.
-   Use cancellation tokens em operações assíncronas de I/O e propague o
    `CancellationToken` recebido até EF Core e serviços externos.
-   Prefira APIs assíncronas para I/O. Não use `Task.Run` para tornar
    chamadas síncronas de banco ou rede aparentemente assíncronas.
-   Evite `.Result`, `.Wait()` e bloqueios de chamadas assíncronas.
-   Não use estado mutável estático para armazenar dados de requisições
    ou usuários.
-   Não injete `IServiceProvider` para resolver serviços manualmente
    quando a injeção direta for possível.

## 5. Contratos, DTOs e validação

-   Defina contratos de request/response explícitos. Não aceite
    entidades de domínio ou entidades EF diretamente como payload.
-   Separe DTOs de entrada e saída quando suas necessidades e exposição
    forem diferentes.
-   Não exponha campos internos, propriedades de navegação, hashes,
    tokens, dados de auditoria ou informações privadas.
-   Valide dados na borda da aplicação e mantenha invariantes
    importantes também protegidas no domínio.
-   Centralize regras de validação reutilizáveis; evite copiar a mesma
    validação em endpoints diferentes.
-   Use Data Annotations, validação manual ou bibliotecas já instaladas
    conforme o padrão da solução. Não adicione um framework de validação
    sem necessidade.
-   Diferencie validação de formato/entrada de regra de negócio. Ex.:
    formato de e-mail é validação de entrada; impedir uma operação
    proibida pelo estado atual é regra de negócio.
-   Não confie em validação exclusivamente no frontend.
-   Use tipos adequados: `Guid` para identificadores quando esse for o
    padrão, `DateTimeOffset` para instantes com fuso/offset e `decimal`
    para valores monetários.
-   Evite strings mágicas para estados e categorias quando enums ou
    value objects forem mais seguros e adequados.

## 6. Entity Framework Core e banco de dados

### 6.1 Modelagem e DbContext

-   Mantenha o `DbContext` e as configurações de persistência na camada
    responsável pela infraestrutura.
-   Prefira configurações explícitas com
    `IEntityTypeConfiguration<TEntity>` quando o mapeamento crescer ou
    precisar ser organizado por entidade.
-   Configure chaves, índices, relacionamentos, tamanhos, precisão,
    nulabilidade e comportamento de exclusão explicitamente quando forem
    relevantes ao domínio.
-   Use `DbSet<TEntity>` e nomes coerentes com o modelo.
-   Evite colocar regras de negócio complexas em `OnModelCreating`,
    interceptors ou entidades de persistência.
-   Evite carregar grafos inteiros sem necessidade. Projete consultas
    para DTOs com `Select`.
-   Use `AsNoTracking()` em consultas somente leitura quando apropriado.
-   Evite `Include` em cadeia quando uma projeção resolve o caso com
    menos dados.
-   Não faça consultas N+1. Verifique o SQL/forma da consulta quando o
    desempenho for relevante.
-   Não carregue todos os registros para filtrar ou paginar em memória.
    Filtre, ordene e pagine no banco.
-   Use paginação com ordenação estável; defina limites máximos para
    tamanho de página.
-   Use `ExecuteUpdateAsync`/`ExecuteDeleteAsync` apenas quando o
    comportamento de tracking, interceptors e regras de domínio
    permitir.
-   Não compartilhe uma instância de `DbContext` entre threads. Respeite
    seu lifetime scoped.

### 6.2 Persistência e transações

-   Use parâmetros e APIs LINQ do EF Core. Nunca concatene entrada do
    usuário em SQL.
-   Para SQL bruto inevitável, prefira APIs parametrizadas
    (`FromSqlInterpolated` ou parâmetros explícitos); revise
    cuidadosamente qualquer SQL dinâmico.
-   Use transações explícitas quando uma operação realmente envolver
    múltiplas etapas que precisam ser atômicas. Não envolva toda
    consulta em transação manual sem motivo.
-   Considere concorrência otimista (`rowversion`/concurrency token) em
    dados sujeitos a atualizações simultâneas.
-   Trate violações de unicidade e concorrência de maneira previsível,
    convertendo-as em erros de domínio/API apropriados.
-   Não use `SaveChanges` repetidamente sem necessidade. Agrupe
    alterações relacionadas quando isso mantiver a atomicidade.
-   Não faça chamadas externas demoradas dentro de uma transação de
    banco sem avaliar bloqueios e consistência.

### 6.3 Migrations e evolução do schema

-   Toda mudança de schema deve ser acompanhada de migration revisada.
-   Nunca edite uma migration já aplicada/compartilhada para alterar o
    histórico. Crie uma nova migration corretiva.
-   Revise o método `Up` e o `Down`, nomes de índices, constraints,
    defaults e possíveis perdas de dados.
-   Não execute `Database.EnsureCreated()` em aplicações que usam
    migrations.
-   Não aplique migrations automaticamente em produção durante o
    startup, a menos que isso seja uma decisão explícita e segura da
    arquitetura/deploy.
-   Considere gerar e revisar scripts SQL idempotentes para ambientes de
    produção.
-   Não renomeie/remova colunas ou tabelas sem considerar dados
    existentes, compatibilidade e estratégia de rollout.
-   Não inclua dados sensíveis em migrations ou seeders.

## 7. ASP.NET Core Identity, autenticação e autorização

-   Use ASP.NET Core Identity para gerenciamento de usuários, senhas,
    roles e tokens quando essa for a solução adotada.
-   Não implemente armazenamento ou comparação manual de senhas. Use os
    mecanismos de hashing e validação do Identity.
-   Nunca armazene senha em texto puro, logs, claims ou respostas da
    API.
-   Configure políticas de senha, lockout, confirmação de e-mail e
    requisitos de segurança de acordo com o risco do sistema.
-   Não reduza os requisitos padrão de segurança sem justificativa
    explícita.
-   Diferencie autenticação de autorização: estar autenticado não
    significa ter permissão para executar qualquer operação.
-   Proteja endpoints por padrão quando lidarem com dados privados. Use
    `[Authorize]`, policies ou `RequireAuthorization()` conforme o
    estilo adotado.
-   Prefira policies e requisitos de autorização para regras que não
    sejam simplesmente "está autenticado?".
-   Não confie em `UserId`, role ou permissões enviados pelo cliente.
    Obtenha a identidade a partir do principal autenticado e valide o
    acesso ao recurso.
-   Evite confiar apenas em roles para autorização granular por recurso;
    verifique propriedade e permissões no caso de uso.
-   Não retorne `IdentityUser` ou `ApplicationUser` diretamente. Use
    DTOs explícitos e seguros.
-   Não exponha `PasswordHash`, `SecurityStamp`, `ConcurrencyStamp`,
    tokens de confirmação/reset ou outros campos internos.
-   Trate os resultados de `UserManager` e `SignInManager`, incluindo
    erros de validação e bloqueio, sem descartar mensagens relevantes
    nem expor detalhes sensíveis.
-   Não crie um usuário duplicado por corrida entre verificação e
    inserção; trate a resposta de unicidade do banco/Identity.
-   Se utilizar cookies, avalie CSRF, `SameSite`, `Secure`, `HttpOnly` e
    a configuração de CORS conforme o cenário.
-   Se utilizar JWT bearer, valide assinatura, emissor, audiência,
    expiração e algoritmo/configuração esperados. Mantenha chaves e
    segredos fora do código-fonte.
-   Não armazene tokens de acesso ou refresh em `localStorage` por
    padrão sem avaliar o modelo de ameaça do cliente.
-   Não misture autenticação por cookie e bearer sem definir claramente
    quais clientes e endpoints usam cada esquema.
-   Configure CORS com origens, métodos e headers necessários. Nunca use
    origem irrestrita com credenciais.
-   Use HTTPS em ambientes que transportam credenciais ou tokens.
-   Não registre credenciais, cookies, tokens, cabeçalhos
    `Authorization` ou dados pessoais sensíveis nos logs.

## 8. OpenAPI, Scalar e Swagger

-   Mantenha a documentação da API coerente com o contrato HTTP real.
-   Identifique o mecanismo já utilizado no projeto:
    `Microsoft.AspNetCore.OpenApi`, Swashbuckle/Swagger ou outra
    integração. Não registre geradores duplicados sem necessidade.
-   Scalar e Swagger são interfaces para explorar a documentação
    OpenAPI; evite manter descrições divergentes em configurações
    paralelas.
-   Centralize a configuração de OpenAPI, esquemas de segurança,
    metadados, exemplos e transformadores em extensões/configurações
    próprias quando isso melhorar a organização.
-   Documente endpoints com resumo, descrição, parâmetros, respostas
    possíveis, códigos HTTP, autenticação e exemplos relevantes.
-   Documente DTOs e propriedades públicas com nomes e descrições úteis.
    Use XML comments quando o projeto estiver configurado para
    incluí-los no documento OpenAPI.
-   Não presuma que gerar o XML documentation file faz com que
    Scalar/Swagger o exiba automaticamente. Verifique se o documento
    OpenAPI realmente incorpora os comentários e se a UI está lendo o
    documento correto.
-   Mantenha nomes e versões dos documentos consistentes, por exemplo
    `v1`, e não duplique versões acidentalmente.
-   Configure esquemas de autenticação no OpenAPI para que a interface
    represente corretamente os endpoints protegidos. A configuração da
    documentação não substitui a autenticação real da API.
-   Não exponha documentação interativa em produção por padrão se isso
    não for uma decisão explícita. Use configuração por ambiente e
    política de acesso.
-   Não documente como resposta possível algo que o endpoint não
    retorna. Não anuncie autenticação, paginação ou filtros que não
    estejam implementados.
-   Ao alterar um endpoint, atualize seus metadados OpenAPI e verifique
    o resultado no documento gerado, não apenas na interface visual.
-   Se Scalar e Swagger coexistirem por requisito, ambos devem consumir
    o mesmo contrato OpenAPI sempre que possível.

## 9. Injeção de dependência e configuração

-   Registre serviços no composition root (`Program.cs` ou extensões
    chamadas por ele).
-   Agrupe registros por responsabilidade em métodos como
    `AddApplication`, `AddInfrastructure` e `AddApi` somente quando a
    separação trouxer clareza.
-   Use lifetimes corretamente:
    -   `Singleton`: estado compartilhado seguro para toda a aplicação;
        não pode depender de serviços scoped.
    -   `Scoped`: normalmente serviços de requisição e `DbContext`.
    -   `Transient`: serviços leves e sem estado compartilhado.
-   Não injete serviços scoped em singletons.
-   Prefira options tipadas (`IOptions<T>`, `IOptionsSnapshot<T>` ou
    `IOptionsMonitor<T>`) para configurações estruturadas.
-   Valide configurações obrigatórias no startup quando possível.
-   Não coloque secrets, connection strings reais, chaves JWT ou
    credenciais em arquivos versionados.
-   Use User Secrets no desenvolvimento e um gerenciador seguro de
    segredos/variáveis protegidas no deploy.
-   Não espalhe leitura de `IConfiguration` por toda a aplicação;
    encapsule configurações específicas em options.
-   Não crie service locator ou container paralelo.

## 10. Tratamento de erros, logs e observabilidade

-   Use tratamento global de exceções e respostas consistentes com
    `ProblemDetails`.
-   Exceções devem representar situações excepcionais; não use
    exceptions como fluxo normal de controle.
-   Não capture `Exception` para ignorar o erro ou retornar sucesso.
-   Registre contexto útil para diagnóstico, mas nunca segredos ou dados
    sensíveis.
-   Prefira logs estruturados com placeholders, por exemplo
    `logger.LogInformation("User {UserId} created", userId)`.
-   Use níveis adequados: Debug/Trace para diagnóstico, Information para
    eventos relevantes, Warning para situações recuperáveis e Error para
    falhas.
-   Evite log duplicado da mesma exceção em várias camadas.
-   Preserve a exceção original ao relançar (`throw;`), não `throw ex;`.
-   Use correlation/trace identifiers quando disponíveis.
-   Não retorne mensagens internas de exceção ao cliente.

## 11. Async, desempenho e recursos

-   Use `async`/`await` para operações de I/O.
-   Propague `CancellationToken` em operações que possam ser canceladas.
-   Evite alocações, materializações e serializações desnecessárias em
    caminhos críticos, mas não sacrifique legibilidade por
    micro-otimizações sem medição.
-   Não use `Task.Run` para operações de I/O.
-   Não faça chamadas síncronas bloqueantes em código assíncrono.
-   Dispose corretamente de recursos descartáveis; prefira
    `using`/`await using` quando aplicável.
-   Não armazene resultados de consultas em cache sem definir
    invalidação, expiração e consistência.
-   Considere `HttpClientFactory` para clientes HTTP gerenciados pela
    aplicação.
-   Não introduza cache, filas, background services ou processamento
    paralelo sem requisito concreto.

## 12. Testes e qualidade

-   Escreva testes para regras de negócio, casos de uso, validações e
    comportamentos importantes.
-   Prefira testes unitários rápidos para regras isoladas e testes de
    integração para EF Core, Identity, autenticação, autorização e
    endpoints.
-   Não confie exclusivamente no EF Core InMemory para validar
    comportamento relacional. Quando necessário, use SQLite ou um banco
    de teste compatível com o provider de produção.
-   Teste migrations e consultas críticas no provider relevante quando o
    comportamento depender dele.
-   Cubra cenários de sucesso, entrada inválida, não autenticado, sem
    permissão, recurso inexistente, conflito e falha de dependência.
-   Testes não devem depender de ordem de execução, estado compartilhado
    ou dados reais de produção.
-   Use nomes de testes que expressem cenário e resultado esperado.
-   Não altere ou remova testes apenas para fazer a suíte passar.
    Corrija a causa ou explique a incompatibilidade.
-   Antes de concluir, execute, conforme disponível:

``` bash
dotnet restore
dotnet build
dotnet test
```

-   Aplique `dotnet format` quando fizer parte do fluxo do repositório;
    revise o diff para evitar alterações não relacionadas.
-   Resolva warnings introduzidos pela mudança. Não suprima warnings
    globalmente para esconder problemas.

## 13. Segurança e privacidade

-   Trate toda entrada externa como não confiável.
-   Aplique autorização no servidor em cada operação que acesse ou
    altere recursos protegidos.
-   Evite mass assignment: mapeie explicitamente campos permitidos em
    operações de criação e atualização.
-   Use consultas parametrizadas e nunca concatene entrada em SQL,
    comandos ou expressões.
-   Valide uploads, tamanho de payloads, tipos de conteúdo e nomes de
    arquivos conforme o risco.
-   Não exponha informações de usuários por enumeração de contas em
    fluxos de login, recuperação ou confirmação.
-   Use rate limiting em endpoints sensíveis, especialmente login,
    registro, recuperação de senha e envio de códigos.
-   Não invente mecanismos criptográficos. Utilize APIs e bibliotecas
    consolidadas da plataforma.
-   Não desative validação de certificado TLS em produção.
-   Não coloque dados pessoais ou credenciais em logs, telemetria,
    fixtures públicos ou mensagens de erro.

## 14. Convenções de C

-   Siga `.editorconfig` e as convenções já existentes.
-   Use nullable reference types de forma consistente; não adicione `!`
    para silenciar warnings sem compreender a nulabilidade.
-   Prefira `var` quando o tipo for evidente pelo lado direito; use tipo
    explícito quando melhorar a leitura.
-   Use `sealed` quando a classe não foi projetada para herança e isso
    fizer sentido para o projeto.
-   Prefira injeção por construtor e dependências explícitas.
-   Evite classes estáticas com estado mutável.
-   Use records para DTOs imutáveis quando forem adequados; não
    transforme entidades mutáveis do EF Core em records automaticamente.
-   Evite métodos com muitos parâmetros; agrupe dados relacionados em
    tipos apropriados quando isso melhorar o contrato.
-   Mantenha métodos curtos e com um propósito claro, sem fragmentar
    operações simples em dezenas de métodos triviais.
-   Prefira `DateTimeOffset` para representar instantes. Use
    `DateOnly`/`TimeOnly` quando o conceito for realmente uma data ou
    hora sem instante.
-   Use `decimal` para valores financeiros.
-   Não use `dynamic` ou reflexão quando uma solução tipada for viável.

## 15. Fluxo obrigatório para novas funcionalidades

Para cada funcionalidade:

1.  Entenda o requisito, os atores envolvidos, permissões e regras de
    negócio.
2.  Localize funcionalidades semelhantes e reutilize seus padrões.
3.  Identifique a camada responsável por cada mudança.
4.  Defina contratos de entrada/saída e comportamento HTTP.
5.  Implemente regras no lugar correto, sem concentrar lógica no
    endpoint.
6.  Implemente persistência e migration, se houver alteração no modelo.
7.  Configure autenticação/autorização conforme a necessidade.
8.  Atualize OpenAPI e verifique Scalar/Swagger.
9.  Adicione ou atualize testes.
10. Execute build e testes; revise o diff e possíveis impactos de
    compatibilidade.

## 16. Fluxo obrigatório para correções

Ao corrigir um bug:

1.  Reproduza ou identifique precisamente a causa.
2.  Corrija a origem do problema, não apenas o sintoma.
3.  Evite refatorações amplas não relacionadas.
4.  Adicione um teste de regressão quando viável.
5.  Verifique efeitos sobre contratos, banco, migrations, Identity e
    documentação.
6.  Informe claramente o que foi alterado e quais verificações foram
    executadas.

## 17. O que evitar

-   Não criar uma arquitetura completa para uma API pequena sem
    necessidade.
-   Não colocar toda a aplicação em `Program.cs`, controllers ou
    endpoints.
-   Não transformar cada método em uma interface sem benefício concreto.
-   Não criar repositório genérico sobre `DbContext` apenas para
    esconder o EF Core. Use repositórios quando representarem uma
    abstração útil do domínio ou quando a arquitetura existente exigir.
-   Não duplicar `DbContext`, configurações de Identity ou registros de
    serviços.
-   Não retornar entidades EF ou objetos internos do Identity
    diretamente.
-   Não usar `EnsureCreated` junto com migrations.
-   Não aplicar migrations automaticamente em produção sem decisão
    explícita.
-   Não expor Swagger/Scalar, detalhes de erro ou dados sensíveis sem
    controle.
-   Não adicionar bibliotecas para resolver problemas que a plataforma
    já resolve adequadamente.
-   Não fazer refatorações cosméticas extensas junto de uma alteração
    funcional.
-   Não considerar uma tarefa concluída apenas porque compila:
    comportamento, segurança, documentação e testes também importam.

------------------------------------------------------------------------

## Regra final

**A melhor implementação é a mais simples que atende corretamente ao
requisito, respeita os limites da arquitetura, protege os dados e pode
ser compreendida e modificada com segurança por outra pessoa.**

Quando houver conflito entre este arquivo e a arquitetura/convenção já
consolidada no repositório, inspecione o contexto, preserve a
consistência do projeto e explique a decisão. Não imponha padrões
mecanicamente.
