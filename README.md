# Raízes do Nordeste — API Back-End

API RESTful desenvolvida em **ASP.NET Core 8** para a rede de lanchonetes "Raízes do Nordeste", como parte do Projeto Multidisciplinar (Trilha Back-End) — UNINTER 2026.

A solução contempla gestão de pedidos multicanal, controle de estoque por unidade, autenticação JWT, programa de fidelização, pagamento simulado (mock) e conformidade com a LGPD.

---

## Tecnologias

| Componente | Tecnologia |
|---|---|
| Linguagem | C# / .NET 8 |
| Framework Web | ASP.NET Core 8 (Minimal API + Controllers) |
| ORM | Entity Framework Core 8 |
| Banco de Dados | SQL Server (LocalDB ou instância completa) |
| Autenticação | JWT Bearer + BCrypt (hash de senha) |
| Documentação | Swagger / OpenAPI (Swashbuckle 6) |
| Auditoria | AuditoriaLog (tabela interna) |

---

## Pré-requisitos

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- SQL Server LocalDB **ou** SQL Server Express/Developer
  - LocalDB vem incluído no Visual Studio; para instalar separado: [SQL Server Express](https://www.microsoft.com/pt-br/sql-server/sql-server-downloads)
- [EF Core CLI](https://learn.microsoft.com/pt-br/ef/core/cli/dotnet) (instalar uma vez):
  ```bash
  dotnet tool install --global dotnet-ef
  ```

---

## Configuração do Ambiente

### 1. Clone o repositório

```bash
git clone https://github.com/SEU_USUARIO/RaizesDoNordeste.git
cd RaizesDoNordeste/RaizerNordesteWeb.API
```

### 2. Configure as variáveis de ambiente

Copie o arquivo de exemplo e ajuste os valores:

```bash
cp appsettings.example.json appsettings.json
```

Edite `appsettings.json` com sua connection string e uma chave JWT segura:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=(localdb)\\mssqllocaldb;Database=RaizesNordesteDb;Trusted_Connection=True;MultipleActiveResultSets=true"
  },
  "Jwt": {
    "SecretKey": "SUA_CHAVE_SECRETA_COM_MINIMO_32_CARACTERES",
    "Issuer": "RaizesDoNordesteAPI",
    "Audience": "RaizesDoNordesteClientes",
    "ExpiracaoHoras": 8
  }
}
```

> **Atenção:** nunca suba o `appsettings.json` com dados reais para o repositório. O arquivo já está no `.gitignore`.

---

## Instalação e Execução

### 3. Instale as dependências

```bash
dotnet restore
```

### 4. Crie o banco de dados e execute as migrations

```bash
dotnet ef database update
```

Isso criará o banco `RaizesNordesteDb` automaticamente com todas as tabelas.

### 5. Inicie a API

```bash
dotnet run
```

A API estará disponível em:
- `https://localhost:7XXX` (HTTPS)
- `http://localhost:5XXX` (HTTP)

> A porta exata aparece no terminal ao iniciar. Verifique também em `Properties/launchSettings.json`.

---

## Documentação Swagger / OpenAPI

Com a API rodando, acesse:

```
https://localhost:{PORTA}/swagger
```

O Swagger lista todos os endpoints, permite autenticar via token JWT (botão **Authorize**) e testar as requisições diretamente no browser.

### Como autenticar no Swagger:

1. Chame `POST /auth/login` com e-mail e senha
2. Copie o `accessToken` da resposta
3. Clique em **Authorize** (canto superior direito)
4. Cole o token no formato: `Bearer SEU_TOKEN_AQUI`
5. Confirme — todos os endpoints protegidos passarão a aceitar sua identidade

---

## Endpoints Principais

| Método | Rota | Descrição | Auth |
|---|---|---|---|
| POST | `/auth/register` | Cadastro de usuário (com consentimento LGPD) | Público |
| POST | `/auth/login` | Login e geração do token JWT | Público |
| GET | `/pedidos` | Lista pedidos (com filtros por canal, status, unidade) | JWT |
| POST | `/pedidos` | Cria pedido (valida estoque e canal) | JWT |
| PATCH | `/pedidos/{id}/status` | Atualiza status do pedido | JWT (Gerente/Cozinha) |
| DELETE | `/pedidos/{id}` | Cancela pedido (devolve ao estoque) | JWT |
| POST | `/pagamentos` | Processa pagamento mock (aprovado/recusado) | JWT |
| GET | `/pagamentos/{pedidoId}` | Consulta pagamento de um pedido | JWT |
| GET | `/estoque` | Consulta estoque por unidade | JWT (Gerente+) |
| POST | `/estoque/movimentar` | Entrada/saída de estoque | JWT (Gerente+) |
| GET | `/fidelidade/{clienteId}` | Saldo de pontos do cliente | JWT |
| GET | `/fidelidade/{clienteId}/historico` | Histórico de pedidos e pontos | JWT |
| GET | `/produtos` | Lista produtos do cardápio | JWT |
| GET | `/unidades` | Lista unidades da rede | JWT |

