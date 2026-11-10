# OrderFlow

OrderFlow é um projeto de estudo backend focado na construção de uma API realista de gerenciamento de pedidos usando .NET, PostgreSQL, Docker e testes automatizados.

A proposta é evoluir uma API simples para um backend com preocupações próximas de um sistema real, passando por criação de pedidos, reserva de estoque, pagamentos simulados, processamento em background, mensageria, idempotência e observabilidade.

## Status do projeto

Em desenvolvimento.

Documentação oficial por objetivo:

- Apresentação geral e execução local: `README.md`
- Escopo, regras e roadmap: `docs/PROJECT_SCOPE.md`
- Progresso real de execução: `docs/PROJECT_PROGRESS.md`

Resumo atual:

- Fundação e MVP funcional concluídos
- Produtos: criação, listagem, consulta por ID e atualização de preço/status
- Estoque: criação, consulta e aumento, com proteção contra aumentos e reservas concorrentes
- Pedidos: criação, consulta por ID, cancelamento e expedição, com reserva/liberação/baixa atômica de estoque
- Pagamentos: aprovação e rejeição simuladas, com confirmação transacional e manutenção ou liberação das reservas
- Outbox transacional: expedição grava o evento pendente `OrderDispatched` no mesmo commit do pedido e estoque
- Erros de aplicação traduzidos para `ProblemDetails` com código estável
- Último snapshot completo validado com 223 testes, incluindo PostgreSQL, publicação confirmada e recuperação de RabbitMQ reais
- `OrderFlow.Worker` publica lotes da Outbox no RabbitMQ com confirms, mandatory e MessageId estavel

Use `docs/PROJECT_PROGRESS.md` como fonte oficial para saber o que já foi feito, o que está em andamento e o que falta.

## Objetivos

Este projeto tem como objetivo praticar e demonstrar:

- Criação de APIs REST com ASP.NET Core
- Persistência com PostgreSQL e Entity Framework Core
- Separação entre API, aplicação, domínio e infraestrutura
- Regras de negócio envolvendo pedidos, estoque e pagamentos
- Transações e proteção contra concorrência
- Testes de integração usando banco de dados real
- Ambiente local reproduzível com Docker
- Processamento assíncrono e preocupações de produção em fases posteriores

## Stack atual

- .NET 10 (LTS)
- ASP.NET Core Web API
- Entity Framework Core 10
- PostgreSQL 16
- Docker / Docker Compose
- xUnit v3 com Microsoft Testing Platform
- Testcontainers
- RabbitMQ 4.4.0 e RabbitMQ.Client 7.2.2 (publicação em broker único)

Planejado para fases posteriores:

- MassTransit
- .NET Aspire

## Funcionalidades atuais

### Catálogo de produtos

- Criar produtos
- Listar produtos
- Buscar detalhes de um produto
- Atualizar preço e status de um produto

### Estoque

- Registrar estoque para produtos
- Aumentar quantidade disponível
- Reservar estoque ao criar um pedido
- Liberar estoque reservado ao cancelar um pedido
- Serializar alterações concorrentes sobre o mesmo estoque

### Pedidos

- Criar pedido
- Validar produtos e disponibilidade em estoque
- Reservar estoque atomicamente
- Consultar pedido por ID
- Cancelar pedido
- Liberar estoque reservado no cancelamento
- Expedir integralmente pedido confirmado, com baixa física e encerramento da reserva

### Pagamentos

- Aprovar ou rejeitar pagamento com `POST /payments/{orderId}/approve` e `POST /payments/{orderId}/reject`, sem corpo
- Aceitar somente pedidos pendentes; retornar `404 order.not_found` ou `409 payment.order_not_pending` nos erros esperados
- Aprovação mantém estoque reservado até a expedição; rejeição libera a reserva atomicamente

### Funcionalidades planejadas

- Publicar e processar eventos de forma assíncrona
- Adicionar retries, idempotência e observabilidade

## Roadmap resumido

- Fase 1 - Fundação: concluída
- Fase 2 - Produtos: concluída
- Fase 3 - Estoque: concluída
- Fase 4 - Pedidos: concluída
- Fase 5 - Cancelamento: concluída
- Fase 6 - Testes de integração: concluída
- Fase 7 - Pagamentos: concluída (aprovação e rejeição implementadas)
- Expedição integral: concluída
- Fase 8 - Mensageria: em andamento (Outbox transacional e primeiro processador em background concluídos)
- Fase 9 - Produção: pendente

Para o roadmap detalhado, consulte `docs/PROJECT_SCOPE.md` e `docs/PROJECT_PROGRESS.md`.

## Executando localmente

Arquivo de solução:

- `OrderFlow.slnx`

### Requisitos

- .NET SDK 10
- Docker com Docker Compose

O CLI do Entity Framework está fixado na versão `10.0.9` pelo arquivo `dotnet-tools.json`; não é necessário instalá-lo globalmente.

### 1. Restaurar ferramentas e dependências

Na raiz do repositório:

```bash
dotnet tool restore
dotnet restore OrderFlow.slnx
```

### 2. Subir o PostgreSQL

Copie `.env.example` para `.env` na raiz do repositorio. Os valores de exemplo sao para desenvolvimento local; o `.env` fica fora do Git. O Compose sobe PostgreSQL e RabbitMQ:

```powershell
Copy-Item .env.example .env
```

```bash
docker compose up -d
```

### 3. Criar ou atualizar o banco

```bash
dotnet tool run dotnet-ef database update --project src/OrderFlow.Infrastructure --startup-project src/OrderFlow.Api
```

As migrations são aplicadas explicitamente. A API não altera o schema automaticamente durante a inicialização.

### 4. Executar a API

```bash
dotnet run --project src/OrderFlow.Api
```

