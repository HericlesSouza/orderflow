# Progresso do Projeto

## Resumo Atual

- Ultima atualizacao: 2026-08-17
- Fase atual: Fase 7 - Pagamentos
- Foco atual: MVP estabilizado e pronto para iniciar o fluxo de pagamento simulado
- Proximo marco concreto: definir o contrato do primeiro fluxo de pagamento e confirmar ou rejeitar um pedido de forma consistente
- Estado real da solucao:
  - API implementada hoje: `GET /health`, endpoints de Products e Inventory ja documentados, mais `POST /orders`, `GET /orders/{id}` e `POST /orders/{id}/cancel`
  - Application implementada hoje: fluxos de Products e Inventory, mais criacao, consulta por id e cancelamento de pedidos com validacao de produto e estoque
  - Infrastructure implementada hoje: persistencia de Products, Inventory, Orders e OrderItems, com unidade de trabalho e bloqueio de linhas de pedido e estoque durante fluxos transacionais
  - A solucao possui tratamento centralizado de erros com `AppError`, `Result<T>` tipado, traducao HTTP por tipo e codigo estavel em `ProblemDetails`
  - A criacao de pedido preserva o preco do momento da compra e grava pedido, itens e reservas em uma unica transacao; o cancelamento libera as reservas e atualiza o status na mesma transacao
  - Aumento, reserva e liberacao de estoque usam transacoes e bloqueios de linha para preservar atualizacoes concorrentes
  - O preparo de um ambiente novo usa `dotnet-ef` local e aplicacao explicita das migrations

## Snapshot de Validacao

- SDK .NET validado: `10.0.400`
- Entity Framework CLI local: `10.0.9`
- Build: `dotnet build OrderFlow.slnx` executado com sucesso
- Testes totais: 130 aprovados (`82` unitarios/arquiteturais e `48` integrados)
- Testes de integracao: fluxos de Products, Inventory e Orders, incluindo rollback, preco historico e concorrencia, usando PostgreSQL real via um Testcontainer, bancos isolados por feature, migrations e reset por Respawn
- Observacoes de ambiente:
  - `dotnet build OrderFlow.slnx` e `dotnet test OrderFlow.slnx` executados em 2026-08-17
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
- [x] Proteger aumentos e reservas concorrentes com transacao e bloqueio de linha

### Fase 4 - Pedidos

Status: `Concluido`

- [x] Criar entidades `Order` e `OrderItem`
- [x] Implementar calculo de total no dominio
- [x] Implementar transicoes basicas de status no dominio
- [x] Testar regras principais de pedido no dominio
- [x] Integrar pedidos ao `OrderFlowDbContext` e as migrations
- [x] Criar endpoint de criacao de pedido
- [x] Validar produtos e estoque no fluxo de aplicacao
- [x] Reservar estoque dentro da criacao do pedido
- [x] Persistir pedido e itens de forma atomica
- [x] Proteger a reserva concorrente com bloqueio de estoque por linha
- [x] Criar endpoint `GET /orders/{id}`

### Fase 5 - Cancelamento

Status: `Concluido`

- [x] Iniciar regra de cancelamento no dominio
- [x] Implementar cancelamento de pedido no fluxo de aplicacao
- [x] Liberar estoque reservado ao cancelar
- [x] Impedir cancelamento invalido na API/Application
- [x] Criar endpoint `POST /orders/{id}/cancel`
- [x] Testar cancelamento e liberacao de estoque ponta a ponta
- [x] Priorizar o erro de estado em tentativas repetidas de cancelamento

### Fase 6 - Testes de Integracao

Status: `Concluido`

- [x] Configurar Testcontainers com PostgreSQL
- [x] Criar infraestrutura base para testes de integracao
- [x] Testar criacao de produto
- [x] Testar criacao inicial de estoque
- [x] Testar consulta de estoque por produto
- [x] Testar aumento de estoque
- [x] Validar execucao com Docker ativo no ambiente
- [x] Testar criacao de pedido
- [x] Testar pedido com estoque insuficiente
- [x] Testar cancelamento de pedido
- [x] Testar aumentos concorrentes e aumento concorrente com reserva de estoque

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

- Definir o contrato do fluxo de pagamento aprovado
- Implementar a confirmacao de pedido apos pagamento aprovado
- Implementar a rejeicao de pagamento com liberacao atomica de estoque
- Criar testes unitarios e de integracao para os dois resultados de pagamento

## Historico de Atualizacoes

- 2026-08-17 - Refatoracao da suite finalizada com namespaces por camada/feature/caso de uso, fixtures unitarias locais, API de dados com builders persistentes, traits completos e teardown explicito dos bancos integrados.
- 2026-08-17 - Suite separada em `OrderFlow.UnitTests` e `OrderFlow.IntegrationTests`, migrada para xUnit v3/MTP e organizada com traits, builders deterministicas e theories tipadas.
- 2026-08-17 - Testes integrados passaram a compartilhar um PostgreSQL Testcontainer, com bancos isolados por feature e limpeza por Respawn; suite validada com 130 testes.
- 2026-07-19 - Estabilizado o MVP: aumento de estoque protegido por transacao e `FOR UPDATE`, cancelamento repetido retornando `order.cannot_cancel` e codigos de erro expostos em `ProblemDetails`.
- 2026-07-19 - Adicionado `dotnet-ef` `10.0.9` como ferramenta local, documentado o bootstrap completo do banco e normalizada a formatacao do repositorio.
- 2026-07-19 - Suite completa validada com .NET SDK `10.0.302`: build sem avisos/erros e 127 testes aprovados com PostgreSQL via Testcontainers.
- 2026-07-13 - Concluidas as Fases 5 e 6: implementado `POST /orders/{id}/cancel`, com bloqueio transacional do pedido e do estoque, liberacao atomica de reservas e testes de integracao para sucesso, erro, repeticao e inconsistencias.
- 2026-07-13 - Suite validada com .NET SDK `10.0.301`: build sem avisos/erros e 122 testes aprovados, incluindo Testcontainers com PostgreSQL via Docker.
- 2026-07-11 - Concluida a Fase 4 com `POST /orders`, `GET /orders/{id}`, persistencia de `Order`/`OrderItem`, preco historico e reserva atomica de estoque.
- 2026-07-11 - Adicionada protecao contra sobrevenda com bloqueio pessimista por linha no PostgreSQL e teste concorrente, elevando a suite para 109 testes aprovados.
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
