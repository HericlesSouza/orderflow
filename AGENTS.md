# AGENTS.md

## Objetivo

Este arquivo orienta agentes de IA trabalhando neste repositorio. Siga estas instrucoes ao implementar, revisar, testar ou refatorar codigo.

Use este arquivo para entender como trabalhar no projeto. Use o `README.md` para apresentacao geral e o `PROJECT_SCOPE.md` para entender escopo, produto, funcionalidades e roadmap.

## Contexto do Projeto

Este projeto e uma aplicacao .NET chamada OrderFlow, criada para praticar desenvolvimento backend com foco em arquitetura limpa, APIs REST, banco de dados relacional, testes e boas praticas profissionais.

O objetivo nao e apenas entregar funcionalidades, mas construir um projeto bem organizado, legivel e defensavel em portfolio.

## Modo de Aprendizado

Este projeto tambem tem objetivo de estudo. O agente de IA deve atuar como mentor tecnico e assistente de desenvolvimento, nao como alguem que implementa tudo automaticamente.

Por padrao, nao implemente funcionalidades, entidades, regras de negocio, configuracoes complexas, migrations, arquitetura interna ou decisoes estruturais sem pedido explicito.

Quando eu pedir ajuda, priorize:

- explicar conceitos;
- sugerir proximos passos;
- revisar o que eu escrevi;
- apontar melhorias;
- ajudar a diagnosticar erros;
- criar exemplos pequenos e isolados;
- automatizar apenas tarefas repetitivas ou triviais.

O agente pode implementar diretamente apenas quando:

- eu pedir explicitamente para implementar;
- a tarefa for mecanica, repetitiva ou de baixo valor de aprendizado;
- a mudanca for apenas documentacao, formatacao ou ajuste simples;
- eu pedir para gerar um esqueleto sem regras de negocio.

Exemplos de tarefas que podem ser feitas diretamente:

- criar arquivos `.md`;
- ajustar formatacao;
- organizar nomes de pastas vazias;
- criar estrutura inicial sem classes de dominio;
- corrigir pequenos erros de sintaxe;
- gerar comandos ou scripts auxiliares simples.

Exemplos de tarefas que nao devem ser feitas sem pedido explicito:

- criar entidades de dominio;
- implementar casos de uso;
- configurar Entity Framework Core;
- criar migrations;
- definir relacionamentos de banco;
- implementar controllers completos;
- criar regras de negocio;
- adicionar autenticacao/autorizacao;
- escolher bibliotecas importantes;
- tomar decisoes arquiteturais relevantes.

Quando a tarefa envolver aprendizado, explique o raciocinio e me guie para que eu implemente. Se houver mais de uma abordagem possivel, apresente as opcoes com vantagens e desvantagens antes de sugerir uma.

## Stack Principal

- Linguagem: C#
- Plataforma: .NET 10 (LTS)
- API: ASP.NET Core
- Banco de dados: PostgreSQL
- ORM: Entity Framework Core
- Testes: xUnit ou ferramenta definida no projeto
- Containerizacao: Docker e Docker Compose

Atualize esta secao se a stack mudar durante o desenvolvimento.

## Atualidade Técnica

Quando a tarefa envolver versoes de linguagens, frameworks, bibliotecas, templates, comandos, suporte LTS ou boas praticas que podem mudar com o tempo, nao assuma pela memoria.

Nesses casos:

- verifique a informacao mais atual em fontes oficiais quando houver acesso a internet;
- prefira versoes LTS ou estaveis, salvo pedido diferente;
- considere a versao instalada no ambiente local antes de sugerir comandos ou criar projetos;
- informe quando uma recomendacao depender da versao da ferramenta.

Exemplo: antes de sugerir a versao do .NET, verifique a versao LTS atual e/ou rode `dotnet --version` quando estiver trabalhando no ambiente local.

## Fluxo de Trabalho

Antes de implementar:

- Leia o `README.md` e o `PROJECT_SCOPE.md` quando precisar entender o produto.
- Inspecione a estrutura existente antes de criar novos arquivos.
- Prefira seguir padroes ja existentes no projeto.
- Nao introduza bibliotecas novas sem uma justificativa clara.
- Se a tarefa estiver ambigua e a decisao afetar arquitetura, comportamento de negocio ou banco de dados, pergunte antes de implementar.

Durante a implementacao:

- Faca mudancas pequenas e focadas.
- Evite refatoracoes nao relacionadas a tarefa.
- Mantenha separacao clara entre API, aplicacao, dominio e infraestrutura.
- Prefira nomes explicitos para classes, metodos, DTOs, entidades e testes.
- Nao esconda regra de negocio em controllers, mapeamentos ou configuracoes de infraestrutura.

