# Questionário de domínio e arquitetura — OrderFlow

Use este material para revisar o projeto antes de avançar para mensageria e Outbox. Responda primeiro sem consultar o gabarito; ele fica ao final do documento.

O questionário reflete o projeto no estado atual: 15 endpoints, pagamentos simulados e expedição integral até o estado `Dispatched`.

## Parte 1 — Fundamentos e domínio

### 1. Entidades principais

Quais são as quatro entidades centrais do domínio atual e qual responsabilidade cada uma representa?

Resposta:

................................................................................

### 2. Preço histórico

Por que `OrderItem` armazena o preço unitário no momento da compra, em vez de consultar o preço atual de `Product` quando o pedido é exibido?

Resposta:

................................................................................

### 3. Quantidades de estoque

Explique a diferença entre `Quantity`, `ReservedQuantity` e `AvailableQuantity`. Qual é a fórmula de `AvailableQuantity`?

Resposta:

................................................................................

### 4. Reserva no pedido

Um produto possui `Quantity = 10` e `ReservedQuantity = 3`. Um novo pedido válido solicita 4 unidades. Quais serão os três valores do estoque depois da reserva? O pedido de 8 unidades seria aceito nesse mesmo estado?

Resposta:

................................................................................

## Parte 2 — Ciclo de vida do pedido

### 5. Estados e transições

Liste os cinco estados possíveis de um pedido e desenhe, em texto, as transições permitidas entre eles.

Resposta:

................................................................................

### 6. Aprovação de pagamento

Ao aprovar um pedido em `PendingPayment`, qual status é aplicado e o que acontece com `Quantity`, `ReservedQuantity` e `AvailableQuantity`?

Resposta:

................................................................................

### 7. Rejeição versus cancelamento

Compare a rejeição de pagamento com o cancelamento de um pedido pendente: quais são os efeitos iguais e qual a diferença de significado de negócio?

Resposta:

................................................................................

### 8. Expedição

Um pedido confirmado reservou 2 unidades de um estoque com `Quantity = 5`, `ReservedQuantity = 2` e `AvailableQuantity = 3`. Qual é o resultado após `POST /orders/{id}/dispatch`? Por que a aprovação e a expedição são rotas separadas?

Resposta:

................................................................................

## Parte 3 — Arquitetura e contratos

### 9. Responsabilidade das camadas

Descreva a responsabilidade das camadas API, Application, Domain e Infrastructure no OrderFlow. Dê um exemplo de algo que não deve ficar na API.

Resposta:

................................................................................

### 10. DTOs e entidades

Por que a API usa requests, responses e outputs em vez de receber ou devolver diretamente as entidades do domínio?

Resposta:

................................................................................

### 11. Resultados e erros

Qual problema `Result<T>` resolve nos casos de uso? Como `AppError` chega ao cliente HTTP como `ProblemDetails`?

Resposta:

................................................................................

### 12. Regras de domínio

Por que métodos como `Order.ConfirmPayment()`, `Order.RejectPayment()` e `Order.Dispatch()` pertencem ao domínio, e não aos endpoints?

Resposta:

................................................................................

## Parte 4 — Persistência e concorrência

### 13. Mudanças persistidas

Em um caso de uso transacional, em que momento uma alteração feita em uma entidade rastreada pelo EF Core é efetivamente salva no PostgreSQL?

Resposta:

................................................................................

### 14. Unidade de trabalho

Qual é o papel de `IUnitOfWork` e do `EfUnitOfWork`? O que acontece quando um caso de uso retorna `Result.Failure`?

Resposta:

................................................................................

### 15. Bloqueio pessimista

O que `FOR UPDATE` protege no fluxo de pedidos e estoque? Dê um exemplo de problema que poderia ocorrer sem esse bloqueio.

Resposta:

................................................................................

### 16. Migrations

Por que adicionar `DispatchedAt` exigiu uma migration? A API aplica migrations automaticamente ao iniciar? Qual comando atualiza o banco local?

Resposta:

................................................................................

## Parte 5 — Testes e evolução

### 17. Tipos de teste

Qual a diferença entre os testes unitários e os de integração do projeto? Cite uma situação que deve ser testada com PostgreSQL real.

Resposta:

................................................................................

### 18. Estrutura de testes

Para que servem builders de teste, `ImmediateUnitOfWork`, collections de testes de integração e Testcontainers?

Resposta:

................................................................................

### 19. Cenário concorrente

Duas requisições de expedição chegam simultaneamente para o mesmo pedido confirmado. Qual resultado é esperado e por que não ocorre baixa dupla no estoque?

Resposta:

................................................................................

### 20. Próxima fase

Por que o próximo passo recomendado é introduzir eventos e Outbox antes de integrar RabbitMQ e MassTransit diretamente? Que risco o Outbox busca reduzir?

Resposta:

................................................................................

---

## Gabarito comentado

### 1. Entidades principais

`Product` representa o catálogo e preço atual; `InventoryItem` controla as quantidades de um produto; `Order` representa o pedido e seu ciclo de vida; `OrderItem` representa cada produto comprado, com quantidade e preço histórico.

