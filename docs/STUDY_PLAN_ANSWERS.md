# Respostas — Trilha para a Fase 8

Este documento responde ao [plano de estudos](STUDY_PLAN.md) a partir do comportamento atual do OrderFlow e descreve a solução conceitual para a futura Fase 8. Não altera o desenho atual da aplicação.

## Módulo 1 — Contexto mínimo do fluxo atual

### Autoavaliação

**1. Por que `PaymentApproved` e `OrderDispatched` representam fatos diferentes?**

`PaymentApproved` informa que o pagamento foi aprovado e que o pedido pode passar para `Confirmed`; a reserva de estoque continua existindo. `OrderDispatched` informa que a mercadoria foi efetivamente expedida: o pedido passa para `Dispatched` e as quantidades física e reservada do estoque são baixadas. São transições distintas, em momentos distintos, e podem interessar a consumidores diferentes.

**2. Por que não seria adequado publicar `OrderDispatched` antes de a expedição ser persistida?**

Se a mensagem fosse publicada e a persistência falhasse ou fosse revertida, um consumidor receberia a informação de que um pedido foi expedido quando, no banco, ele não foi. A publicação deve representar um fato já confirmado. Na solução com Outbox, a alteração do pedido/estoque e o registro pendente da mensagem são gravados antes do mesmo commit.

**3. Qual informação do pedido deve estar disponível para um consumidor saber o que foi expedido?**

O identificador do pedido, o momento da expedição e os itens expedidos. Para cada item, o consumidor precisa ao menos do identificador do produto e da quantidade expedida.

### Exercício

Após uma expedição bem-sucedida, o pedido muda de `Confirmed` para `Dispatched` e recebe o instante de expedição. Para cada item do pedido, `Quantity` e `ReservedQuantity` do estoque diminuem pela quantidade expedida. Como ambas diminuem pelo mesmo valor, `AvailableQuantity` permanece inalterada. O evento deve comunicar `orderId`, `dispatchedAtUtc` e a lista de itens com `productId` e `quantity`.

## Módulo 2 — Persistência, transação e consistência

### Autoavaliação

**1. Em que momento o estado alterado por `Order.Dispatch()` é persistido no banco?**

`Order.Dispatch()` altera a entidade que está sendo rastreada pelo `DbContext`, mas ainda não grava nada no banco. No fluxo atual, a persistência ocorre em `dbContext.SaveChangesAsync`, dentro de `EfUnitOfWork.ExecuteInTransactionAsync`; ela só se torna permanente quando `CommitAsync` confirma a transação.

**2. O que acontece com pedido, estoque e futuro registro de Outbox se uma validação falhar antes do commit?**

O caso de uso retorna falha e a unidade de trabalho executa rollback da transação, além de limpar as entidades rastreadas. Portanto, nenhuma alteração no pedido ou estoque é confirmada. Quando a Outbox existir e fizer parte dessa mesma transação, também não existirá registro pendente do evento.

**3. Por que `FOR UPDATE` e transação são complementares, e não sinônimos?**

A transação define a unidade atômica: todas as alterações são confirmadas juntas ou revertidas juntas. `FOR UPDATE` bloqueia as linhas lidas para impedir que outra transação concorrente as altere enquanto a operação decide e grava suas mudanças. Uma transação sem os bloqueios adequados ainda pode sofrer decisões concorrentes incorretas; um bloqueio, por sua vez, não substitui o commit ou rollback atômico da transação.

### Exercício

**1. Banco confirmado e publicação direta no RabbitMQ depois**

Se o commit do banco terminar e o processo falhar antes de chamar o broker, o pedido ficará expedido e o estoque atualizado, mas a mensagem será perdida. Os componentes que dependiam dela não saberão da expedição. Esse é o problema de *dual write*.

**2. Expedição e mensagem pendente na Outbox no mesmo commit**

O commit cria ou confirma os três efeitos juntos: pedido, estoque e registro pendente da mensagem. Se o processo cair antes ou durante a publicação no broker, a mensagem continua persistida na Outbox. Um worker posterior a localizará e poderá tentar publicá-la novamente.

## Módulo 3 — Eventos de domínio e mensagens de integração

### Autoavaliação

**1. Por que `OrderDispatched` não deve ser emitido para uma tentativa de expedição que retornou `409`?**

O `409 Conflict` indica que a expedição não pôde ser concluída — por exemplo, porque o pedido não está confirmado ou a reserva de estoque está inconsistente. Como o fato `OrderDispatched` não ocorreu, emiti-lo produziria uma informação falsa para outros componentes.

**2. Um evento significa que outro componente já o recebeu?**

Não. O evento registra que um fato ocorreu; isso não garante sua publicação, entrega, processamento ou confirmação pelo consumidor. Ele pode estar pendente na Outbox, já estar no broker aguardando consumo, ou ser entregue novamente após uma falha antes do *acknowledge*.

**3. Qual mudança seria necessária caso, no futuro, a expedição parcial passasse a existir?**

O modelo precisaria representar que apenas parte do pedido foi enviada, inclusive um estado como `PartiallyDispatched` e as quantidades já expedidas por item. O contrato do evento deveria comunicar a expedição/remessa específica, com `orderId`, identificador da remessa, data UTC e somente os itens e quantidades enviados naquela operação. Um evento por `OrderItem` é possível, mas uma mensagem por remessa expressa melhor que vários itens foram expedidos juntos.

### Exercício

Contrato conceitual da mensagem de integração:

