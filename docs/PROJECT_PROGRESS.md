# Progresso do Projeto

## Resumo Atual

- Ultima atualizacao: 2026-07-11
- Fase atual: Fase 4 - Pedidos
- Foco atual: iniciar o fluxo ponta a ponta de pedidos sobre a base ja concluida de produtos e estoque
- Proximo marco concreto: integrar `Order` e `OrderItem` ao `OrderFlowDbContext` e preparar a persistencia do primeiro `POST /orders`
- Estado real da solucao:
  - API implementada hoje: `GET /health`, `POST /products`, `GET /products`, `GET /products/{id}`, `PATCH /products/{id}/price`, `PATCH /products/{id}/status`, `POST /inventory/items`, `GET /inventory/items/{productId}` e `POST /inventory/items/{productId}/increase`
  - Application implementada hoje: fluxos de criacao, listagem, consulta por id e atualizacao de produto, alem de `CreateInventoryItemUseCase`, `GetInventoryItemByProductIdUseCase` e `IncreaseInventoryItemStockUseCase`
  - Infrastructure implementada hoje: persistencia, listagem, consulta por id e atualizacao de `Product`, mais repositorio de estoque com `Add`, `ExistsByProductId`, `GetByProductId` e `Update`
  - A solucao agora possui tratamento centralizado de erros de aplicacao com `AppError`, `Result<T>` tipado e traducao HTTP por tipo
  - Dominio ja possui bases de `InventoryItem`, `Order` e `OrderItem`; Pedidos ainda nao chegaram na API, Application e Infrastructure

## Snapshot de Validacao

- SDK .NET validado: `10.0.301`
- Build: `dotnet build OrderFlow.slnx` executado com sucesso
- Testes totais: 90 aprovados
- Testes de integracao implementados hoje: criacao, listagem, consulta por id e atualizacao de produto, mais criacao inicial, consulta por produto e aumento de estoque, com seed direto no banco para isolar os cenarios
- Observacoes de ambiente:
  - `dotnet build OrderFlow.slnx` e `dotnet test OrderFlow.slnx` executados em 2026-07-11
  - build e execucao da suite passaram no ambiente atual
  - os testes de integracao dependem de Docker acessivel no ambiente

## Progresso por Fase

### Fase 1 - Fundacao

Status: `Concluido`

- [x] Criar solucao .NET
- [x] Criar projetos principais
- [x] Configurar PostgreSQL com Docker Compose
- [x] Configurar EF Core
- [x] Configurar migrations
- [x] Configurar OpenAPI + Scalar
- [x] Criar endpoint de health check

### Fase 2 - Produtos

Status: `Concluido`

- [x] Criar entidade `Product`
- [x] Criar endpoint `POST /products`
- [x] Criar validacoes basicas de entrada
- [x] Criar use case de criacao de produto
- [x] Criar persistencia com EF Core para produto
- [x] Criar testes unitarios do dominio de produto
- [x] Criar testes de integracao para criacao de produto
- [x] Implementar `GET /products`
- [x] Implementar `GET /products/{id}`
- [x] Implementar `PATCH /products/{id}/price`
- [x] Implementar `PATCH /products/{id}/status`

### Fase 3 - Estoque

Status: `Concluido`

- [x] Criar entidade `InventoryItem`
- [x] Implementar regras de aumento, reserva e liberacao no dominio
- [x] Testar regras principais de estoque no dominio
- [x] Integrar estoque ao `OrderFlowDbContext` e as migrations
- [x] Criar endpoints iniciais de estoque (`POST /inventory/items` e `GET /inventory/items/{productId}`)
- [x] Implementar entrada inicial de estoque na aplicacao (`POST /inventory/items`)
- [x] Criar persistencia com EF Core para estoque
- [x] Impedir fluxos invalidos na borda da aplicacao para criacao inicial de estoque
- [x] Criar testes de integracao para criacao inicial de estoque
- [x] Implementar `GET /inventory/items/{productId}`
- [x] Implementar `POST /inventory/items/{productId}/increase`
- [x] Fechar o fluxo principal restante de estoque com aumento de quantidade

### Fase 4 - Pedidos

Status: `Em andamento`

- [x] Criar entidades `Order` e `OrderItem`
- [x] Implementar calculo de total no dominio
- [x] Implementar transicoes basicas de status no dominio
- [x] Testar regras principais de pedido no dominio
- [ ] Integrar pedidos ao `OrderFlowDbContext` e as migrations
- [ ] Criar endpoint de criacao de pedido
- [ ] Validar produtos e estoque no fluxo de aplicacao
- [ ] Reservar estoque dentro da criacao do pedido
- [ ] Persistir pedido e itens
- [ ] Criar endpoint `GET /orders/{id}`

### Fase 5 - Cancelamento

