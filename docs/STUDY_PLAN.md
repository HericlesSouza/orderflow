# Trilha direcionada para a Fase 8 — OrderFlow

Este roteiro prepara você para planejar a mensageria do OrderFlow sem refazer conteúdos que já domina. O foco é entender como uma mudança confiável no banco se transforma em um evento que outros componentes podem processar depois.

## Como usar

Siga os módulos em ordem. Em cada um, consulte os pontos indicados no código, responda às perguntas sem buscar a resposta e execute apenas os exercícios de análise. Não há alteração de código nesta trilha.

Você pode avançar quando atender ao critério de conclusão do módulo. O questionário em [PROJECT_QUIZ.md](PROJECT_QUIZ.md) continua sendo a revisão geral do projeto; este material aprofunda apenas os pontos necessários para a Fase 8.

## Módulo 1 — Contexto mínimo do fluxo atual

### Objetivo

Relacionar os fatos relevantes do domínio com os futuros eventos, sem revisar todo o ciclo de pedidos.

### Conceitos essenciais

- `Product.Price` é o preço atual de catálogo; `OrderItem.UnitPrice` é o preço histórico congelado na criação do pedido.
- Criar um pedido reserva estoque: aumenta `ReservedQuantity` e reduz `AvailableQuantity`; não reduz a quantidade física.
- Aprovar pagamento muda o pedido para `Confirmed`, mas preserva a reserva.
- Expedir um pedido confirmado muda o estado para `Dispatched`, reduz `Quantity` e `ReservedQuantity` pela mesma quantidade e mantém `AvailableQuantity` inalterada.
- `Dispatched` é o fato de negócio mais apropriado para o primeiro evento de estudo: ele representa uma transição concluída e sem retorno no modelo atual.

### Onde observar no projeto

- `src/OrderFlow.Domain/Entities/Order.cs`
- `src/OrderFlow.Domain/Entities/InventoryItem.cs`
- `src/OrderFlow.Application/Orders/Dispatch/DispatchOrderUseCase.cs`

### Autoavaliação

1. Por que `PaymentApproved` e `OrderDispatched` representam fatos diferentes?
2. Por que não seria adequado publicar `OrderDispatched` antes de a expedição ser persistida?
3. Qual informação do pedido deve estar disponível para um consumidor saber o que foi expedido?

### Exercício

Descreva em até cinco linhas o que mudou no pedido e no estoque depois de uma expedição bem-sucedida. Em seguida, liste os dados mínimos que um evento `OrderDispatched` deveria comunicar: identificador do pedido, momento da expedição e itens expedidos.

### Critério para avançar

Você consegue explicar por que a expedição, e não a aprovação de pagamento, é o primeiro ponto natural para publicar um evento de operação logística.

## Módulo 2 — Persistência, transação e consistência

### Objetivo

Entender onde e quando uma alteração se torna permanente, e por que a Outbox deve participar da mesma transação.

### Conceitos essenciais

| Componente | Responsabilidade no OrderFlow |
|---|---|
| `DbContext` | Rastreia entidades carregadas e transforma alterações em comandos SQL no `SaveChangesAsync`. |
| Repositório | Encapsula consultas e carregamento de agregados, inclusive consultas bloqueadas. |
| `IUnitOfWork` | Define a fronteira transacional do caso de uso: executa, persiste e confirma ou reverte o conjunto. |
| `FOR UPDATE` | Bloqueia linhas relevantes durante a transação para que operações concorrentes sejam serializadas. |

Na infraestrutura atual, `EfUnitOfWork.ExecuteInTransactionAsync` coordena a transação. Os casos de uso de pedidos carregam o pedido com `GetByIdForUpdateAsync`; na expedição, os estoques também são bloqueados antes da validação e da baixa.

Uma transação protege apenas aquilo que participa dela. A futura gravação do pedido expedido e do registro de Outbox precisa ocorrer antes do mesmo commit: ou ambos são confirmados, ou ambos são revertidos.

### Onde observar no projeto

