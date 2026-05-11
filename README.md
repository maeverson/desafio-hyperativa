# Desafio Hyperativa — Card API

API REST segura desenvolvida em **.NET 8** para cadastro e consulta de números de cartões, com armazenamento seguro via **AES-256** e **SHA-256**.

---

## Tecnologias Utilizadas

| Tecnologia                     | Versão | Finalidade           |
| ------------------------------ | ------ | -------------------- |
| .NET                           | 8.0    | Plataforma base      |
| ASP.NET Core Web API           | 8.0    | Framework da API     |
| Entity Framework Core          | 8.0    | ORM                  |
| PostgreSQL                     | 16     | Banco de dados       |
| JWT Bearer                     | 8.0    | Autenticação         |
| FluentValidation               | 11.9   | Validação de entrada |
| Serilog                        | 8.0    | Logging estruturado  |
| Swashbuckle / Swagger          | 6.9    | Documentação da API  |
| BCrypt.Net                     | 4.0    | Hash de senhas       |
| Docker + Docker Compose        | —      | Containerização      |
| xUnit + FluentAssertions + Moq | —      | Testes unitários     |

---

## Estrutura do Projeto

```
desafio-hyperativa/
├── src/
│   ├── DesafioHyperativa.Domain/         # Entidades e interfaces
│   │   ├── Entities/                     # Card, User
│   │   └── Interfaces/                   # ICardRepository, IUserRepository
│   ├── DesafioHyperativa.Shared/         # Helpers e constantes
│   │   ├── Constants/                    # AppConstants
│   │   └── Helpers/                      # CardHashHelper (SHA-256, Luhn)
│   ├── DesafioHyperativa.Application/    # Regras de negócio
│   │   ├── DTOs/                         # Requests e Responses
│   │   ├── Interfaces/                   # Contratos dos serviços
│   │   ├── Services/                     # AuthService, CardService
│   │   └── Validators/                   # FluentValidation
│   ├── DesafioHyperativa.Infrastructure/ # Implementações externas
│   │   ├── Data/                         # AppDbContext
│   │   ├── Repositories/                 # CardRepository, UserRepository
│   │   ├── Security/                     # AesEncryptionService, JwtService
│   │   └── Extensions/                   # DI registration
│   └── DesafioHyperativa.API/            # Camada de apresentação
│       ├── Controllers/                  # AuthController, CardsController
│       ├── Middleware/                   # GlobalException, RequestLogging
│       ├── Extensions/                   # ServiceCollectionExtensions
│       └── Program.cs
├── tests/
│   └── DesafioHyperativa.UnitTests/      # Testes unitários
├── postman/
│   ├── Desafio-Hyperativa.postman_collection.json
│   ├── Desafio-Hyperativa.postman_environment.json
│   └── cartoes-exemplo.txt
├── Dockerfile
├── docker-compose.yml
└── README.md
```

---

## Segurança do Cartão

Os números de cartão **nunca são armazenados em texto puro**. A estratégia é:

| Campo            | Método      | Finalidade      |
| ---------------- | ----------- | --------------- |
| `card_hash`      | SHA-256     | Busca/indexação |
| `encrypted_card` | AES-256-CBC | Auditoria       |

A API **nunca retorna** o número completo do cartão — apenas o `cardId` (UUID).

---

## Como Executar Localmente

### Pré-requisitos

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- [PostgreSQL 16+](https://www.postgresql.org/)

### 1. Configurar banco de dados

```bash
psql -U postgres -c "CREATE USER hyperativa WITH PASSWORD 'hyperativa@123';"
psql -U postgres -c "CREATE DATABASE hyperativa_db OWNER hyperativa;"
```

### 2. Configurar variáveis de ambiente (ou appsettings.json)

Edite `src/DesafioHyperativa.API/appsettings.json`:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Port=5432;Database=hyperativa_db;Username=hyperativa;Password=hyperativa@123"
  },
  "JwtSettings": {
    "SecretKey": "SuaChaveSecretaAqui32CaracteresMinimo!",
    "Issuer": "DesafioHyperativa",
    "Audience": "DesafioHyperativaClients",
    "ExpiresInMinutes": "60"
  },
  "Encryption": {
    "Key": "<base64 de 32 bytes>",
    "IV": "<base64 de 16 bytes>"
  }
}
```

> **Gerar chaves de criptografia:**
>
> ```bash
> # Chave AES-256 (32 bytes)
> openssl rand -base64 32
> # IV AES (16 bytes)
> openssl rand -base64 16
> ```

### 3. Executar Migrations

```bash
cd src/DesafioHyperativa.Infrastructure
dotnet ef database update --startup-project ../DesafioHyperativa.API
```

> As migrations também são aplicadas **automaticamente** ao iniciar a API.

### 4. Iniciar a API

```bash
cd src/DesafioHyperativa.API
dotnet run
```

A API estará disponível em `http://localhost:5000` (ou `https://localhost:5001`).  
O Swagger UI estará em `http://localhost:5000`.

---

## Como Subir com Docker / Podman

### Pré-requisitos