```text
Nome: OrderDispatched

{
  orderId: Guid,
  dispatchedAtUtc: DateTime,
  items: [
    {
      productId: Guid,
      quantity: int
    }
  ]
}
```

O preço atual do catálogo não deve entrar nesse contrato: ele pode ter mudado desde a criação do pedido e não é necessário para identificar o que saiu do estoque. O preço unitário histórico pertence ao item do pedido e só deveria integrar uma mensagem se existisse um consumidor com uma necessidade comercial ou financeira explícita.

## Módulo 4 — Outbox pattern

### Autoavaliação

**1. Que dados uma mensagem pendente da Outbox precisa ter além do conteúdo do evento?**

Ela precisa de um identificador único da mensagem, tipo do evento, data de criação, data de processamento (inicialmente nula) e contador de tentativas. O identificador permite deduplicação e rastreabilidade; o tipo permite desserializar e rotear corretamente; datas e tentativas apoiam reprocessamento, diagnóstico e monitoramento.

**2. Por que marcar uma mensagem como publicada antes de o broker confirmar o recebimento é arriscado?**

Se a marcação ocorrer primeiro e a publicação falhar, a mensagem parecerá concluída para o worker, embora o broker nunca a tenha recebido. Ela pode deixar de ser tentada novamente, causando perda de mensagem.

**3. Qual comportamento um consumidor deve ter ao receber novamente o mesmo `OrderDispatched`?**

Ele deve ser idempotente: reconhecer a mesma mensagem pelo seu identificador e não repetir o efeito de negócio. Por exemplo, pode registrar mensagens processadas com uma restrição única e ignorar uma segunda entrega já concluída.

### Exercício

Tabela conceitual de Outbox:

| Campo | Finalidade |
| --- | --- |
| `Id` | Identificador único da mensagem; também pode ser propagado como ID da mensagem ao consumidor. |
| `EventType` | Identifica o contrato a desserializar e publicar, como `OrderDispatched`. |
| `Payload` | Conteúdo serializado do evento. |
| `CreatedAtUtc` | Mostra quando a publicação se tornou pendente. |
| `ProcessedAtUtc` | Nulo enquanto pendente; preenchido somente depois de publicação confirmada. |
| `AttemptCount` | Permite observar e limitar retentativas. |

`ProcessedAtUtc` e `AttemptCount` são os campos mais diretamente úteis para localizar mensagens pendentes ou problemáticas e reprocessá-las. `Id` permite rastrear a mensagem de ponta a ponta.

## Módulo 5 — RabbitMQ e MassTransit

### Autoavaliação

**1. O que RabbitMQ faz que o `DbContext` não faz?**

O `DbContext` rastreia entidades e persiste alterações no banco relacional da aplicação. RabbitMQ recebe mensagens, aplica regras de roteamento, mantém filas e as entrega a consumidores. Ele é o transporte entre processos, não o mecanismo de persistência do agregado de domínio.

**2. Se um consumidor falhar antes do acknowledge, por que ele pode receber a mesma mensagem de novo?**

Sem o *acknowledge*, o broker não considera o processamento concluído. Ao ocorrer falha, desconexão ou expiração conforme a configuração, a mensagem pode ser devolvida à fila e entregue novamente, para que não seja perdida.

**3. Por que MassTransit não elimina a necessidade de consumidores idempotentes?**

MassTransit ajuda a configurar transporte, consumidores e retentativas, mas não transforma o processamento distribuído em “exatamente uma vez”. Um consumidor pode executar um efeito externo e falhar antes de confirmar a mensagem; numa nova entrega, o mesmo efeito seria repetido se ele não detectar a duplicidade.

### Exercício

Um futuro consumidor de `OrderDispatched` pode ter a única responsabilidade de criar a notificação de expedição para o cliente. Ele usa o ID da mensagem para registrar, em armazenamento persistente e com unicidade, que aquela notificação já foi criada. Se receber a mesma mensagem de novo, encontra o registro existente e encerra o processamento sem criar uma segunda notificação.

## Exercício final — Fluxo de ponta a ponta

```text
POST /orders/{id}/dispatch
        |
        v
DispatchOrderUseCase abre uma transação
        |
        +--> carrega pedido e estoques com bloqueio
        +--> valida regras de expedição
        +--> Order.Dispatch() e InventoryItem.Dispatch(...)
        +--> cria a mensagem pendente OrderDispatched na Outbox
        |
        v
SaveChanges + commit único no PostgreSQL
        |
        v
worker encontra mensagens pendentes na Outbox
        |
        v
publica OrderDispatched no RabbitMQ
        |
        v
RabbitMQ roteia para uma fila
        |
        v
consumer idempotente processa e faz acknowledge
```

**1. A expedição não passa na validação de domínio.** O caso de uso retorna falha, a transação é revertida e não existe alteração persistida nem mensagem pendente na Outbox.

**2. O commit do banco falha.** A transação é revertida. Pedido, estoque e registro da Outbox não são confirmados; portanto, não há evento a publicar.

**3. O commit funciona, mas RabbitMQ está indisponível.** A expedição permanece confirmada, assim como a mensagem pendente na Outbox. O worker registra a falha conforme a estratégia adotada e tenta publicar novamente depois; o pedido não deve ser revertido.

## Conclusão

Com esse fluxo, `OrderDispatched` representa um fato concluído, a Outbox torna a intenção de publicação durável e o consumidor idempotente torna entregas repetidas seguras. RabbitMQ e MassTransit entram como transporte e integração após essas garantias de consistência serem definidas.
