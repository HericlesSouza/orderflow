# OrderFlow

OrderFlow é um projeto de estudo backend focado na construção de uma API realista de gerenciamento de pedidos usando .NET, PostgreSQL, Docker, testes automatizados e processamento assíncrono.

A ideia é evoluir uma API simples para um backend com preocupações mais próximas de um sistema real, passando por criação de pedidos, reserva de estoque, simulação de pagamento, processamento em background, mensageria, idempotência e observabilidade.

## Status do Projeto

🚧 Em desenvolvimento

Foco atual:

- Configuração inicial do projeto
- Catálogo de produtos
- Controle de estoque
- Criação de pedidos com reserva de estoque

## Objetivos

Este projeto tem como objetivo praticar e demonstrar:

- Criação de APIs REST com ASP.NET Core
- Persistência com PostgreSQL e Entity Framework Core
- Separação entre camadas de API, aplicação, domínio e infraestrutura
- Regras de negócio envolvendo pedidos, estoque e pagamentos
- Testes de integração usando banco de dados real
- Ambiente local com Docker
- Processamento assíncrono com RabbitMQ e MassTransit
- Preocupações de produção como logs, health checks, retries e idempotência

## Stack Tecnológica

- .NET 10 (LTS)
- ASP.NET Core Web API
- Entity Framework Core
- PostgreSQL
- Docker / Docker Compose
- xUnit
- Testcontainers
- RabbitMQ
- MassTransit

## Funcionalidades Principais

### Catálogo de Produtos

- Criar produtos
- Listar produtos
- Buscar detalhes de um produto
- Atualizar preço/status de um produto

### Estoque

- Registrar estoque para produtos
- Aumentar quantidade disponível
- Reservar estoque ao criar um pedido
- Liberar estoque reservado ao cancelar um pedido

### Pedidos

- Criar pedido
- Validar disponibilidade dos produtos
- Reservar estoque
- Cancelar pedido
- Acompanhar status do pedido

### Pagamentos

- Simular aprovação de pagamento
- Simular rejeição de pagamento
- Confirmar ou cancelar pedidos com base no resultado do pagamento

### Processamento em Background

Planejado:

- Publicar eventos de domínio/integração
- Processar eventos de pedidos de forma assíncrona
- Atualizar status de pedidos por meio de workers
- Aplicar padrões de retry e outbox

## Regras de Negócio

Regras iniciais:

- Um pedido não pode ser criado com produtos indisponíveis.
- Ao criar um pedido, o estoque dos produtos deve ser reservado.
- Ao cancelar um pedido, o estoque reservado deve ser liberado.
- Um pedido pago não pode ser cancelado pelo fluxo comum de cancelamento.
- A aprovação do pagamento confirma o pedido.
- A rejeição do pagamento cancela o pedido e libera o estoque.
- A quantidade em estoque não pode ficar negativa.

## Visão Geral da API

Endpoints iniciais:

```http
POST /products
GET /products
GET /products/{id}

POST /inventory/items
POST /inventory/items/{productId}/increase
GET /inventory/items/{productId}

POST /orders
GET /orders/{id}
POST /orders/{id}/cancel
```

Endpoints planejados:

```http
POST /auth/register
POST /auth/login

POST /payments/{orderId}/approve
POST /payments/{orderId}/reject

GET /customers/{customerId}/orders
GET /admin/reports/sales
```

## Arquitetura

Estrutura inicial:

```txt
OrderFlow.Api
OrderFlow.Application
OrderFlow.Domain
OrderFlow.Infrastructure
OrderFlow.Tests
```

Estrutura planejada:

```txt
OrderFlow.Api
OrderFlow.Application
OrderFlow.Domain
OrderFlow.Infrastructure
OrderFlow.Contracts
OrderFlow.Worker
OrderFlow.Tests
```

## Roadmap de Desenvolvimento

### Fase 1 - Fundação

- Criar estrutura da solução
    
- Configurar PostgreSQL com Docker Compose
    
- Adicionar EF Core e migrations
    
- Adicionar Swagger/OpenAPI
    
- Adicionar endpoint de health check
    

### Fase 2 - Domínio Principal

- Implementar catálogo de produtos
    
- Implementar controle de estoque
    
- Implementar criação de pedidos
    
- Implementar reserva de estoque
    
- Implementar cancelamento de pedidos
    

### Fase 3 - Testes

- Adicionar testes unitários para regras de domínio
    
- Adicionar testes de integração com PostgreSQL
    
- Testar criação de pedido e reserva de estoque
    
- Testar cancelamento de pedido e liberação de estoque
    

### Fase 4 - Pagamentos

- Adicionar simulação de pagamento
    
- Tratar pagamentos aprovados e rejeitados
    
- Atualizar status do pedido com base no pagamento
    

### Fase 5 - Mensageria

- Adicionar RabbitMQ
    
- Adicionar MassTransit
    
- Publicar eventos de pedidos
    
- Adicionar worker em background
    
- Processar eventos de pagamento/pedido de forma assíncrona
    

### Fase 6 - Preocupações de Produção

- Adicionar logs estruturados
    
- Adicionar health checks
    
- Adicionar políticas de retry
    
- Adicionar idempotência
    
- Adicionar padrão outbox
    
- Adicionar observabilidade com .NET Aspire
    

## Executando Localmente

Arquivo de solução atual:

- `OrderFlow.slnx` (formato padrão mais atual do SDK .NET 10)

Requisitos:

- .NET SDK
    
- Docker
    
- Docker Compose
    

Subir dependências:

```bash
docker compose up -d
```

Executar a API:

```bash
dotnet run --project src/OrderFlow.Api
```

Executar testes:

```bash
dotnet test
```

## Notas de Aprendizado

Este projeto será desenvolvido em fases. A primeira versão prioriza um backend funcional e compreensível. Padrões mais avançados, como mensageria, outbox, idempotência e observabilidade, serão adicionados depois que o fluxo principal estiver estável.