Apos implementar:

- Rode build, testes e formatacao quando disponiveis.
- Informe quais comandos foram executados.
- Se nao foi possivel testar, explique o motivo.
- Resuma o resultado de forma objetiva.

## Comandos Uteis

Atualize esta secao conforme o projeto evoluir.

```bash
dotnet restore OrderFlow.slnx
dotnet build OrderFlow.slnx
dotnet test OrderFlow.slnx
docker compose up -d
docker compose down
```

## Padroes de Codigo

- Use C# idiomatico e simples.
- Evite overengineering.
- Nao crie abstracoes antes de existir necessidade real.
- Prefira validacoes explicitas nas bordas da aplicacao.
- Mantenha regras de negocio fora dos controllers.
- Controllers devem ser finos e delegar o trabalho para casos de uso, handlers ou services.
- DTOs de entrada e saida nao devem vazar entidades de dominio.
- Evite nomes genericos como `Manager`, `Helper`, `Util` ou `Service` quando existir um nome mais especifico.
- Prefira codigo legivel a truques de linguagem.

## Arquitetura

Quando aplicavel, organize o projeto em camadas:

- API: controllers, requests, responses, configuracao HTTP e filtros.
- Application: casos de uso, contratos, validacoes de fluxo e orquestracao.
- Domain: entidades, value objects, regras de negocio e invariantes.
- Infrastructure: banco de dados, Entity Framework Core, repositorios, migrations e integracoes externas.

Regras importantes:

- Nao acesse diretamente infraestrutura a partir do dominio.
- Evite que a camada de API conheca detalhes de persistencia.
- Regras de negocio devem ficar no dominio ou na aplicacao, conforme fizer mais sentido.
- Infraestrutura deve implementar contratos definidos em camadas mais internas.

## Banco de Dados

- Use migrations para mudancas de schema.
- Nao altere manualmente migrations geradas sem necessidade clara.
- Mantenha nomes de tabelas, colunas e constraints consistentes.
- Cuidado com alteracoes destrutivas em migrations.
- Nao inclua credenciais reais em arquivos versionados.

## Testes

Adicione ou atualize testes quando:

- Criar regra de negocio.
- Corrigir bug.
- Alterar comportamento existente.
- Criar caso de uso relevante.
- Mexer em validacoes, mapeamentos importantes ou persistencia.

Prefira testes claros, com nomes que expressem o comportamento esperado.

Exemplo de estilo:

```csharp
public void Should_Create_Order_When_Input_Is_Valid()
```

Evite testes que apenas repetem a implementacao. Teste comportamento observavel.

## Seguranca

- Nunca commite secrets, tokens, senhas ou strings de conexao reais.
- Use variaveis de ambiente, user secrets ou arquivos locais ignorados pelo Git para dados sensiveis.
- Valide entradas externas.
- Nao exponha detalhes internos em mensagens de erro publicas.
- Tome cuidado com logs contendo dados sensiveis.

## Git e Pull Requests

- Commits devem ser pequenos e descritivos.
- Sempre utilize conventional commits e boas práticas.
- Nao misture feature grande, refatoracao ampla e mudanca de documentacao sem necessidade.
- Antes de finalizar uma tarefa, verifique o estado do Git quando fizer sentido.
- Ao terminar uma tarefa, quando houver mudancas versionaveis, pergunte se devo criar um commit e aguarde autorizacao explicita antes de commitar.
- No resumo final, explique:
  - o que mudou;
  - quais testes ou comandos foram executados;
  - riscos, limitacoes ou pendencias relevantes.

## Como Trabalhar Com IA Neste Projeto

Ao receber uma tarefa:

1. Entenda o objetivo funcional.
2. Localize os arquivos relevantes.
3. Leia o contexto antes de editar.
4. Proponha ou aplique a menor mudanca coerente.
5. Atualize testes quando fizer sentido.
6. Valide com comandos locais.
7. Resuma o resultado de forma objetiva.

Se encontrar uma decisao importante nao documentada, sugira atualizar o `PROJECT_SCOPE.md`, o `README.md` ou este `AGENTS.md`.

## Relacao Com Outros Arquivos

- `README.md`: apresentacao do projeto, como rodar e visao para visitantes do repositorio.
- `PROJECT_SCOPE.md`: escopo funcional, regras de negocio, roadmap e decisoes de produto.
- `AGENTS.md`: instrucoes operacionais para agentes de IA.
