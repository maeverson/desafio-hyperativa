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

## Como Subir com Docker

```bash
# Construir e subir todos os serviços
docker compose up --build

# Em background
docker compose up --build -d

# Parar
docker compose down

# Parar e remover volumes
docker compose down -v
```

A API ficará disponível em `http://localhost:8080`.  
O Swagger UI estará em `http://localhost:8080`.

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
