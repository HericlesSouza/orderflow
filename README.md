# OrderFlow

OrderFlow e um projeto de estudo backend focado na construcao de uma API realista de gerenciamento de pedidos usando .NET, PostgreSQL, Docker, testes automatizados e processamento assincrono.

A proposta do projeto e evoluir uma API simples para um backend com preocupacoes mais proximas de um sistema real, passando por criacao de pedidos, reserva de estoque, simulacao de pagamento, processamento em background, mensageria, idempotencia e observabilidade.

## Status do Projeto

Em desenvolvimento.

Documentacao oficial por objetivo:

- Apresentacao geral: `README.md`
- Escopo, regras e roadmap: `docs/PROJECT_SCOPE.md`
- Progresso real de execucao: `docs/PROJECT_PROGRESS.md`

Resumo rapido do momento:

- Fundacao principal concluida
- Endpoints atuais de Products concluidos: criacao, listagem, consulta por id e atualizacao de preco/status
- Estoque ja possui criacao inicial e consulta por produto via API
- Pedidos ainda estao concentrados no dominio, sem fluxo ponta a ponta em API/Application/Infrastructure
- Suite validada atualmente com 83 testes aprovados; os testes de integracao exigem Docker ativo no ambiente

Use `docs/PROJECT_PROGRESS.md` como fonte oficial para saber o que ja foi feito, o que esta em andamento, o que falta e eventuais bloqueios.

## Objetivos

Este projeto tem como objetivo praticar e demonstrar:

- Criacao de APIs REST com ASP.NET Core
- Persistencia com PostgreSQL e Entity Framework Core
- Separacao entre camadas de API, aplicacao, dominio e infraestrutura
- Regras de negocio envolvendo pedidos, estoque e pagamentos
- Testes de integracao usando banco de dados real
- Ambiente local com Docker
- Processamento assincrono com RabbitMQ e MassTransit
- Preocupacoes de producao como logs, health checks, retries e idempotencia

## Stack Tecnologica

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

### Catalogo de Produtos

- Criar produtos
- Listar produtos
- Buscar detalhes de um produto
- Atualizar preco e status de um produto

### Estoque

- Registrar estoque para produtos
- Aumentar quantidade disponivel
- Reservar estoque ao criar um pedido
- Liberar estoque reservado ao cancelar um pedido

### Pedidos

- Criar pedido
- Validar disponibilidade dos produtos
- Reservar estoque
- Cancelar pedido
- Acompanhar status do pedido

### Pagamentos

- Simular aprovacao de pagamento
- Simular rejeicao de pagamento
- Confirmar ou cancelar pedidos com base no resultado do pagamento

### Processamento em Background

Planejado:

- Publicar eventos de dominio e integracao
- Processar eventos de pedidos de forma assincrona
- Atualizar status de pedidos por meio de workers
- Aplicar retry e outbox pattern

## Roadmap Resumido

- Fase 1 - Fundacao
- Fase 2 - Produtos
- Fase 3 - Estoque
- Fase 4 - Pedidos
- Fase 5 - Cancelamento
- Fase 6 - Testes de Integracao
- Fase 7 - Pagamentos
- Fase 8 - Mensageria
- Fase 9 - Producao

Para o roadmap detalhado e o status real de cada fase, consulte `docs/PROJECT_SCOPE.md` e `docs/PROJECT_PROGRESS.md`.

## Executando Localmente

Arquivo de solucao atual:

- `OrderFlow.slnx`

Requisitos:

- .NET SDK
- Docker
- Docker Compose

Subir dependencias:

```bash
docker compose up -d
```

Executar a API:

```bash
dotnet run --project src/OrderFlow.Api
```

Documentacao interativa da API:

- Scalar UI: `http://localhost:5059/scalar`
- OpenAPI JSON: `http://localhost:5059/openapi/v1.json`

Executar testes:

```bash
dotnet test OrderFlow.slnx
```

## Notas de Aprendizado

Este projeto sera desenvolvido em fases. A primeira versao prioriza um backend funcional e compreensivel. Padroes mais avancados, como mensageria, outbox, idempotencia e observabilidade, serao adicionados depois que o fluxo principal estiver estavel.