Status: `Em andamento`

- [x] Iniciar regra de cancelamento no dominio
- [ ] Implementar cancelamento de pedido no fluxo de aplicacao
- [ ] Liberar estoque reservado ao cancelar
- [ ] Impedir cancelamento invalido na API/Application
- [ ] Criar endpoint `POST /orders/{id}/cancel`
- [ ] Testar cancelamento e liberacao de estoque ponta a ponta

### Fase 6 - Testes de Integracao

Status: `Em andamento`

- [x] Configurar Testcontainers com PostgreSQL
- [x] Criar infraestrutura base para testes de integracao
- [x] Testar criacao de produto
- [x] Testar criacao inicial de estoque
- [x] Testar consulta de estoque por produto
- [x] Testar aumento de estoque
- [x] Validar execucao com Docker ativo no ambiente
- [ ] Testar criacao de pedido
- [ ] Testar pedido com estoque insuficiente
- [ ] Testar cancelamento de pedido

### Fase 7 - Pagamentos

Status: `Pendente`

- [ ] Simular pagamento aprovado
- [ ] Simular pagamento rejeitado
- [ ] Atualizar status do pedido
- [ ] Liberar estoque em caso de rejeicao
- [ ] Testar fluxo de pagamento

### Fase 8 - Mensageria

Status: `Pendente`

- [ ] Adicionar RabbitMQ
- [ ] Adicionar MassTransit
- [ ] Publicar eventos de pedido
- [ ] Criar worker para processar eventos
- [ ] Adicionar retries basicos

### Fase 9 - Producao

Status: `Pendente`

- [ ] Adicionar logs estruturados
- [ ] Adicionar health checks completos
- [ ] Adicionar idempotencia
- [ ] Adicionar outbox pattern
- [ ] Adicionar observabilidade com .NET Aspire

## Proximos Passos

- Integrar `Order` e `OrderItem` ao `OrderFlowDbContext`, configuracoes EF Core e migrations
- Criar o fluxo de criacao de Pedido validando produto ativo e estoque disponivel
- Reservar estoque dentro da criacao do pedido e persistir pedido + itens
- Expandir os testes de integracao para cobrir o primeiro fluxo real de pedidos

## Historico de Atualizacoes

- 2026-07-11 - Implementado `POST /inventory/items/{productId}/increase` com request dedicado, use case, persistencia via `UpdateAsync` e testes unitarios/integracao.
- 2026-07-11 - Concluida a Fase 3 de Estoque com API de criacao, consulta e aumento, elevando a suite para 90 testes aprovados.
- 2026-07-11 - Sincronizado o `PROJECT_PROGRESS.md` com o estado real do codigo, incluindo o endpoint `GET /inventory/items/{productId}` no retrato oficial da API.
- 2026-07-11 - Atualizado o snapshot de validacao para .NET SDK `10.0.301`, build verde e 83 testes aprovados no ambiente atual.
- 2026-07-06 - Implementado `POST /inventory/items` com API, use case, repositorio, testes de integracao e persistencia protegendo a regra de um estoque por produto.
- 2026-07-06 - Refatorado o tratamento de erros para `AppError` global com `Result<T>` tipado e traducao HTTP centralizada por tipo (`Validation`, `NotFound`, `Conflict`).
- 2026-07-06 - Atualizado o snapshot da suite para 77 testes aprovados apos concluir create inventory e a revisao do fluxo de erros.
- 2026-07-04 - Refatorada a suite de testes de Products com organizacao por camada/feature, `FakeProductRepository` compartilhado e seed direto no banco nos testes de integracao.
- 2026-07-04 - Simplificada a API de mutacao de `Product` com remocao do metodo generico `Update(...)`; o snapshot da suite passou a refletir 66 testes aprovados.
- 2026-07-04 - Concluida a Fase 2 de Produtos com `GET /products`, `PATCH /products/{id}/price` e `PATCH /products/{id}/status`, alem de novos testes unitarios e de integracao.
- 2026-07-04 - Atualizado o snapshot da suite para 70 testes aprovados apos finalizar o CRUD de Products.
- 2026-07-04 - Criado o arquivo oficial de progresso e consolidado o estado real do repositorio com base em codigo, estrutura e testes.
- 2026-07-04 - Reconciliado o progresso com o estado real do codigo: API/Application/Infrastructure ainda estao concentradas em Produtos, enquanto Estoque e Pedidos ja possuem base no dominio.
- 2026-07-04 - Validado `dotnet test OrderFlow.slnx` com 46 testes aprovados e 0 falhas no ambiente atual.
- 2026-07-04 - Implementado `GET /products/{id}` com reaproveitamento de DTOs de saida, consulta no repositorio e testes de integracao cobrindo sucesso e `404`.
