# Progresso do Projeto

## Resumo Atual

- Ultima atualizacao: 2026-09-14
- Fase atual: Outbox transacional para expedicao
- Foco atual: registrar `OrderDispatched` junto da expedicao no mesmo commit de pedido e estoque
- Proximo marco concreto: criar o worker que le mensagens pendentes da Outbox para futura publicacao
- Pagamentos: `POST /payments/{orderId}/approve` confirma pedidos pendentes mantendo reservas; `POST /payments/{orderId}/reject` libera reservas atomicamente, sem migration ou entidade Payment.
- Erros de pagamento: `404 order.not_found`, `409 payment.order_not_pending` e, para reserva inconsistente na rejeicao, `409 order.inventory_inconsistent`.
- Estado real da solucao:
  - API implementada hoje: `GET /health`, endpoints de Products e Inventory ja documentados, mais `POST /orders`, `GET /orders/{id}`, `POST /orders/{id}/cancel`, `POST /orders/{id}/dispatch`, `POST /payments/{orderId}/approve` e `POST /payments/{orderId}/reject`
  - Application implementada hoje: fluxos de Products e Inventory, criacao, consulta, cancelamento e expedicao de pedidos, pagamentos simulados e registro do evento `OrderDispatched` na Outbox
  - Infrastructure implementada hoje: persistencia de Products, Inventory, Orders, OrderItems e mensagens de Outbox, com unidade de trabalho e bloqueio de linhas de pedido e estoque durante fluxos transacionais
  - Contracts implementado hoje: contrato publico `OrderDispatched` com pedido, instante UTC de expedicao e itens enviados
  - A solucao possui tratamento centralizado de erros com `AppError`, `Result<T>` tipado, traducao HTTP por tipo e codigo estavel em `ProblemDetails`
  - A criacao de pedido preserva o preco do momento da compra e grava pedido, itens e reservas em uma unica transacao; cancelamento, rejeicao e expedicao atualizam pedido e reservas na mesma transacao
  - Uma expedicao valida tambem grava uma mensagem pendente de Outbox na mesma transacao; erros e tentativas concorrentes nao deixam mensagens extras
  - Aumento, reserva e liberacao de estoque usam transacoes e bloqueios de linha para preservar atualizacoes concorrentes
  - O preparo de um ambiente novo usa `dotnet-ef` local e aplicacao explicita das migrations

## Snapshot de Validacao

- SDK .NET validado: `10.0.401`
- Entity Framework CLI local: `10.0.9`
- Build: `dotnet build OrderFlow.slnx` executado com sucesso
- Testes totais: 197 aprovados
- Testes de integracao: fluxos de Products, Inventory, Orders e Payments, incluindo rollback, preco historico, concorrencia e persistencia da Outbox, usando PostgreSQL real via um Testcontainer, bancos isolados por feature, migrations e reset por Respawn
- Observacoes de ambiente:
  - projetos e suites validados em 2026-09-14 com build serial (`-m:1 -nodeReuse:false`) para evitar bloqueios locais do MSBuild
  - restore emitiu `NU1900` porque a consulta de vulnerabilidades ao NuGet estava indisponivel; pacotes restaurados e builds concluidos
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

Status: `Concluido`

- [x] Simular pagamento aprovado
- [x] Simular pagamento rejeitado
- [x] Atualizar status para Confirmed na aprovacao
- [x] Liberar estoque em caso de rejeicao
- [x] Testar aprovacao, estados invalidos, reservas preservadas e concorrencia com cancelamento
- [x] Testar rejeicao de pagamento, transicoes invalidas e concorrencia

### Fase 8 - Mensageria

Status: `Em andamento`

- [x] Criar `OrderFlow.Contracts` e o contrato `OrderDispatched`
- [x] Registrar `OrderDispatched` em uma Outbox transacional durante a expedicao
- [x] Criar migration para `outbox_messages` com payload `jsonb` e indice parcial de pendentes
- [x] Testar persistencia, falhas e concorrencia da Outbox com PostgreSQL real
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
- [x] Adicionar outbox pattern transacional (antecipado na Fase 8)
- [ ] Adicionar observabilidade com .NET Aspire

## Proximos Passos

- Criar um worker que busca mensagens pendentes em `outbox_messages`
- Definir como o worker registra tentativa, sucesso e falha antes de integrar RabbitMQ e MassTransit

## Historico de Atualizacoes

- 2026-09-14 - Iniciada a Fase 8: criado `OrderFlow.Contracts`, o evento `OrderDispatched` e a Outbox transacional. A migration cria `outbox_messages` com payload `jsonb` e indice parcial para pendencias; 197 testes aprovados, incluindo sucesso, erro e concorrencia da expedicao.
- 2026-09-07 - Expedicao integral implementada: `POST /orders/{id}/dispatch` baixa estoque fisico e reserva para pedidos confirmados, registra `DispatchedAt` e usa migration; 196 testes aprovados. Colecao Postman ampliada para 99 requisicoes e 15 endpoints.
- 2026-09-07 - Rejeicao simulada implementada com liberacao atomica de reservas, bloqueios transacionais e cobertura de concorrencia com aprovacao e cancelamento; 164 testes aprovados. Colecao Postman ampliada para 87 requisicoes e 14 endpoints.
- 2026-09-07 - Aprovacao simulada implementada com reservas preservadas e bloqueio de pedido; 147 testes aprovados, incluindo disputa entre aprovacao e cancelamento. Colecao Postman ampliada para 77 requisicoes e 13 endpoints.
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
