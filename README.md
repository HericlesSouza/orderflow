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
- Último snapshot completo validado com 197 testes, incluindo integração com PostgreSQL real
- `OrderFlow.Worker` processa lotes pendentes da Outbox, registra tentativas e usa `LoggingEventPublisher` como destino provisório

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

Planejado para fases posteriores:

- RabbitMQ
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

O projeto evolui em fases. O MVP prioriza um backend funcional, compreensível e testável. Pagamentos simulados e expedição integral já concluem o ciclo principal do pedido; a Outbox transacional iniciou a fase de mensageria. O `OrderFlow.Worker` já busca mensagens pendentes em lotes, publica por uma abstração inicial baseada em logs e registra tentativas, sucesso e falha. Nesta etapa, `LoggingEventPublisher` é um destino terminal provisório: mensagens marcadas como processadas após o log não serão reenviadas automaticamente quando RabbitMQ for adicionado. O `OutboxProcessor` possui cobertura unitária específica e o `OutboxMessageRepository` possui cobertura integrada com PostgreSQL; os próximos incrementos são tornar polling e lote configuráveis, adicionar backoff/dead-letter e então integrar RabbitMQ e MassTransit. Concorrência entre instâncias, idempotência e observabilidade permanecem mapeadas para a evolução de produção.
