# Guia completo da API OrderFlow no Postman

Este guia permite executar manualmente, pelo Postman, todos os endpoints e fluxos funcionais disponíveis atualmente na API OrderFlow. Os exemplos formam uma sequência: respostas anteriores alimentam variáveis usadas nas requisições seguintes.

Ao final, você terá exercitado:

- saúde da API;
- criação, listagem, consulta e alteração de produtos;
- criação, consulta e aumento de estoque;
- criação e consulta de pedidos com reserva de estoque;
- preservação do preço histórico do pedido;
- cancelamento com liberação de estoque;
- expedição integral com baixa física do estoque reservado;
- validações e erros de negócio;
- rollback de reservas;
- proteção contra alterações concorrentes de estoque.

> A API não possui autenticação. Não é necessário configurar token ou header `Authorization`.

## Coleção pronta para importação

Uma coleção Postman v2.1 autocontida está disponível em [`postman/OrderFlow.postman_collection.json`](../postman/OrderFlow.postman_collection.json).

Para importá-la no Postman Desktop:

1. Abra o workspace local desejado.
2. Selecione **Import**.
3. Escolha o arquivo `postman/OrderFlow.postman_collection.json` ou arraste-o para a janela de importação.
4. Confirme que a coleção **OrderFlow Local - Complete API Flows** apareceu na barra lateral.

A coleção importada contém 99 requisições organizadas em nove pastas, incluindo preparação de dados, caminhos felizes, erros, concorrência, pagamentos e expedição. Ela usa variáveis da própria coleção e, por isso, não exige a criação de um environment separado. Execute as pastas na ordem numérica para rodar o fluxo completo.

As seções seguintes também ensinam a montar os mesmos fluxos manualmente usando um environment. Se você importar a coleção pronta, pode pular a criação manual descrita na seção 3.