- [Docker](https://docs.docker.com/get-docker/) + [Docker Compose v2](https://docs.docker.com/compose/install/) **ou** [Podman](https://podman.io/getting-started/installation) + [podman-compose](https://github.com/containers/podman-compose)

> **Nota para usuários Podman:** o `docker-compose.yml` é compatível com ambos. Os comandos abaixo mostram as duas variantes.

### 1. Construir e subir o ambiente

```bash
# Docker
docker compose up --build -d

# Podman
podman-compose up -d --build
```

O que sobe:

- **`hyperativa-db`** — PostgreSQL 16 na porta `5432`
- **`hyperativa-api`** — API .NET 8 na porta `8080`

As migrations do banco e o seed do usuário `admin` são aplicados **automaticamente** na inicialização. A API tenta conectar ao banco por até 30 segundos (10 tentativas com intervalo de 3s) antes de falhar.

### 2. Verificar se os containers estão em execução

```bash
# Docker
docker compose ps

# Podman
podman ps
```

Esperando ver ambos com status **`Up`**:

```
hyperativa-db   Up (healthy)
hyperativa-api  Up
```

### 3. Verificar os logs

```bash
# Acompanhar logs em tempo real
docker compose logs -f api

# Podman
podman logs -f hyperativa-api
```

Quando a API estiver pronta, você verá:

```
[INF] Usuário admin criado com sucesso.
[INF] Now listening on: http://[::]:8080
```

### 4. Confirmar que a API está respondendo

```bash
curl http://localhost:8080/health
# Esperado: Healthy
```

### 5. Acessar a documentação

- **Swagger UI:** `http://localhost:8080`
- **Health Check:** `http://localhost:8080/health`

### Parar e remover o ambiente

```bash
# Parar (mantém volumes de dados)
docker compose down
podman-compose down

# Parar e remover todos os volumes (apaga dados do banco)
docker compose down -v
podman-compose down -v
```

### Troubleshooting

| Sintoma                                        | Causa provável                                | Solução                                                      |
| ---------------------------------------------- | --------------------------------------------- | ------------------------------------------------------------ |
| `hyperativa-api` em `Exited` logo após subir   | API não conseguiu conectar ao banco           | Verifique os logs: `podman logs hyperativa-api`              |
| `Name or service not known` nos logs           | DNS entre containers não resolvendo           | Certifique-se de que o plugin `dnsname` do CNI está ativo    |
| `address already in use` na porta 8080 ou 5432 | Outra instância usando a porta                | Encerre o processo ou altere a porta no `docker-compose.yml` |
| `sockets not supported` no build (Podman)      | Aviso cosmético do Podman ao commitar camadas | Pode ser ignorado; o build continua normalmente              |

---

## Variáveis de Ambiente

| Variável                               | Descrição                          | Padrão                   |
| -------------------------------------- | ---------------------------------- | ------------------------ |
| `ConnectionStrings__DefaultConnection` | String de conexão PostgreSQL       | —                        |
| `JwtSettings__SecretKey`               | Chave secreta JWT (mín. 32 chars)  | —                        |
| `JwtSettings__Issuer`                  | Issuer do JWT                      | DesafioHyperativa        |
| `JwtSettings__Audience`                | Audience do JWT                    | DesafioHyperativaClients |
| `JwtSettings__ExpiresInMinutes`        | Expiração do token em minutos      | 60                       |
| `Encryption__Key`                      | Chave AES-256 em Base64 (32 bytes) | —                        |
| `Encryption__IV`                       | IV AES em Base64 (16 bytes)        | —                        |
| `ASPNETCORE_ENVIRONMENT`               | Ambiente da aplicação              | Docker                   |

---

## Como Executar Migrations Manualmente

```bash
# Instalar ferramentas EF Core (uma vez)
dotnet tool install --global dotnet-ef

# Criar nova migration
dotnet ef migrations add <NomeDaMigration> \
  --project src/DesafioHyperativa.Infrastructure \
  --startup-project src/DesafioHyperativa.API

# Aplicar migrations
dotnet ef database update \
  --project src/DesafioHyperativa.Infrastructure \
  --startup-project src/DesafioHyperativa.API
```

---

## Fluxo de Autenticação

```
1. POST /api/auth/login  →  { username, password }
2. API valida credenciais e retorna JWT Bearer Token
3. Todos os demais endpoints exigem: Authorization: Bearer <token>
```

**Usuário padrão criado automaticamente:**

| Campo    | Valor       |
| -------- | ----------- |
| Username | `admin`     |
| Password | `Admin@123` |
| Roles    | Admin, User |

---

## Exemplos de Requests

### Autenticação

```bash
curl -X POST http://localhost:8080/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{"username": "admin", "password": "Admin@123"}'
```

**Response:**

```json
{
  "accessToken": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "tokenType": "Bearer",
  "expiresIn": 3600,
  "refreshToken": "..."
}
```

---

### Inserir Cartão

```bash
curl -X POST http://localhost:8080/api/cards \
  -H "Authorization: Bearer <seu_token>" \
  -H "Content-Type: application/json" \
  -d '{"cardNumber": "4111111111111111"}'
```

**Response:**

```json
{
  "cardId": "3fa85f64-5717-4562-b3fc-2c963f66afa6"
}
```

---

### Upload em Lote (TXT)

```bash
# Criar arquivo
echo -e "4111111111111111\n5500005555555559\n378282246310005" > cards.txt

curl -X POST http://localhost:8080/api/cards/upload \
  -H "Authorization: Bearer <seu_token>" \
  -F "file=@cards.txt"
```

**Response:**

```json
{
  "totalLines": 3,
  "processed": 3,
  "invalid": 0,
  "generatedIds": [
    "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    "7d793037-a076-4dbe-a1d8-23a4a13a5d73",
    "1a2b3c4d-5e6f-7a8b-9c0d-1e2f3a4b5c6d"
  ]
}
```

---

### Consultar Cartão

```bash
curl -X GET http://localhost:8080/api/cards/4111111111111111 \
  -H "Authorization: Bearer <seu_token>"
```

**Response (encontrado):**

```json
{
  "exists": true,
  "cardId": "3fa85f64-5717-4562-b3fc-2c963f66afa6"
}
```

**Response (não encontrado):**

```json
{
  "exists": false,
  "cardId": null
}
```

---

### Health Check

```bash
curl http://localhost:8080/health
```

---

## Testando com Postman

A pasta `postman/` contém uma collection completa com todos os endpoints e scripts de teste automatizados.

```
postman/
├── Desafio-Hyperativa.postman_collection.json   # Collection com 16 requests
├── Desafio-Hyperativa.postman_environment.json  # Environment (baseUrl, token)
└── cartoes-exemplo.txt                          # Arquivo de exemplo para upload em lote
```

### 1. Importar no Postman

1. Abra o Postman
2. Clique em **Import** (canto superior esquerdo)
3. Selecione os dois arquivos abaixo e importe:
   - `postman/Desafio-Hyperativa.postman_collection.json`
   - `postman/Desafio-Hyperativa.postman_environment.json`
4. No seletor de environments (canto superior direito), escolha **"Desafio Hyperativa - Local"**

### 2. Obter o token JWT automaticamente

Execute a request **Auth → Login - Credenciais válidas**. O script de teste salva o `accessToken` automaticamente na variável de environment — todas as demais requests de `Cards` já o utilizam via `{{accessToken}}`.

### 3. Testar o upload em lote

Na request **Cards → Upload Lote - TXT válido**:

1. Clique no campo `file` em **Body → form-data**
2. Selecione o arquivo `postman/cartoes-exemplo.txt`
3. Envie a request

### Requests disponíveis

| Grupo  | Request                           | Descrição                                |
| ------ | --------------------------------- | ---------------------------------------- |
| Auth   | Login - Credenciais válidas       | 200 + salva token automaticamente        |
| Auth   | Login - Senha incorreta           | 401                                      |
| Auth   | Login - Payload inválido          | 400 + detalhes de validação              |
| Cards  | Inserir Cartão - Válido (Visa)    | 200 + UUID gerado                        |
| Cards  | Inserir Cartão - Idempotente      | 200 + mesmo UUID (sem duplicata)         |
| Cards  | Inserir Cartão - Mastercard       | 200                                      |
| Cards  | Inserir Cartão - Com espaços      | 200 (normalização automática)            |
| Cards  | Inserir Cartão - Muito curto      | 400                                      |
| Cards  | Inserir Cartão - Com letras       | 400                                      |
| Cards  | Inserir Cartão - Sem autenticação | 401                                      |
| Cards  | Consultar Cartão - Existe         | `exists: true` + UUID                    |
| Cards  | Consultar Cartão - Não existe     | `exists: false`, `cardId: null`          |
| Cards  | Consultar Cartão - Com traços     | `exists: true` (normalização automática) |
| Cards  | Upload Lote - TXT válido          | 200 + processed / invalid / generatedIds |
| Cards  | Upload Lote - Arquivo não .txt    | 400                                      |
| Cards  | Upload Lote - Sem arquivo         | 400                                      |
| Health | Health Check                      | 200 `Healthy`                            |

---

## Como Executar os Testes

```bash
# Todos os testes
dotnet test

# Com cobertura (requer coverlet)
dotnet test --collect:"XPlat Code Coverage"

# Apenas testes unitários
dotnet test tests/DesafioHyperativa.UnitTests
```

---

## Endpoints da API

| Método | Endpoint                  | Auth   | Descrição               |
| ------ | ------------------------- | ------ | ----------------------- |
| POST   | `/api/auth/login`         | ❌     | Autenticação            |
| POST   | `/api/cards`              | ✅ JWT | Inserir cartão unitário |
| POST   | `/api/cards/upload`       | ✅ JWT | Upload TXT em lote      |
| GET    | `/api/cards/{cardNumber}` | ✅ JWT | Consultar cartão        |
| GET    | `/health`                 | ❌     | Health check            |
| GET    | `/`                       | ❌     | Swagger UI              |