Documentação interativa:

- Scalar UI: `http://localhost:5059/scalar`
- OpenAPI JSON: `http://localhost:5059/openapi/v1.json`

Health check:

- `http://localhost:5059/health`

### 5. Executar os testes

Mantenha o Docker ativo porque os testes de integração criam um PostgreSQL real com Testcontainers.

```bash
dotnet test --solution OrderFlow.slnx
```

A suíte está separada por responsabilidade:

```bash
# Não depende de Docker
dotnet test --project tests/OrderFlow.UnitTests/OrderFlow.UnitTests.csproj

# Depende do Docker para criar o PostgreSQL efêmero
dotnet test --project tests/OrderFlow.IntegrationTests/OrderFlow.IntegrationTests.csproj
```

Nos testes unitários, builders de domínio mantêm defaults válidos e dados incidentais determinísticos; fixtures locais concentram mocks apenas nos casos de uso com setup repetitivo. Nos testes integrados, uma fixture de assembly compartilha um único container PostgreSQL, cria um banco por feature, aplica migrations e usa Respawn antes de cada teste.

As classes da mesma feature pertencem à mesma collection e executam em série; Products, Inventory e Orders usam bancos distintos e podem executar em paralelo. A preparação de cenários integrados é feita por builders persistentes específicos.

Os projetos usam xUnit v3 com Microsoft Testing Platform, selecionado pelo `global.json`; no .NET 10, os comandos indicam explicitamente `--solution` ou `--project`. É possível filtrar por traits de categoria, camada e feature:

```bash
dotnet test --project tests/OrderFlow.UnitTests/OrderFlow.UnitTests.csproj -- --filter-query '/[Category=Unit]'
dotnet test --project tests/OrderFlow.UnitTests/OrderFlow.UnitTests.csproj -- --filter-query '/[Layer=Domain]'
dotnet test --project tests/OrderFlow.UnitTests/OrderFlow.UnitTests.csproj -- --filter-query '/[Feature=Products]'
dotnet test --project tests/OrderFlow.IntegrationTests/OrderFlow.IntegrationTests.csproj -- --filter-query '/[Category=Integration]'
dotnet test --project tests/OrderFlow.IntegrationTests/OrderFlow.IntegrationTests.csproj -- --filter-query '/[Layer=Api]'
dotnet test --project tests/OrderFlow.IntegrationTests/OrderFlow.IntegrationTests.csproj -- --filter-query '/[Feature=Orders]'
```

Para gerar cobertura em formato Cobertura:

```bash
dotnet test --project tests/OrderFlow.UnitTests/OrderFlow.UnitTests.csproj --coverlet --coverlet-output-format cobertura --coverlet-file-prefix unit
dotnet test --project tests/OrderFlow.IntegrationTests/OrderFlow.IntegrationTests.csproj --coverlet --coverlet-output-format cobertura --coverlet-file-prefix integration
```

O relatório é criado em `TestResults/`.

### 6. Verificar a formatação

```bash
dotnet format OrderFlow.slnx --verify-no-changes --no-restore
```

### Encerrar as dependências locais

```bash
docker compose down
```

## Notas de aprendizado

O roteiro para evoluir a mensageria em pequenos incrementos esta em [MESSAGING_LEARNING_PLAN.md](docs/MESSAGING_LEARNING_PLAN.md). Cada bloco sera explicado e revisado antes de avancar; o plano nao autoriza implementacao automatica.

O Worker publica a Outbox no RabbitMQ usando `RabbitMqEventPublisher`. A conexao e o canal sao abertos na primeira publicacao e reutilizados; o publicador declara o exchange direto `orderflow.events`, a fila quorum `orderflow.order-dispatched` e o binding `order.dispatched.v1`. A publicacao e persistente, usa o Id da Outbox como MessageId e aguarda confirmacao do broker com mandatory. Falhas permanecem pendentes na Outbox. Ainda nao ha consumidor, cluster, claim/lease, Inbox ou filas de retry/DLQ; esses conceitos ficam para blocos futuros. Mensagens historicas marcadas como processadas pelo logger nao sao reabertas automaticamente.

A inclusão e o processamento usam `IOutboxRepository`. O método `Add<TEvent>` recebe o identificador estável do evento e seu contrato tipado; a Infrastructure serializa o payload e prepara a entidade interna `OutboxMessage`, sem salvar antecipadamente. O commit continua sob responsabilidade da unidade de trabalho. Os identificadores públicos ficam em `EventTypes`, no projeto Contracts.

A recuperacao automatica de conexao e topologia esta habilitada no cliente RabbitMQ. Durante uma queda, a Outbox permanece pendente e o publisher preserva a conexao para a biblioteca recupera-la. Falha na primeira conexao e canal fechado por erro de protocolo sao tratados na proxima tentativa do Worker. A recuperacao da conexao nao republica mensagens; o Worker continua responsavel por tentar novamente com o mesmo MessageId. Confirmacao perdida ainda pode causar duplicatas.

### Executar a publicacao no ambiente local

Com RabbitMQ e PostgreSQL ativos, execute `dotnet run --project src/OrderFlow.Worker`. O perfil Development usa guest/guest apenas para o broker local. O processo .NET nao importa o .env do Compose; credenciais diferentes devem ser configuradas por user secrets ou variaveis como RabbitMq__UserName e RabbitMq__Password. Fora de Development, as credenciais sao obrigatorias e nao tem fallback. Para observar o fluxo, expeda um pedido confirmado e veja a mensagem na fila pelo painel em http://localhost:15672. A Outbox deve ter ProcessedAt preenchido somente apos publicacao confirmada. A fila permanece com a mensagem porque este bloco ainda nao inclui consumidor.