Consulte a documentação oficial do Postman sobre [importação de coleções](https://learning.postman.com/docs/getting-started/importing-and-exporting/importing-data/) caso a opção **Import** tenha mudado de posição na interface.

## 1. Endpoints disponíveis

| Área | Método | Endpoint | Resultado principal |
|---|---|---|---|
| Saúde | `GET` | `/health` | Confirma que a API está respondendo |
| Produtos | `POST` | `/products` | Cria um produto |
| Produtos | `GET` | `/products` | Lista os produtos do mais recente para o mais antigo |
| Produtos | `GET` | `/products/{id}` | Consulta um produto |
| Produtos | `PATCH` | `/products/{id}/price` | Atualiza o preço |
| Produtos | `PATCH` | `/products/{id}/status` | Ativa ou desativa o produto |
| Estoque | `POST` | `/inventory/items` | Cria o estoque inicial de um produto |
| Estoque | `GET` | `/inventory/items/{productId}` | Consulta o estoque pelo produto |
| Estoque | `POST` | `/inventory/items/{productId}/increase` | Aumenta o estoque físico |
| Pedidos | `POST` | `/orders` | Cria um pedido e reserva estoque |
| Pedidos | `GET` | `/orders/{id}` | Consulta um pedido e seus itens |
| Pedidos | `POST` | `/orders/{id}/cancel` | Cancela o pedido e libera suas reservas |
| Pedidos | `POST` | `/orders/{id}/dispatch` | Expede pedido confirmado e baixa estoque físico |
| Pagamentos | `POST` | `/payments/{orderId}/approve` | Confirma o pedido mantendo suas reservas |
| Pagamentos | `POST` | `/payments/{orderId}/reject` | Rejeita o pedido e libera suas reservas |

Total: **15 endpoints**.

## 2. Preparação do ambiente local

Execute os comandos abaixo na raiz do repositório.

### 2.1. Restaurar ferramentas e dependências

```powershell
dotnet tool restore
dotnet restore OrderFlow.slnx
```

### 2.2. Subir o PostgreSQL

```powershell
docker compose up -d
```

### 2.3. Aplicar as migrations

```powershell
dotnet tool run dotnet-ef database update --project src/OrderFlow.Infrastructure --startup-project src/OrderFlow.Api
```

### 2.4. Executar a API

```powershell
dotnet run --project src/OrderFlow.Api
```

A API deve responder em:

```text
http://localhost:5059
```

Recursos auxiliares disponíveis em ambiente `Development`:

- Scalar: `http://localhost:5059/scalar`
- OpenAPI: `http://localhost:5059/openapi/v1.json`

As migrations são aplicadas explicitamente. Iniciar a API não cria nem atualiza o schema automaticamente.

### 2.5. Recomeçar com um banco vazio, se necessário

Os exemplos usam nomes únicos, mas IDs e registros criados continuam no banco entre execuções. Para apagar o volume local e recomeçar do zero:

```powershell
docker compose down -v
docker compose up -d
dotnet tool run dotnet-ef database update --project src/OrderFlow.Infrastructure --startup-project src/OrderFlow.Api
```

> `docker compose down -v` apaga todos os dados do PostgreSQL local do projeto. Use-o somente quando quiser realmente reiniciar o banco.

## 3. Configuração do Postman

### 3.1. Criar a coleção

Crie uma coleção chamada `OrderFlow Local`. Dentro dela, sugere-se criar as pastas abaixo:

```text
00 - Health
01 - Products
02 - Inventory
03 - Orders
04 - Error scenarios
05 - Concurrency
```

### 3.2. Criar o ambiente

Crie e selecione um ambiente chamado `OrderFlow Local`. Adicione inicialmente estas variáveis:

| Variável | Valor inicial | Finalidade |
|---|---|---|
| `baseUrl` | `http://localhost:5059` | Endereço da API |
| `notFoundId` | `11111111-1111-1111-1111-111111111111` | GUID válido que não deve existir no banco |
| `productId` | vazio | Produto principal |
| `secondProductId` | vazio | Segundo produto do pedido com vários itens |
| `inactiveProductId` | vazio | Produto usado no erro de produto inativo |
| `productWithoutInventoryId` | vazio | Produto usado no erro de estoque ausente |
| `zeroStockProductId` | vazio | Produto com estoque físico zero |
| `orderId` | vazio | Pedido principal com vários itens |
| `singleItemOrderId` | vazio | Pedido com um item, usado no cancelamento |
| `concurrentIncreaseProductId` | vazio | Cenário de dois aumentos concorrentes |
| `concurrentMixedProductId` | vazio | Cenário de aumento simultâneo com reserva |
| `lastUnitProductId` | vazio | Cenário de disputa pela última unidade |
| `concurrentWinnerOrderId` | vazio | Pedido vencedor da disputa pela última unidade |

O Postman referencia uma variável usando chaves duplas, por exemplo:

```text
{{baseUrl}}/products/{{productId}}
```

Os scripts deste guia usam `pm.environment.set(...)` para salvar IDs automaticamente. Essa é a API oficial do Postman para [alterar variáveis de ambiente por scripts](https://learning.postman.com/docs/use/send-requests/variables/environment-variables).

### 3.3. Headers

Nas requisições que possuem JSON no corpo, configure:

```http
Content-Type: application/json
Accept: application/json
```

Nas requisições sem corpo, o `Accept: application/json` é opcional.

### 3.4. Onde adicionar os scripts

Cada bloco denominado **Script pós-resposta** deve ser colocado na aba:

```text
Scripts > Post-response
```

Além de salvar variáveis, os scripts registram verificações na área `Test Results` do Postman.

## 4. Fluxo principal completo

Execute esta seção na ordem apresentada.

### 4.1. Verificar a saúde da API

**Requisição**

```http
GET {{baseUrl}}/health
```

**Script pós-resposta**

```javascript
pm.test("API respondeu com 200", () => {
    pm.response.to.have.status(200);
});

pm.test("Status da aplicação é Healthy", () => {
    pm.expect(pm.response.json().status).to.eql("Healthy");
});
```

**Resposta esperada — `200 OK`**

```json
{
  "status": "Healthy"
}
```

### 4.2. Criar o produto principal

**Requisição**

```http
POST {{baseUrl}}/products
Content-Type: application/json
```

```json
{
  "name": "Notebook Postman {{$guid}}",
  "description": "Produto principal do guia Postman",
  "price": 100.00
}
```

`{{$guid}}` é uma variável dinâmica do Postman. Ela evita conflitos de nome quando o guia for repetido.

**Script pós-resposta**

```javascript
pm.test("Produto criado", () => {
    pm.response.to.have.status(201);
});

const body = pm.response.json();
pm.environment.set("productId", body.id);

pm.test("Produto inicia ativo e com preço 100", () => {
    pm.expect(body.isActive).to.eql(true);
    pm.expect(body.price).to.eql(100);
});
```

**Resposta esperada — `201 Created`**

```json
{
  "id": "<guid salvo em productId>",
  "name": "Notebook Postman <guid>",
  "description": "Produto principal do guia Postman",
  "price": 100.00,
  "isActive": true,
  "createdAt": "<data UTC>",
  "updatedAt": "<data UTC>"
}
```

Efeito: o produto é persistido ativo. O header `Location` aponta para `/products/{id}`.

### 4.3. Criar o segundo produto

**Requisição**

```http
POST {{baseUrl}}/products
Content-Type: application/json
```

```json
{
  "name": "Mouse Postman {{$guid}}",
  "description": "Segundo produto do pedido",
  "price": 25.50
}
```

**Script pós-resposta**

```javascript
pm.test("Segundo produto criado", () => {
    pm.response.to.have.status(201);
});

pm.environment.set("secondProductId", pm.response.json().id);
```

### 4.4. Criar o produto que ficará temporariamente inativo

**Requisição**

```http
POST {{baseUrl}}/products
Content-Type: application/json
```

```json
{
  "name": "Produto inativo Postman {{$guid}}",
  "description": "Usado para validar pedidos com produto inativo",
  "price": 30.00
}
```

**Script pós-resposta**

```javascript
pm.test("Produto para cenário inativo criado", () => {
    pm.response.to.have.status(201);
});

pm.environment.set("inactiveProductId", pm.response.json().id);
```

### 4.5. Criar o produto que ficará sem estoque cadastrado

**Requisição**

```http
POST {{baseUrl}}/products
Content-Type: application/json
```

```json
{
  "name": "Produto sem estoque Postman {{$guid}}",
  "description": "Usado para validar ausência de InventoryItem",
  "price": 40.00
}
```

**Script pós-resposta**

```javascript
pm.test("Produto sem estoque criado", () => {
    pm.response.to.have.status(201);
});

pm.environment.set("productWithoutInventoryId", pm.response.json().id);
```

Não crie estoque para esse produto.

### 4.6. Criar o produto que terá estoque zero

**Requisição**

```http
POST {{baseUrl}}/products
Content-Type: application/json
```

```json
{
  "name": "Produto zerado Postman {{$guid}}",
  "description": "Usado para validar estoque insuficiente",
  "price": 50.00
}
```

**Script pós-resposta**

```javascript
pm.test("Produto para estoque zero criado", () => {
    pm.response.to.have.status(201);
});

pm.environment.set("zeroStockProductId", pm.response.json().id);
```

### 4.7. Listar produtos

**Requisição**

```http
GET {{baseUrl}}/products
```

**Script pós-resposta**

```javascript
pm.test("Lista de produtos retornada", () => {
    pm.response.to.have.status(200);
    pm.expect(pm.response.json()).to.be.an("array");
});

pm.test("Produtos estão do mais recente para o mais antigo", () => {
    const products = pm.response.json();
    const dates = products.map(product => new Date(product.createdAt).getTime());
    const sortedDates = [...dates].sort((a, b) => b - a);
    pm.expect(dates).to.eql(sortedDates);
});
```

**Resposta esperada — `200 OK`**

Um array de produtos. Em um banco vazio, o endpoint retorna `[]`. Após as criações deste guia, o produto criado mais recentemente deve aparecer primeiro.

### 4.8. Consultar o produto principal

**Requisição**

```http
GET {{baseUrl}}/products/{{productId}}
```

**Script pós-resposta**

```javascript
pm.test("Produto principal encontrado", () => {
    pm.response.to.have.status(200);
    pm.expect(pm.response.json().id).to.eql(pm.environment.get("productId"));
});
```

### 4.9. Atualizar o preço do produto principal

**Requisição**

```http
PATCH {{baseUrl}}/products/{{productId}}/price
Content-Type: application/json
```

```json
{
  "price": 120.00
}
```

**Script pós-resposta**

```javascript
pm.test("Preço atualizado", () => {
    pm.response.to.have.status(200);
    pm.expect(pm.response.json().price).to.eql(120);
});
```

Efeito: o preço atual passa a ser `120.00` e `updatedAt` é atualizado.

### 4.10. Desativar o produto do cenário de erro

**Requisição**

```http
PATCH {{baseUrl}}/products/{{inactiveProductId}}/status
Content-Type: application/json
```

```json
{
  "isActive": false
}
```

**Script pós-resposta**

```javascript
pm.test("Produto desativado", () => {
    pm.response.to.have.status(200);
    pm.expect(pm.response.json().isActive).to.eql(false);
});
```

Mantenha-o inativo até o cenário de pedido com produto inativo.

### 4.11. Criar o estoque do produto principal

**Requisição**

```http
POST {{baseUrl}}/inventory/items
Content-Type: application/json
```

```json
{
  "productId": "{{productId}}",
  "quantity": 10
}
```

**Script pós-resposta**

```javascript
pm.test("Estoque principal criado", () => {
    pm.response.to.have.status(201);
});

const body = pm.response.json();
pm.test("Estoque inicia sem reservas", () => {
    pm.expect(body.quantity).to.eql(10);
    pm.expect(body.reservedQuantity).to.eql(0);
    pm.expect(body.availableQuantity).to.eql(10);
});
```

**Resposta esperada — `201 Created`**

```json
{
  "id": "<guid do estoque>",
  "productId": "<productId>",
  "quantity": 10,
  "reservedQuantity": 0,
  "availableQuantity": 10,
  "createdAt": "<data UTC>",
  "updatedAt": "<data UTC>"
}
```

`quantity` representa a quantidade física total. `availableQuantity` é calculado por `quantity - reservedQuantity`.

### 4.12. Criar o estoque do segundo produto

**Requisição**

```http
POST {{baseUrl}}/inventory/items
Content-Type: application/json
```

```json
{
  "productId": "{{secondProductId}}",
  "quantity": 20
}
```

**Script pós-resposta**

```javascript
pm.test("Estoque do segundo produto criado", () => {
    pm.response.to.have.status(201);
    pm.expect(pm.response.json().availableQuantity).to.eql(20);
});
```

### 4.13. Criar um estoque com quantidade zero

Quantidade inicial zero é válida.

**Requisição**

```http
POST {{baseUrl}}/inventory/items
Content-Type: application/json
```

```json
{
  "productId": "{{zeroStockProductId}}",
  "quantity": 0
}
```

**Script pós-resposta**

```javascript
pm.test("Estoque zero criado", () => {
    pm.response.to.have.status(201);
    pm.expect(pm.response.json().quantity).to.eql(0);
    pm.expect(pm.response.json().availableQuantity).to.eql(0);
});
```

### 4.14. Consultar o estoque principal

O identificador da rota é o ID do produto, não o ID do registro de estoque.

**Requisição**

```http
GET {{baseUrl}}/inventory/items/{{productId}}
```

**Script pós-resposta**

```javascript
pm.test("Estoque encontrado pelo produto", () => {
    pm.response.to.have.status(200);
    pm.expect(pm.response.json().productId).to.eql(pm.environment.get("productId"));
});
```

### 4.15. Aumentar o estoque principal

**Requisição**

```http
POST {{baseUrl}}/inventory/items/{{productId}}/increase
Content-Type: application/json
```

```json
{
  "quantity": 5
}
```

**Script pós-resposta**

```javascript
pm.test("Estoque aumentado", () => {
    pm.response.to.have.status(200);
});

const body = pm.response.json();
pm.test("Quantidade física e disponível passaram para 15", () => {
    pm.expect(body.quantity).to.eql(15);
    pm.expect(body.reservedQuantity).to.eql(0);
    pm.expect(body.availableQuantity).to.eql(15);
});
```

### 4.16. Criar um pedido com um item

**Requisição**

```http
POST {{baseUrl}}/orders
Content-Type: application/json
```

```json
{
  "items": [
    {
      "productId": "{{secondProductId}}",
      "quantity": 1
    }
  ]
}
```

**Script pós-resposta**

```javascript
pm.test("Pedido com um item criado", () => {
    pm.response.to.have.status(201);
});

const body = pm.response.json();
pm.environment.set("singleItemOrderId", body.id);

pm.test("Pedido aguarda pagamento e totaliza 25,50", () => {
    pm.expect(body.status).to.eql("PendingPayment");
    pm.expect(body.totalAmount).to.eql(25.5);
    pm.expect(body.items).to.have.lengthOf(1);
});
```

Efeito: uma unidade do segundo produto é reservada.

### 4.17. Criar um pedido com vários itens

**Requisição**

```http
POST {{baseUrl}}/orders
Content-Type: application/json
```

```json
{
  "items": [
    {
      "productId": "{{productId}}",
      "quantity": 2
    },
    {
      "productId": "{{secondProductId}}",
      "quantity": 3
    }
  ]
}
```

**Script pós-resposta**

```javascript
pm.test("Pedido com vários itens criado", () => {
    pm.response.to.have.status(201);
});

const body = pm.response.json();
pm.environment.set("orderId", body.id);

pm.test("Total usa os preços atuais", () => {
    pm.expect(body.status).to.eql("PendingPayment");
    pm.expect(body.totalAmount).to.eql(316.5);
    pm.expect(body.items).to.have.lengthOf(2);
});
```

O total esperado é:

```text
(2 × 120,00) + (3 × 25,50) = 316,50
```

Pedido, itens e reservas são persistidos em uma única transação.

### 4.18. Confirmar as reservas de estoque

Consulte o estoque principal:

```http
GET {{baseUrl}}/inventory/items/{{productId}}
```

Resultado esperado:

```json
{
  "quantity": 15,
  "reservedQuantity": 2,
  "availableQuantity": 13
}
```

Consulte também o segundo produto:

```http
GET {{baseUrl}}/inventory/items/{{secondProductId}}
```

Resultado esperado após os pedidos de um e vários itens:

```json
{
  "quantity": 20,
  "reservedQuantity": 4,
  "availableQuantity": 16
}
```

Os exemplos mostram somente os campos relevantes; a resposta real também contém IDs e datas.

### 4.19. Consultar o pedido com vários itens

**Requisição**

```http
GET {{baseUrl}}/orders/{{orderId}}
```

**Script pós-resposta**

```javascript
pm.test("Pedido encontrado", () => {
    pm.response.to.have.status(200);
});

const body = pm.response.json();
pm.test("Pedido mantém os itens e total", () => {
    pm.expect(body.id).to.eql(pm.environment.get("orderId"));
    pm.expect(body.status).to.eql("PendingPayment");
    pm.expect(body.totalAmount).to.eql(316.5);
    pm.expect(body.items).to.have.lengthOf(2);
});
```

**Formato da resposta — `200 OK`**

```json
{
  "id": "<orderId>",
  "status": "PendingPayment",
  "totalAmount": 316.50,
  "createdAt": "<data UTC>",
  "cancelledAt": null,
  "items": [
    {
      "id": "<guid do item>",
      "productId": "<productId>",
      "quantity": 2,
      "unitPrice": 120.00,
      "totalPrice": 240.00
    },
    {
      "id": "<guid do item>",
      "productId": "<secondProductId>",
      "quantity": 3,
      "unitPrice": 25.50,
      "totalPrice": 76.50
    }
  ]
}
```

### 4.20. Confirmar a preservação do preço histórico

Primeiro, altere novamente o preço atual do produto principal:

```http
PATCH {{baseUrl}}/products/{{productId}}/price
Content-Type: application/json
```

```json
{
  "price": 150.00
}
```

Depois consulte novamente:

```http
GET {{baseUrl}}/orders/{{orderId}}
```

Use este script na consulta do pedido:

```javascript
pm.test("Preço histórico foi preservado", () => {
    pm.response.to.have.status(200);

    const body = pm.response.json();
    const productId = pm.environment.get("productId");
    const item = body.items.find(currentItem => currentItem.productId === productId);

    pm.expect(item).to.not.eql(undefined);
    pm.expect(item.unitPrice).to.eql(120);
    pm.expect(item.totalPrice).to.eql(240);
    pm.expect(body.totalAmount).to.eql(316.5);
});
```

O produto passa a custar `150.00`, mas o pedido continua com o preço unitário `120.00`, usado no momento da compra.

### 4.21. Cancelar o pedido com um item

**Requisição**

```http
POST {{baseUrl}}/orders/{{singleItemOrderId}}/cancel
```

Não envie body.

**Script pós-resposta**

```javascript
pm.test("Pedido cancelado", () => {
    pm.response.to.have.status(200);
});

const body = pm.response.json();
pm.test("Status e data de cancelamento foram atualizados", () => {
    pm.expect(body.status).to.eql("Cancelled");
    pm.expect(body.cancelledAt).to.not.eql(null);
});
```

Efeito: a única reserva desse pedido é liberada na mesma transação da mudança de status.

### 4.22. Confirmar a liberação do estoque

**Requisição**

```http
GET {{baseUrl}}/inventory/items/{{secondProductId}}
```

**Script pós-resposta**

```javascript
pm.test("Reserva do pedido cancelado foi liberada", () => {
    pm.response.to.have.status(200);

    const body = pm.response.json();
    pm.expect(body.quantity).to.eql(20);
    pm.expect(body.reservedQuantity).to.eql(3);
    pm.expect(body.availableQuantity).to.eql(17);
});
```

As três unidades do pedido principal continuam reservadas. Apenas a unidade pertencente ao pedido cancelado foi liberada.

## 5. Cenários de validação e erro

Os cenários abaixo são independentes, exceto quando uma variável da seção anterior é indicada.

### 5.1. Formatos de erro

Erros detectados pelos validadores HTTP usam `ValidationProblemDetails`. Exemplo:

```json
{
  "type": "https://tools.ietf.org/html/rfc9110#section-15.5.1",
  "title": "One or more validation errors occurred.",
  "status": 400,
  "errors": {
    "Price": [
      "'Price' must be greater than '0'."
    ]
  }
}
```

O texto exato criado pelo FluentValidation pode variar entre versões. Verifique principalmente o status e as chaves de `errors`.

Erros esperados da aplicação usam `ProblemDetails` com um código estável:

```json
{
  "type": "https://tools.ietf.org/html/rfc9110#section-15.5.5",
  "title": "Resource not found.",
  "status": 404,
  "detail": "Product not found.",
  "code": "product.not_found"
}
```

O mapeamento é:

| Tipo da aplicação | HTTP | Título |
|---|---:|---|
| `Validation` | `400` | `Validation failed.` |
| `NotFound` | `404` | `Resource not found.` |
| `Conflict` | `409` | `Conflict detected.` |

Exceções inesperadas retornam `500 Internal Server Error`:

```json
{
  "title": "An unexpected error occurred.",
  "status": 500,
  "detail": "<mensagem disponível apenas em Development>"
}
```

Não existe um endpoint destinado a provocar um erro `500`; esse formato documenta o tratamento global de falhas inesperadas.

### 5.2. Produto inválido

```http
POST {{baseUrl}}/products
Content-Type: application/json
```

```json
{
  "name": "",
  "description": "",
  "price": 0
}
```

**Esperado:** `400 Bad Request`, com as chaves `Name`, `Description` e `Price` em `errors`.

```javascript
pm.test("Produto inválido foi rejeitado", () => {
    pm.response.to.have.status(400);

    const errors = pm.response.json().errors;
    pm.expect(errors).to.have.property("Name");
    pm.expect(errors).to.have.property("Description");
    pm.expect(errors).to.have.property("Price");
});
```

O limite é de 200 caracteres para `name` e 1.000 para `description`. Campos vazios ou acima desses limites também são rejeitados.

### 5.3. Consultar produto inexistente

```http
GET {{baseUrl}}/products/{{notFoundId}}
```

**Esperado:** `404`, código `product.not_found`.

```javascript
pm.test("Produto inexistente retorna código estável", () => {
    pm.response.to.have.status(404);
    pm.expect(pm.response.json().code).to.eql("product.not_found");
});
```

### 5.4. Atualizar produto inexistente

Teste as duas requisições:

```http
PATCH {{baseUrl}}/products/{{notFoundId}}/price
Content-Type: application/json
```

```json
{
  "price": 10.00
}
```

```http
PATCH {{baseUrl}}/products/{{notFoundId}}/status
Content-Type: application/json
```

```json
{
  "isActive": false
}
```

**Esperado em ambas:** `404`, código `product.not_found`.

### 5.5. Atualizar preço com valor inválido

```http
PATCH {{baseUrl}}/products/{{productId}}/price
Content-Type: application/json
```

```json
{
  "price": 0
}
```

**Esperado:** `400`, com a chave `Price` em `errors`. Preços negativos também são inválidos.

### 5.6. Atualizar status sem informar valor

```http
PATCH {{baseUrl}}/products/{{productId}}/status
Content-Type: application/json
```

```json
{}
```

**Esperado:** `400`, com a chave `IsActive` em `errors`.

### 5.7. Criar estoque para produto inexistente

```http
POST {{baseUrl}}/inventory/items
Content-Type: application/json
```

```json
{
  "productId": "{{notFoundId}}",
  "quantity": 10
}
```

**Esperado:** `404`, código `product.not_found`.

### 5.8. Criar estoque duplicado

Repita a criação de estoque para o produto principal:

```http
POST {{baseUrl}}/inventory/items
Content-Type: application/json
```

```json
{
  "productId": "{{productId}}",
  "quantity": 10
}
```

**Esperado:** `409`, código `inventory_item.already_exists`.

```javascript
pm.test("Segundo estoque para o mesmo produto foi rejeitado", () => {
    pm.response.to.have.status(409);
    pm.expect(pm.response.json().code).to.eql("inventory_item.already_exists");
});
```

O banco também possui uma restrição única para proteger essa regra contra criações concorrentes.

### 5.9. Criar estoque com dados inválidos

```http
POST {{baseUrl}}/inventory/items
Content-Type: application/json
```

```json
{
  "productId": "00000000-0000-0000-0000-000000000000",
  "quantity": -1
}
```

**Esperado:** `400`, com as chaves `ProductId` e `Quantity` em `errors`.

### 5.10. Consultar estoque inexistente

```http
GET {{baseUrl}}/inventory/items/{{productWithoutInventoryId}}
```

**Esperado:** `404`, código `inventory_item.not_found`.

> A rota é `/inventory/items/{productId}`. O caminho legado `/inventory/{productId}` não existe e retorna `404`.

### 5.11. Aumentar estoque com quantidade inválida

```http
POST {{baseUrl}}/inventory/items/{{productId}}/increase
Content-Type: application/json
```

```json
{
  "quantity": 0
}
```

**Esperado:** `400`, com a chave `Quantity` em `errors`. Quantidades negativas também são rejeitadas.

### 5.12. Aumentar estoque inexistente

```http
POST {{baseUrl}}/inventory/items/{{productWithoutInventoryId}}/increase
Content-Type: application/json
```

```json
{
  "quantity": 5
}
```

**Esperado:** `404`, código `inventory_item.not_found`.

### 5.13. Criar pedido sem itens

```http
POST {{baseUrl}}/orders
Content-Type: application/json
```

```json
{
  "items": []
}
```

**Esperado:** `400`, com a chave `Items` em `errors`.

`items: null` também é rejeitado.

### 5.14. Criar pedido com produto repetido

```http
POST {{baseUrl}}/orders
Content-Type: application/json
```

```json
{
  "items": [
    {
      "productId": "{{productId}}",
      "quantity": 1
    },
    {
      "productId": "{{productId}}",
      "quantity": 2
    }
  ]
}
```

**Esperado:** `400`, com a chave `Items` e mensagem indicando que cada produto pode aparecer somente uma vez.

### 5.15. Criar pedido com item inválido

```http
POST {{baseUrl}}/orders
Content-Type: application/json
```

```json
{
  "items": [
    {
      "productId": "00000000-0000-0000-0000-000000000000",
      "quantity": 0
    }
  ]
}
```

**Esperado:** `400`, com erros referentes a `Items[0].ProductId` e `Items[0].Quantity`.

### 5.16. Criar pedido com produto inexistente

```http
POST {{baseUrl}}/orders
Content-Type: application/json
```

```json
{
  "items": [
    {
      "productId": "{{notFoundId}}",
      "quantity": 1
    }
  ]
}
```

**Esperado:** `404`, código `order.product_not_found`.

### 5.17. Criar pedido com produto inativo

```http
POST {{baseUrl}}/orders
Content-Type: application/json
```

```json
{
  "items": [
    {
      "productId": "{{inactiveProductId}}",
      "quantity": 1
    }
  ]
}
```

**Esperado:** `409`, código `order.product_inactive`.

Depois desse teste, valide a reativação do produto:

```http
PATCH {{baseUrl}}/products/{{inactiveProductId}}/status
Content-Type: application/json
```

```json
{
  "isActive": true
}
```

**Esperado:** `200`, com `isActive: true`.

### 5.18. Criar pedido para produto sem estoque cadastrado

```http
POST {{baseUrl}}/orders
Content-Type: application/json
```

```json
{
  "items": [
    {
      "productId": "{{productWithoutInventoryId}}",
      "quantity": 1
    }
  ]
}
```

**Esperado:** `409`, código `order.inventory_not_found`.

### 5.19. Criar pedido com estoque insuficiente

```http
POST {{baseUrl}}/orders
Content-Type: application/json
```

```json
{
  "items": [
    {
      "productId": "{{zeroStockProductId}}",
      "quantity": 1
    }
  ]
}
```

**Esperado:** `409`, código `order.insufficient_inventory`.

### 5.20. Confirmar rollback quando um dos itens não possui estoque

Antes do teste, consulte e anote `reservedQuantity` do produto principal:

```http
GET {{baseUrl}}/inventory/items/{{productId}}
```

Depois tente criar:

```http
POST {{baseUrl}}/orders
Content-Type: application/json
```

```json
{
  "items": [
    {
      "productId": "{{productId}}",
      "quantity": 1
    },
    {
      "productId": "{{zeroStockProductId}}",
      "quantity": 1
    }
  ]
}
```

**Esperado:** `409`, código `order.insufficient_inventory`.

Consulte novamente o estoque principal. `reservedQuantity` deve permanecer inalterado: nenhuma reserva parcial é persistida quando qualquer item falha.

### 5.21. Consultar pedido inexistente

```http
GET {{baseUrl}}/orders/{{notFoundId}}
```

**Esperado:** `404`, código `order.not_found`.

### 5.22. Cancelar pedido inexistente

```http
POST {{baseUrl}}/orders/{{notFoundId}}/cancel
```

**Esperado:** `404`, código `order.not_found`.

### 5.23. Cancelar novamente um pedido cancelado

Repita:

```http
POST {{baseUrl}}/orders/{{singleItemOrderId}}/cancel
```

**Esperado:** `409`, código `order.cannot_cancel`.

```javascript
pm.test("Cancelamento repetido foi rejeitado", () => {
    pm.response.to.have.status(409);
    pm.expect(pm.response.json().code).to.eql("order.cannot_cancel");
});
```

Consulte o estoque do segundo produto e confirme que a reserva não foi liberada uma segunda vez.

### 5.24. GUID inválido na rota

Os parâmetros de rota usam a restrição `guid`. Por exemplo:

```http
GET {{baseUrl}}/products/id-invalido
```

**Esperado:** `404`, porque nenhuma rota corresponde ao valor informado. Esse caso é diferente de um GUID válido cujo recurso não existe e, portanto, não retorna o código `product.not_found`.

## 6. Testes avançados de concorrência

Esses testes usam [`pm.sendRequest`](https://learning.postman.com/latest-v-12/docs/tests-and-scripts/write-scripts/postman-sandbox-reference/pm-send-request) para enviar requisições assíncronas pelo sandbox do Postman.

Crie as requisições de preparação e execução em uma pasta separada. Não reutilize os produtos do fluxo principal.

Abra o console do Postman para facilitar o diagnóstico:

```text
View > Show Postman Console
```

### 6.1. Dois aumentos simultâneos

#### Preparação A — criar produto

```http
POST {{baseUrl}}/products
Content-Type: application/json
```

```json
{
  "name": "Concorrência aumentos {{$guid}}",
  "description": "Dois aumentos simultâneos",
  "price": 10.00
}
```

**Script pós-resposta**

```javascript
pm.test("Produto concorrente criado", () => pm.response.to.have.status(201));
pm.environment.set("concurrentIncreaseProductId", pm.response.json().id);
```

#### Preparação B — criar estoque 10

```http
POST {{baseUrl}}/inventory/items
Content-Type: application/json
```

```json
{
  "productId": "{{concurrentIncreaseProductId}}",
  "quantity": 10
}
```

#### Execução — disparar aumentos de 5 e 7

Crie uma requisição `GET {{baseUrl}}/health` chamada `Disparar dois aumentos`. Adicione este script pós-resposta:

```javascript
const baseUrl = pm.environment.get("baseUrl");
const productId = pm.environment.get("concurrentIncreaseProductId");

const increase = quantity => pm.sendRequest({
    url: `${baseUrl}/inventory/items/${productId}/increase`,
    method: "POST",
    header: {
        "Content-Type": "application/json",
        "Accept": "application/json"
    },
    body: {
        mode: "raw",
        raw: JSON.stringify({ quantity })
    }
});

const responses = await Promise.all([
    increase(5),
    increase(7)
]);

pm.test("Os dois aumentos foram aceitos", () => {
    pm.expect(responses.map(response => response.code)).to.eql([200, 200]);
});

console.log("Respostas dos aumentos concorrentes", responses.map(response => response.json()));
```

Depois consulte:

```http
GET {{baseUrl}}/inventory/items/{{concurrentIncreaseProductId}}
```

**Resultado final esperado:**

```json
{
  "quantity": 22,
  "reservedQuantity": 0,
  "availableQuantity": 22
}
```

Os dois aumentos são preservados porque cada atualização bloqueia a linha de estoque dentro de uma transação.

### 6.2. Aumento simultâneo com reserva de pedido

#### Preparação A — criar produto

```http
POST {{baseUrl}}/products
Content-Type: application/json
```

```json
{
  "name": "Concorrência mista {{$guid}}",
  "description": "Aumento simultâneo com reserva",
  "price": 20.00
}
```

**Script pós-resposta**

```javascript
pm.test("Produto concorrente misto criado", () => pm.response.to.have.status(201));
pm.environment.set("concurrentMixedProductId", pm.response.json().id);
```

#### Preparação B — criar estoque 10

```http
POST {{baseUrl}}/inventory/items
Content-Type: application/json
```

```json
{
  "productId": "{{concurrentMixedProductId}}",
  "quantity": 10
}
```

#### Execução — aumentar 5 e reservar 3

Crie uma requisição `GET {{baseUrl}}/health` chamada `Disparar aumento e reserva`. Adicione este script pós-resposta:

```javascript
const baseUrl = pm.environment.get("baseUrl");
const productId = pm.environment.get("concurrentMixedProductId");
const headers = {
    "Content-Type": "application/json",
    "Accept": "application/json"
};

const increaseRequest = pm.sendRequest({
    url: `${baseUrl}/inventory/items/${productId}/increase`,
    method: "POST",
    header: headers,
    body: {
        mode: "raw",
        raw: JSON.stringify({ quantity: 5 })
    }
});

const orderRequest = pm.sendRequest({
    url: `${baseUrl}/orders`,
    method: "POST",
    header: headers,
    body: {
        mode: "raw",
        raw: JSON.stringify({
            items: [{ productId, quantity: 3 }]
        })
    }
});

const [increaseResponse, orderResponse] = await Promise.all([
    increaseRequest,
    orderRequest
]);

pm.test("Aumento e pedido foram aceitos", () => {
    pm.expect(increaseResponse.code).to.eql(200);
    pm.expect(orderResponse.code).to.eql(201);
});

console.log("Aumento concorrente", increaseResponse.json());
console.log("Pedido concorrente", orderResponse.json());
```

Depois consulte:

```http
GET {{baseUrl}}/inventory/items/{{concurrentMixedProductId}}
```

**Resultado final esperado:**

```json
{
  "quantity": 15,
  "reservedQuantity": 3,
  "availableQuantity": 12
}
```

Nenhuma atualização deve ser perdida, independentemente de qual requisição adquirir o bloqueio primeiro.

### 6.3. Dois pedidos disputando a última unidade

#### Preparação A — criar produto

```http
POST {{baseUrl}}/products
Content-Type: application/json
```

```json
{
  "name": "Última unidade {{$guid}}",
  "description": "Dois pedidos concorrentes para uma unidade",
  "price": 30.00
}
```

**Script pós-resposta**

```javascript
pm.test("Produto da última unidade criado", () => pm.response.to.have.status(201));
pm.environment.set("lastUnitProductId", pm.response.json().id);
```

#### Preparação B — criar estoque 1

```http
POST {{baseUrl}}/inventory/items
Content-Type: application/json
```

```json
{
  "productId": "{{lastUnitProductId}}",
  "quantity": 1
}
```

#### Execução — disparar os dois pedidos

Crie uma requisição `GET {{baseUrl}}/health` chamada `Disputar última unidade`. Adicione este script pós-resposta:

```javascript
const baseUrl = pm.environment.get("baseUrl");
const productId = pm.environment.get("lastUnitProductId");

const createOrder = () => pm.sendRequest({
    url: `${baseUrl}/orders`,
    method: "POST",
    header: {
        "Content-Type": "application/json",
        "Accept": "application/json"
    },
    body: {
        mode: "raw",
        raw: JSON.stringify({
            items: [{ productId, quantity: 1 }]
        })
    }
});

const responses = await Promise.all([
    createOrder(),
    createOrder()
]);

const statusCodes = responses
    .map(response => response.code)
    .sort((left, right) => left - right);

pm.test("Apenas um pedido venceu a última unidade", () => {
    pm.expect(statusCodes).to.eql([201, 409]);
});

const winner = responses.find(response => response.code === 201);
const loser = responses.find(response => response.code === 409);

if (winner) {
    pm.environment.set("concurrentWinnerOrderId", winner.json().id);
}

pm.test("Pedido perdedor recebeu estoque insuficiente", () => {
    pm.expect(loser).to.not.eql(undefined);
    pm.expect(loser.json().code).to.eql("order.insufficient_inventory");
});

console.log("Resultados da disputa", responses.map(response => ({
    status: response.code,
    body: response.json()
})));
```

Depois consulte:

```http
GET {{baseUrl}}/inventory/items/{{lastUnitProductId}}
```

**Resultado final esperado:**

```json
{
  "quantity": 1,
  "reservedQuantity": 1,
  "availableQuantity": 0
}
```

Também é possível consultar o pedido vencedor:

```http
GET {{baseUrl}}/orders/{{concurrentWinnerOrderId}}
```

O bloqueio pessimista da linha de estoque impede que os dois pedidos reservem a mesma unidade.

## 7. Estados de pedido e limitações atuais

O domínio possui cinco estados:

| Estado | Significado |
|---|---|
| `PendingPayment` | Pedido criado e aguardando pagamento |
| `Confirmed` | Pagamento aprovado |
| `Cancelled` | Pedido cancelado |
| `PaymentRejected` | Pagamento rejeitado |
| `Dispatched` | Pedido expedido integralmente |

Atualmente, todo pedido nasce como `PendingPayment`. Cancelamento, aprovação, rejeição e expedição estão expostos pela API. Aprovação mantém as reservas até a expedição; rejeição libera a reserva sem reduzir a quantidade física. Expedição aceita somente pedido confirmado e reduz a quantidade física e a reserva pela mesma quantidade.

Também não existe API para provocar intencionalmente inconsistência entre pedido e reserva. A proteção contra estoque inconsistente no cancelamento é coberta pelos testes automatizados, não por um fluxo público.

## 8. Tabela de códigos estáveis

| Código | HTTP | Quando ocorre |
|---|---:|---|
| `product.not_found` | `404` | Consulta ou alteração de produto inexistente; criação de estoque para produto inexistente |
| `product.invalid` | `400` | Proteção da aplicação contra domínio de produto inválido; normalmente antecipada pelo validador HTTP |
| `inventory_item.not_found` | `404` | Consulta ou aumento sem registro de estoque |
| `inventory_item.already_exists` | `409` | Tentativa de criar segundo estoque para o mesmo produto |
| `inventory_item.invalid` | `400` | Proteção da aplicação contra estoque inválido; normalmente antecipada pelo validador HTTP |
| `order.not_found` | `404` | Consulta ou cancelamento de pedido inexistente |
| `order.product_not_found` | `404` | Pedido contém produto inexistente |
| `order.product_inactive` | `409` | Pedido contém produto inativo |
| `order.inventory_not_found` | `409` | Produto do pedido não possui estoque cadastrado |
| `order.insufficient_inventory` | `409` | Quantidade disponível não atende ao pedido |
| `order.invalid` | `400` | Proteção da aplicação contra pedido inválido; normalmente antecipada pelo validador HTTP |
| `order.cannot_cancel` | `409` | Pedido já cancelado ou em estado que não permite cancelamento |
| `order.cannot_dispatch` | `409` | Pedido não confirmado ou já expedido |
| `payment.order_not_pending` | `409` | Aprovação ou rejeição de pedido confirmado, cancelado ou rejeitado |
| `order.inventory_inconsistent` | `409` | Reserva necessária ao cancelamento, rejeição ou expedição está ausente ou insuficiente |

## 9. Checklist final

Use esta lista para confirmar que todo o comportamento público atual foi exercitado:

- [ ] `GET /health`
- [ ] `POST /products`
- [ ] `GET /products`
- [ ] `GET /products/{id}`
- [ ] `PATCH /products/{id}/price`
- [ ] `PATCH /products/{id}/status`, desativando e reativando
- [ ] `POST /inventory/items`, inclusive com quantidade inicial zero
- [ ] `GET /inventory/items/{productId}`
- [ ] `POST /inventory/items/{productId}/increase`
- [ ] `POST /orders` com um item
- [ ] `POST /orders` com vários itens
- [ ] `GET /orders/{id}`
- [ ] Preservação do preço histórico após alteração do produto
- [ ] Reserva de estoque na criação de pedido
- [ ] `POST /orders/{id}/cancel`
- [ ] Liberação de estoque no cancelamento
- [ ] Validações de payload
- [ ] Recursos inexistentes
- [ ] Produto inativo
- [ ] Estoque ausente e insuficiente
- [ ] Estoque duplicado
- [ ] Rollback sem reserva parcial
- [ ] Cancelamento repetido
- [ ] Dois aumentos concorrentes
- [ ] Aumento concorrente com reserva
- [ ] Disputa concorrente pela última unidade
- [ ] Aprovação de pagamento e consulta do pedido confirmado
- [ ] Estoque preservado após aprovação
- [ ] Aprovação repetida, pedido inexistente e aprovação após cancelamento
- [ ] Cancelamento após aprovação
- [ ] Rejeição de pagamento, consulta do pedido rejeitado e liberação de estoque
- [ ] Rejeição repetida, pedido inexistente, aprovação e cancelamento após rejeição
- [ ] Expedição, data `dispatchedAt` e baixa física do estoque reservado
- [ ] Expedição repetida, pedido inexistente e transições inválidas após expedição

Ao concluir a lista, todos os endpoints e comportamentos funcionais expostos pela versão atual da aplicação terão sido percorridos.

## 10. Aprovação de pagamento simulado

Na coleção pronta, execute a pasta **06 - Payment approval** desde a primeira requisição. Ela cria um produto de preço 100, estoque 5 e pedido de 2 unidades, usando as variáveis de coleção `paymentProductId`, `paymentOrderId` e `cancelledPaymentOrderId`. Os scripts capturam os IDs automaticamente.

Selecione **No environment** para usar os valores da coleção sem sobreposição dos IDs de um environment anterior. Cada execução completa da pasta cria novos dados. Reexecutar somente uma mutação pode mudar os resultados esperados das próximas requisições.

### 10.1. Aprovar e consultar

Com o pedido pendente criado pelas três primeiras requisições da pasta:

```http
POST {{baseUrl}}/payments/{{paymentOrderId}}/approve
Accept: application/json
```

Não envie corpo. A resposta é `200 OK`, com o mesmo formato de `OrderResponse` da consulta de pedidos: status `Confirmed`, total 200, preço unitário 100 e `cancelledAt: null`.

Script pós-resposta (já incluído na coleção):

```javascript
pm.test("Pedido confirmado", () => {
    pm.response.to.have.status(200);
    const body = pm.response.json();
    pm.expect(body.status).to.eql("Confirmed");
    pm.expect(body.totalAmount).to.eql(200);
    pm.expect(body.items[0].unitPrice).to.eql(100);
    pm.expect(body.cancelledAt).to.eql(null);
});
```

Consulte `GET {{baseUrl}}/orders/{{paymentOrderId}}` para confirmar a persistência. Consulte `GET {{baseUrl}}/inventory/items/{{paymentProductId}}`: as quantidades continuam sendo físico 5, reservado 2 e disponível 3. A aprovação não revalida o preço ou a ativação atual do produto; usa o pedido já criado.

### 10.2. Erros e transições inválidas

Todas as requisições abaixo estão prontas na mesma pasta, com testes de status e código de erro:

| Cenário | Requisição sem corpo | Resultado |
|---|---|---|
| Aprovação repetida | `POST /payments/{{paymentOrderId}}/approve` | `409 payment.order_not_pending` |
| Pedido inexistente | `POST /payments/{{notFoundId}}/approve` | `404 order.not_found` |
| Cancelar pedido confirmado | `POST /orders/{{paymentOrderId}}/cancel` | `409 order.cannot_cancel` |
| Aprovar pedido cancelado | `POST /payments/{{cancelledPaymentOrderId}}/approve` | `409 payment.order_not_pending` |

Para o último cenário, as requisições 10 e 11 criam outro pedido de uma unidade do produto exclusivo e o cancelam, salvando seu ID em `cancelledPaymentOrderId`. A consulta final verifica que as duas unidades do primeiro pedido continuam reservadas.

Uma segunda aprovação retorna conflito inclusive em disputa simultânea. Aprovação concorrente com cancelamento permite somente uma transição: se a aprovação vencer, as reservas permanecem; se o cancelamento vencer, as reservas daquele pedido são liberadas. Esses cenários são verificados pela suíte de integração com PostgreSQL.

## 11. Rejeição de pagamento simulada

Na coleção pronta, execute a pasta **07 - Payment rejection** desde a primeira requisição. Ela cria produto, estoque e pedido exclusivos, usando `rejectionProductId` e `rejectionOrderId`.

```http
POST {{baseUrl}}/payments/{{rejectionOrderId}}/reject
Accept: application/json
```

Não envie corpo. A resposta é `200 OK` com status `PaymentRejected`, preserva itens, preços históricos, total e data de criação, e mantém `cancelledAt: null`. A consulta de estoque seguinte confirma: físico 5, reservado 0 e disponível 5.

Rejeitar pedido inexistente retorna `404 order.not_found`. Rejeitar novamente, aprovar ou cancelar um pedido rejeitado retorna `409`: para as ações de pagamento, `payment.order_not_pending`; para o cancelamento, `order.cannot_cancel`. Rejeição concorrente com aprovação ou cancelamento permite somente uma transição; a suíte de integração verifica o estado persistido e que a reserva é liberada uma única vez.

## 12. Expedição integral

Na coleção pronta, execute a pasta **08 - Order dispatch** desde a primeira requisição. Ela cria produto, estoque e pedido exclusivos, aprova o pagamento e usa `dispatchProductId` e `dispatchOrderId`.

```http
POST {{baseUrl}}/orders/{{dispatchOrderId}}/dispatch
Accept: application/json
```

Não envie corpo. A resposta `200 OK` retorna status `Dispatched` e `dispatchedAt`. Para o exemplo da coleção, antes da expedição o estoque é físico 5, reservado 2 e disponível 3; depois é físico 3, reservado 0 e disponível 3.

Repetir a expedição ou tentar aprovar, rejeitar ou cancelar o pedido expedido retorna `409`. Pedido inexistente retorna `404 order.not_found`. Duas expedições simultâneas permitem apenas uma baixa; a suíte de integração também verifica uma expedição concorrente com aumento de estoque.
