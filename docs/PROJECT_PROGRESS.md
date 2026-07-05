# Progresso do Projeto

## Resumo Atual

- Ultima atualizacao: 2026-07-04
- Fase atual: Fase 3 - Estoque
- Foco atual: conectar o dominio de estoque ja iniciado com API, Application, Infrastructure e testes de integracao
- Proximo marco concreto: implementar `POST /inventory/items` para registrar o primeiro estoque de produto
- Estado real da solucao:
  - API implementada hoje: `GET /health`, `POST /products`, `GET /products`, `GET /products/{id}`, `PATCH /products/{id}/price` e `PATCH /products/{id}/status`
  - Application implementada hoje: fluxos de criacao, listagem, consulta por id e atualizacao de produto
  - Infrastructure implementada hoje: persistencia, listagem, consulta por id e atualizacao de `Product`
  - Dominio ja possui bases de `InventoryItem`, `Order` e `OrderItem`, mas esses fluxos ainda nao chegaram na API, Application e Infrastructure

## Snapshot de Validacao

- Build: validado durante `dotnet test OrderFlow.slnx`
- Testes totais: 66 aprovados
- Testes de integracao implementados hoje: criacao, listagem, consulta por id e atualizacao de produto, com seed direto no banco para isolar os cenarios de leitura e update
- Observacoes de ambiente:
  - `dotnet test OrderFlow.slnx` executado em 2026-07-04
  - restauracao, compilacao e execucao dos testes passaram
  - a suite ficou verde no ambiente atual apos a reorganizacao dos testes por camada e feature

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

Status: `Em andamento`

- [x] Criar entidade `InventoryItem`
- [x] Implementar regras de aumento, reserva e liberacao no dominio
- [x] Testar regras principais de estoque no dominio
- [ ] Integrar estoque ao `OrderFlowDbContext` e as migrations
- [ ] Criar endpoints de estoque
- [ ] Implementar entrada de estoque na aplicacao
- [ ] Criar persistencia com EF Core para estoque
- [ ] Impedir fluxos invalidos na borda da aplicacao
- [ ] Criar testes de integracao para estoque

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
- [x] Validar execucao com Docker ativo no ambiente
- [ ] Testar entrada de estoque
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

- Integrar `InventoryItem` ao `OrderFlowDbContext` e preparar as migrations correspondentes
- Criar `POST /inventory/items`, `POST /inventory/items/{productId}/increase` e `GET /inventory/items/{productId}`
- Iniciar Estoque conectando o dominio ja pronto com API, Application, Infrastructure e testes
- Criar o fluxo de criacao de Pedido validando produto ativo e estoque disponivel

## Historico de Atualizacoes

- 2026-07-04 - Refatorada a suite de testes de Products com organizacao por camada/feature, `FakeProductRepository` compartilhado e seed direto no banco nos testes de integracao.
- 2026-07-04 - Simplificada a API de mutacao de `Product` com remocao do metodo generico `Update(...)`; o snapshot da suite passou a refletir 66 testes aprovados.
- 2026-07-04 - Concluida a Fase 2 de Produtos com `GET /products`, `PATCH /products/{id}/price` e `PATCH /products/{id}/status`, alem de novos testes unitarios e de integracao.
- 2026-07-04 - Atualizado o snapshot da suite para 70 testes aprovados apos finalizar o CRUD de Products.
- 2026-07-04 - Criado o arquivo oficial de progresso e consolidado o estado real do repositorio com base em codigo, estrutura e testes.
- 2026-07-04 - Reconciliado o progresso com o estado real do codigo: API/Application/Infrastructure ainda estao concentradas em Produtos, enquanto Estoque e Pedidos ja possuem base no dominio.
- 2026-07-04 - Validado `dotnet test OrderFlow.slnx` com 46 testes aprovados e 0 falhas no ambiente atual.
- 2026-07-04 - Implementado `GET /products/{id}` com reaproveitamento de DTOs de saida, consulta no repositorio e testes de integracao cobrindo sucesso e `404`.
