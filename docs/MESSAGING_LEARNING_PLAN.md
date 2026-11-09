# Mensageria em pequenos passos

## Ponto de partida

Voltamos ao commit anterior a integracao RabbitMQ: a Outbox e transacional, o `OutboxRepository` aceita eventos tipados com `Add<TEvent>` e o Worker publica por `LoggingEventPublisher`. RabbitMQ, Inbox, claim/lease, replay e metricas adicionais ainda nao fazem parte da implementacao.

A implementacao ampla foi descartada antes de ser commitada. Este roteiro substitui aquele plano de entrega unica e complementa a trilha conceitual de `STUDY_PLAN.md`.

## Combinado de aprendizagem

- Trabalhar em um unico bloco por vez. Este documento nao autoriza implementar os blocos seguintes.
- Antes de editar, explicar o problema, mostrar o fluxo e propor a menor mudanca; apresentar alternativas quando houver uma decisao relevante.
- Por padrao, o usuario escreve o codigo e o agente orienta, diagnostica e revisa. Implementacao pelo agente exige pedido explicito para aquele bloco.
- Encerrar cada bloco com uma demonstracao ou teste observavel e uma explicacao do que mudou. Aguardar o usuario escolher o proximo passo.
- Nao antecipar classes auxiliares, tabelas ou abstracoes para necessidades futuras. Extrair responsabilidades quando o codigo atual mostrar uma necessidade concreta.
- Validar versoes e comandos quando cada dependencia for introduzida. Nao adicionar pacote apenas porque um bloco futuro podera usa-lo.

## Blocos da publicacao

| Bloco | O que estudar e mudar | Como saber que terminou |
|---|---|---|
| 0. Ler o fluxo existente | Acompanhar expedicao, gravacao da Outbox, consulta pelo Worker e publicacao no log. Sem editar codigo. | Explicar quando a mensagem nasce, quem faz o commit e o que significa `ProcessedAt` hoje. |
| 1. Subir um broker | Adicionar somente um RabbitMQ local ao Compose, com volume e configuracao local de credenciais. Ainda sem alterar o Worker. | Abrir a interface de gerenciamento, identificar conexoes/exchanges/filas e verificar reinicio preservando o volume. |
| 2. Publicar uma mensagem | Adicionar o cliente oficial e um publicador pequeno que implemente `IEventPublisher`; configurar uma rota e uma fila duravel quorum no broker unico. Incluir `MessageId` estavel da Outbox, mensagem persistente, `mandatory` e publisher confirms. | Expedir um pedido e observar a mensagem na fila, sem consumidor. So concluir a Outbox apos confirmacao e sem return; testar rota inexistente. |
| 3. Derrubar e recuperar a conexao | Testar broker fora, timeout, reinicio do Worker e retorno do broker. Usar recuperacao do cliente e tratar conexao inicial/canal fechado apenas onde necessario. | A publicacao nao trava indefinidamente e uma falha nao conclui a Outbox; ao restaurar o broker, uma nova tentativa pode publicar com o mesmo ID. |
| 4. Espacar tentativas | Acrescentar backoff a publicacao para evitar tentativas agressivas; introduzir agenda persistida e sua migration somente neste bloco. | Uma falha agenda nova tentativa, um reinicio conserva a agenda e o sucesso encerra as tentativas. Indisponibilidade do broker nao descarta a mensagem apos tres falhas. |

Comecar com uma fila quorum em um unico broker permite estudar a publicacao, mas nao oferece tolerancia a perda de um no. O cluster entra depois; nao vamos apresentar o ambiente inicial como alta disponibilidade.

No bloco 2, configuracao e publicador sao suficientes como ponto de partida. Declaracao de topologia e fechamento de recursos podem comecar como metodos privados. Uma classe propria de conexao ou topologia sera avaliada apenas se tornar o codigo mais compreensivel. Metricas e CLI de replay ficam fora desses blocos.

## Evolucoes posteriores, escolhidas separadamente

| Necessidade demonstrada | Proximo incremento possivel |
|---|---|
| Continuar publicando apos perder um broker | Expandir para tres nos, verificar replicas quorum e testar queda de um no. |
| Executar mais de um Worker publicador | Implementar claim/lease, token de posse e retomada; introduzir as colunas e os testes de concorrencia nesse momento. |
| Uma acao real precisa receber `OrderDispatched` | Definir essa responsabilidade e criar um consumidor pequeno, com ACK depois de concluir a acao. |
| Reentrega pode repetir o efeito do consumidor | Escolher idempotencia apropriada: unicidade da propria operacao ou Inbox, se ela for necessaria. Explicar a escolha antes de criar tabela. |
| O consumidor falha e precisa de novas tentativas | Acrescentar retry com atraso e DLQ, esclarecendo a contagem de tentativas e protegendo as transferencias. |
| Precisamos recuperar e operar mensagens com falha | Adicionar replay controlado e, depois, metricas e alertas voltados a problemas observados. |

Falha de publicacao permanece na Outbox. Retry/DLQ do consumidor so passam a existir quando houver consumidor. Confirm perdido pode gerar duplicatas; confirmacao do broker nao significa que o consumidor terminou o trabalho.

## Verificacao e Git

Na restauracao, executar restore/build, testes existentes e verificacao de formatacao. Nos blocos futuros, acrescentar somente testes ligados ao comportamento introduzido; testes com broker real entram quando a integracao existir.

As migrations da implementacao descartada foram usadas apenas em bancos efemeros de testes. Nao executar uma reversao destrutiva no banco local nem remover volumes Docker como parte desta restauracao.

Cada incremento deve ficar pequeno e revisavel. Nenhum commit e automatico: solicitar autorizacao explicitamente, conforme `AGENTS.md`.