### Filtro por canal (multicanalidade)

```
GET /pedidos?canalPedido=TOTEM&status=EmPreparo
```

Valores aceitos para `canalPedido`: `App`, `Totem`, `Balcao`, `PickUp`, `Web`

---

## Perfis de Usuário (Roles)

| Perfil | Permissões |
|---|---|
| `Admin` | Acesso total |
| `Gerente` | Pedidos, estoque, relatórios da unidade |
| `Atendente` | Criar pedidos, movimentar estoque |
| `Cozinha` | Atualizar status do pedido |
| `Cliente` | Criar pedidos, consultar os próprios pedidos e pontos |

---

## Fluxo Crítico (MVP)

```
[Cliente] POST /pedidos
     → Valida cliente, unidade, produtos e estoque
     → Desconta estoque
     → Retorna pedido com status "Aguardando"

[Cliente/Atendente] POST /pagamentos
     → Simula chamada ao gateway externo (MockPay)
     → Se aprovado: status do pedido → "EmPreparo" + acumula pontos
     → Se recusado: status do pedido → "Cancelado"

[Cozinha/Atendente] PATCH /pedidos/{id}/status
     → Atualiza: EmPreparo → Pronto → Entregue
     → Registra log de auditoria
```

---

## Coleção de Testes (Postman)

O arquivo de coleção Postman está disponível na raiz do repositório:

```
RaizesDoNordeste_Postman_Collection.json
```

### Como importar:

1. Abra o Postman
2. Clique em **Import**
3. Selecione o arquivo `RaizesDoNordeste_Postman_Collection.json`
4. A coleção aparecerá com as pastas: Auth, Pedidos, Pagamentos, Estoque, Fidelidade, Erros

### Ordem de execução recomendada:

1. `Auth / Registrar usuário (Cliente)`
2. `Auth / Login válido` → copie o `accessToken`
3. Configure a variável `{{token}}` na coleção com o valor copiado
4. `Pedidos / Criar pedido (fluxo principal)`
5. `Pagamentos / Processar pagamento mock (aprovado)`
6. `Pedidos / Consultar status do pedido`
7. Demais cenários de erro na pasta `Erros`

---

## Estrutura do Projeto

```
RaizerNordesteWeb.API/
├── Controllers/         # Endpoints da API (Auth, Pedidos, Pagamentos, Estoque, Fidelidade, Produtos, Unidades)
├── DTOs/                # Contratos de request/response e padrão de erro
├── Models/              # Entidades do domínio (Pedido, Cliente, Produto, Estoque, Pagamento...)
├── Data/                # AppDbContext (EF Core) e configurações do banco
├── Migrations/          # Migrations geradas pelo EF Core
├── Program.cs           # Configuração da aplicação (DI, JWT, Swagger, Middleware)
├── appsettings.json     # Configurações locais (NÃO subir para o repositório)
└── appsettings.example.json  # Modelo de configuração para novos ambientes
```

---

## Segurança e LGPD

- Senhas armazenadas exclusivamente como hash BCrypt (nunca em texto puro)
- Tokens JWT com expiração configurável (padrão: 8 horas)
- Consentimento LGPD obrigatório no cadastro (`consentimentoLGPD: true`)
- Clientes só visualizam seus próprios pedidos (minimização de dados)
- Logs de auditoria para todas as ações sensíveis (criar/cancelar pedido, mudar status, movimentar estoque, processar pagamento)
- Dados pessoais não são expostos em respostas de erro

---

## Padrão de Erro

Todos os erros retornam o mesmo formato JSON:

```json
{
  "error": "NOME_DO_ERRO",
  "message": "Mensagem legível para o usuário.",
  "details": [
    { "field": "campo", "issue": "descrição do problema" }
  ],
  "timestamp": "2026-05-23T12:00:00Z",
  "path": "/rota"
}
```

---

## Observações

- Nenhum pagamento real é processado. O endpoint `/pagamentos` simula a integração com um gateway externo (MockPay), representando o fluxo de envio, resposta e registro do resultado.
- O projeto foi desenvolvido individualmente como atividade avaliativa do Projeto Multidisciplinar — UNINTER 2026.