Erro comum: tratar `InventoryItem` como uma lista de pedidos ou usar o preço atual de `Product` como histórico do pedido.

### 2. Preço histórico

O pedido precisa continuar representando o acordo comercial realizado. Se o preço atual do catálogo mudar, pedidos antigos não podem mudar de total retroativamente.

Erro comum: supor que desativar um produto ou alterar seu preço deve alterar pedidos existentes.

### 3. Quantidades de estoque

`Quantity` é a quantidade física; `ReservedQuantity` está comprometida com pedidos; `AvailableQuantity` é a quantidade ainda vendável: `Quantity - ReservedQuantity`.

### 4. Reserva no pedido

Inicialmente, o disponível é 7. Reservar 4 resulta em `Quantity = 10`, `ReservedQuantity = 7` e `AvailableQuantity = 3`. Um pedido de 8 unidades não seria aceito porque excede o disponível antes da reserva.

### 5. Estados e transições

Estados: `PendingPayment`, `Confirmed`, `Cancelled`, `PaymentRejected` e `Dispatched`.

Transições: `PendingPayment → Confirmed`, `PendingPayment → Cancelled`, `PendingPayment → PaymentRejected` e `Confirmed → Dispatched`. Os demais estados são finais neste momento.

### 6. Aprovação de pagamento

O pedido passa a `Confirmed`. Nenhuma quantidade de estoque muda: a reserva é mantida até a expedição.

### 7. Rejeição versus cancelamento

Ambos terminam o pedido pendente e liberam suas reservas sem reduzir a quantidade física. A rejeição informa falha no pagamento; o cancelamento informa desistência ou interrupção do pedido pelo fluxo de cancelamento.

### 8. Expedição

O resultado é `Quantity = 3`, `ReservedQuantity = 0` e `AvailableQuantity = 3`; o pedido passa a `Dispatched` e recebe `DispatchedAt`. Pagamento aprovado não significa que o depósito já separou ou enviou o produto, portanto são eventos distintos.

### 9. Responsabilidade das camadas

API trata HTTP, valida requests e mapeia respostas. Application orquestra casos de uso e transações. Domain protege regras e transições de negócio. Infrastructure implementa banco, EF Core, repositórios e integrações. Uma regra como “somente pedido confirmado pode ser expedido” não deve ficar no endpoint.

### 10. DTOs e entidades

DTOs preservam o contrato HTTP, evitam vazamento de detalhes de persistência e permitem evoluir API e domínio independentemente. Entidades podem ter invariantes e navegações que não devem ser controladas por entrada externa.

### 11. Resultados e erros

`Result<T>` expressa sucesso ou falha esperada sem usar exceções para fluxo normal. O caso de uso cria um `AppError` com código e tipo; a API o converte para `ProblemDetails`, com status HTTP e código estável.

### 12. Regras de domínio

Esses métodos deixam as regras próximas ao estado que protegem e impedem que um segundo ponto de entrada, como mensageria futura, ignore as mesmas validações.

### 13. Mudanças persistidas

Depois de a operação retornar sucesso, `EfUnitOfWork` chama `SaveChangesAsync` e, em seguida, confirma a transação com `CommitAsync`. A entidade foi alterada antes, em memória e sob rastreamento do EF Core.

### 14. Unidade de trabalho

Ela delimita uma transação para todas as alterações do caso de uso. Em falha esperada, faz rollback, limpa o change tracker e devolve o `Result.Failure`; em exceção inesperada, também faz rollback e propaga a exceção.

### 15. Bloqueio pessimista

`FOR UPDATE` bloqueia as linhas consultadas até o fim da transação. Sem ele, duas requisições poderiam ler a mesma disponibilidade, reservar a mesma unidade ou expedir a mesma reserva duas vezes.

### 16. Migrations

O banco precisa de uma coluna persistida para `DispatchedAt`, por isso a alteração de modelo gera migration. A API não altera schema automaticamente. No banco local, execute:

```bash
dotnet tool run dotnet-ef database update --project src/OrderFlow.Infrastructure --startup-project src/OrderFlow.Api
```

### 17. Tipos de teste

Testes unitários exercitam regras e casos de uso isolados com doubles. Testes de integração exercitam HTTP, EF Core, migrations e PostgreSQL real. Concorrência com `FOR UPDATE`, rollback e persistência após commit exigem banco real.

### 18. Estrutura de testes

Builders criam dados válidos e legíveis; `ImmediateUnitOfWork` permite testar a orquestração sem banco; collections organizam isolamento e execução em série por feature; Testcontainers fornece PostgreSQL efêmero e reproduzível para integração.

### 19. Cenário concorrente

Uma expedição retorna `200` e a outra `409 order.cannot_dispatch`. A primeira bloqueia o pedido, muda o status e baixa o estoque; a segunda só continua após o commit, encontra `Dispatched` e não executa nova baixa.

### 20. Próxima fase

Eventos permitem reagir a pagamentos e expedições sem acoplar o fluxo principal a notificações ou integrações. O Outbox grava a intenção de publicar no mesmo commit do negócio, reduzindo o risco de o banco confirmar o pedido enquanto a mensagem é perdida antes de chegar ao broker.
