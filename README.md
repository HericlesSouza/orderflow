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
- Pedidos: criação, consulta por ID e cancelamento, com reserva/liberação atômica de estoque
- Erros de aplicação traduzidos para `ProblemDetails` com código estável
- Suíte validada com 127 testes aprovados usando PostgreSQL real nos testes de integração

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
- xUnit
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

### Funcionalidades planejadas

- Simular aprovação e rejeição de pagamentos
- Publicar e processar eventos de forma assíncrona
- Adicionar retries, outbox, idempotência e observabilidade

## Roadmap resumido

- Fase 1 - Fundação: concluída
- Fase 2 - Produtos: concluída
- Fase 3 - Estoque: concluída
- Fase 4 - Pedidos: concluída
- Fase 5 - Cancelamento: concluída
- Fase 6 - Testes de integração: concluída
- Fase 7 - Pagamentos: pendente
- Fase 8 - Mensageria: pendente
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
dotnet test OrderFlow.slnx
```

### 6. Verificar a formatação

```bash
dotnet format OrderFlow.slnx --verify-no-changes --no-restore
```

### Encerrar as dependências locais

```bash
docker compose down
```

## Notas de aprendizado

O projeto evolui em fases. O MVP prioriza um backend funcional, compreensível e testável. Mensageria, pagamentos, outbox, idempotência e observabilidade serão adicionados depois da estabilização do fluxo principal.