- `src/OrderFlow.Infrastructure/Persistence/EfUnitOfWork.cs`
- `src/OrderFlow.Infrastructure/Persistence/Repositories/OrderRepository.cs`
- `src/OrderFlow.Infrastructure/Persistence/Repositories/InventoryItemRepository.cs`
- `tests/OrderFlow.UnitTests/Common/Fakes/ImmediateUnitOfWork.cs`

### Autoavaliação

1. Em que momento o estado alterado por `Order.Dispatch()` é persistido no banco?
2. O que acontece com pedido, estoque e futuro registro de Outbox se uma validação falhar antes do commit?
3. Por que `FOR UPDATE` e transação são complementares, e não sinônimos?

### Exercício

Compare dois cenários:

1. A expedição é confirmada no banco e, somente depois, o código tenta publicar diretamente no RabbitMQ.
2. A expedição e uma mensagem pendente na Outbox são confirmadas no mesmo commit; um processo posterior publica a mensagem.

Para cada um, descreva o que ocorre se o processo falhar entre a confirmação do banco e a publicação.

### Critério para avançar

Você consegue justificar por que uma chamada direta ao broker não fornece a mesma garantia de consistência que o registro transacional da Outbox.

### Referência

- [Transações no EF Core](https://learn.microsoft.com/en-us/ef/core/saving/transactions)

## Módulo 3 — Eventos de domínio e mensagens de integração

### Objetivo

Distinguir intenção, fato interno e comunicação externa para não acoplar o domínio ao broker.

### Conceitos essenciais

| Conceito | Exemplo | Significado |
|---|---|---|
| Comando | `DispatchOrder` | Pedido para executar uma ação; pode falhar. |
| Evento de domínio | `OrderDispatched` | Fato ocorrido após a regra de negócio ser concluída. |
| Mensagem de integração | `OrderDispatched` publicado | Contrato enviado a outro processo ou serviço. |

Na primeira versão, o mesmo fato pode originar uma mensagem de integração com dados simples e estáveis. A separação conceitual ainda é importante: o domínio não deve conhecer RabbitMQ, fila ou consumidor.

### Autoavaliação

1. Por que `OrderDispatched` não deve ser emitido para uma tentativa de expedição que retornou `409`?
2. Um evento significa que outro componente já o recebeu? Explique.
3. Qual mudança seria necessária caso, no futuro, a expedição parcial passasse a existir?

### Exercício

Escreva um contrato conceitual para `OrderDispatched`, sem código: nome do evento, `orderId`, data UTC da expedição e lista de itens com `productId` e quantidade. Explique por que não incluiria o preço atual do catálogo.

### Critério para avançar

Você consegue diferenciar um comando que pede expedição de um evento que registra que a expedição já ocorreu.

## Módulo 4 — Outbox pattern

### Objetivo

Compreender como registrar a intenção de publicar uma mensagem de maneira confiável.

### Conceitos essenciais

Uma Outbox é um armazenamento persistente de mensagens a publicar. Em uma implementação futura do OrderFlow, uma expedição bem-sucedida deverá:

1. Alterar o pedido e o estoque.
2. Criar o registro `OrderDispatched` na Outbox.
3. Persistir ambos na mesma transação.
4. Deixar um publicador em background buscar mensagens pendentes.
5. Publicar a mensagem no broker e marcar o registro como processado somente após o sucesso.

Isso reduz a falha conhecida como **dual write**: salvar no banco e publicar externamente são duas operações independentes; se uma funcionar e outra não, o sistema pode ficar inconsistente.

Outbox não garante que uma mensagem seja entregue exatamente uma vez. Na prática, o publicador deve poder repetir tentativas e o consumidor deve ser idempotente: receber a mesma mensagem duas vezes não pode produzir duas vezes o mesmo efeito de negócio.

### Autoavaliação

1. Que dados uma mensagem pendente da Outbox precisa ter além do conteúdo do evento?
2. Por que marcar uma mensagem como publicada antes de o broker confirmar o recebimento é arriscado?
3. Qual comportamento um consumidor deve ter ao receber novamente o mesmo `OrderDispatched`?

### Exercício

Desenhe, no papel ou em um diagrama, uma tabela conceitual de Outbox com: identificador, tipo do evento, payload, data de criação, data de processamento e contador de tentativas. Marque quais campos ajudam em reprocessamento e investigação de falhas.

### Critério para avançar

Você consegue explicar que a Outbox torna a intenção de publicar durável, mas ainda exige retentativas e idempotência.

## Módulo 5 — RabbitMQ e MassTransit

### Objetivo

Conhecer as responsabilidades do broker e da biblioteca de integração, sem adicioná-los prematuramente ao projeto.

### Conceitos essenciais

- **Producer/publicador:** envia uma mensagem para o broker.
- **Exchange:** recebe a publicação e a roteia conforme regras configuradas.
- **Fila:** mantém mensagens aguardando processamento.
- **Consumer/consumidor:** recebe e processa mensagens de uma fila.
- **Acknowledge:** confirmação de que o consumidor processou uma mensagem.
- **Retentativa:** nova tentativa após erro transitório; precisa de limite e observabilidade.
- **RabbitMQ:** broker responsável pelo transporte e roteamento de mensagens.
- **MassTransit:** biblioteca .NET que abstrai a configuração e o uso do transporte, consumidores, retentativas e convenções de mensagens.

RabbitMQ e MassTransit não substituem a Outbox. Eles entram depois que o evento, a persistência transacional e a política de reprocessamento estiverem claros.

### Autoavaliação

1. O que RabbitMQ faz que o `DbContext` não faz?
2. Se um consumidor falhar antes do acknowledge, por que ele pode receber a mesma mensagem de novo?
3. Por que MassTransit não elimina a necessidade de consumidores idempotentes?

### Exercício

Escolha um consumidor futuro de `OrderDispatched`, por exemplo “notificar expedição” ou “atualizar relatório de vendas”. Descreva a única responsabilidade dele e como ele evitaria repetir seu efeito caso receba o mesmo evento duas vezes.

### Critério para avançar

Você consegue posicionar cada componente: Outbox persiste a intenção, RabbitMQ transporta e MassTransit integra a aplicação .NET ao transporte.

### Referências

- [Tutoriais oficiais do RabbitMQ](https://www.rabbitmq.com/tutorials)
- [Configuração do transporte RabbitMQ no MassTransit](https://masstransit.io/documentation/configuration/transports/rabbitmq)
- [Testcontainers for .NET](https://dotnet.testcontainers.org/)

## Exercício final — Fluxo de ponta a ponta

Desenhe e explique o fluxo proposto para uma expedição bem-sucedida:

```text
POST /orders/{id}/dispatch
        |
        v
DispatchOrderUseCase + transação
        |
        +--> Order.Dispatch() e InventoryItem.Dispatch(...)
        |
        +--> cria registro pendente OrderDispatched na Outbox
        |
        v
commit único no PostgreSQL
        |
        v
publicador em background lê a Outbox
        |
        v
RabbitMQ roteia a mensagem para uma fila
        |
        v
consumer idempotente processa e confirma
```

Depois, descreva como o sistema deve reagir em três falhas:

1. A expedição não passa na validação de domínio.
2. O commit do banco falha.
3. O commit funciona, mas RabbitMQ está indisponível.

Resposta esperada: nos dois primeiros casos não deve haver mensagem pendente; no terceiro, a Outbox preserva a mensagem para uma tentativa posterior, sem reverter a expedição já confirmada.

## Checklist de prontidão para planejar a Fase 8

- [ ] Consigo indicar por que `OrderDispatched` é um evento e não um comando.
- [ ] Sei quais dados mínimos precisam compor a mensagem do evento.
- [ ] Entendo por que pedido/estoque e Outbox precisam do mesmo commit.
- [ ] Consigo explicar o risco de publicar diretamente no broker depois de salvar no banco.
- [ ] Sei que Outbox, por si só, não resolve duplicidade no consumidor.
- [ ] Consigo descrever uma estratégia de idempotência para um consumidor.
- [ ] Diferencio as responsabilidades de Outbox, RabbitMQ e MassTransit.
- [ ] Sei quais cenários de falha e concorrência deverão ganhar testes de integração na implementação.

Quando todos os itens estiverem marcados, o próximo passo é criar um plano técnico para a Fase 8: definir o contrato de `OrderDispatched`, o modelo da Outbox, o publicador em background, a integração com RabbitMQ e os testes de confiabilidade.
