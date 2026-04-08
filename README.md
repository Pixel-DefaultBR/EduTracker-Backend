# EduTracker — Backend

API REST desenvolvida em **ASP.NET Core (.NET 9)** para controle de ausências escolares. Registra faltas diárias dos alunos, calcula o total dos últimos 7 dias e dispara alertas automáticos via **Discord Webhook** ao atingir o limite configurado.

---

## Por que essas tecnologias?

### C# / ASP.NET Core
C# foi escolhido por ser uma linguagem versátil, fortemente tipada e orientada a objetos, o que torna o código mais organizado, legível e fácil de manter. Além disso, o ecossistema .NET entrega performance superior a diversas outras linguagens de backend, sendo amplamente utilizado em sistemas corporativos de grande escala. A orientação a objetos facilita a modelagem das regras de negócio de forma clara e coesa.

### React
O React foi escolhido por sua integração natural com APIs REST e pela forma como escala junto ao backend. Com componentes reutilizáveis e gerenciamento de estado simples, é possível evoluir a interface do sistema sem aumentar a complexidade — tornando a combinação C# + React uma stack sólida para projetos que precisam crescer.

### PostgreSQL
O PostgreSQL foi escolhido pelos mesmos princípios: é robusto, open source, altamente escalável e amplamente suportado por provedores cloud. Suporta bem o crescimento do volume de dados e tem excelente integração com o Entity Framework Core via Npgsql.

### Discord como vetor de notificação
Para este protótipo, o Discord foi utilizado como canal de envio de alertas por ser simples de configurar via Webhook e suficiente para validar o fluxo de notificação. A ideia é que, com a evolução do projeto, as notificações sejam migradas para um servidor **SMTP** dedicado, permitindo o envio formal de e-mails para responsáveis e coordenação escolar.

---

## Tecnologias

| Tecnologia | Uso |
|---|---|
| ASP.NET Core 9 | Framework Web API |
| Entity Framework Core 9 | ORM e migrations |
| Npgsql | Driver PostgreSQL |
| Swashbuckle | Documentação Swagger |

---

## Estrutura do Projeto

```
BackEnd/
├── Controllers/
│   ├── FaltasController.cs       # Endpoints de alunos e faltas
│   └── ConfiguracaoController.cs # Endpoint de webhook do Discord
├── Data/
│   └── AppDbContext.cs           # DbContext e configuração do EF Core
├── Migrations/                   # Migrations do banco de dados
├── Models/
│   ├── Aluno.cs                  # Entidade Aluno
│   ├── RegistroFalta.cs          # Entidade RegistroFalta
│   └── Configuracao.cs           # Entidade de configuração (webhook)
├── Services/
│   ├── FaltaService.cs           # Regras de negócio de faltas
│   ├── DiscordService.cs         # Envio de alertas via Discord
│   └── EmailService.cs           # Serviço de e-mail (Mock e SMTP)
├── appsettings.json              # Configurações locais
├── Dockerfile                    # Build para produção
└── Program.cs                    # Entry point e configuração da aplicação
```

---

## Modelagem de Dados

### Aluno
| Campo | Tipo | Descrição |
|---|---|---|
| `Id` | int | Chave primária (auto-increment) |
| `Nome` | string | Nome completo do aluno |
| `Email` | string | E-mail de contato |
| `RegistrosFaltas` | ICollection | Relação com os registros de falta |

### RegistroFalta
| Campo | Tipo | Descrição |
|---|---|---|
| `Id` | int | Chave primária (auto-increment) |
| `AlunoId` | int | FK para Aluno |
| `Data` | DateOnly | Data do registro |
| `QuantidadeFaltas` | int | Quantidade de faltas no dia |

### Configuracao
| Campo | Tipo | Descrição |
|---|---|---|
| `Id` | int | Chave primária |
| `DiscordWebhookUrl` | string | URL do webhook do Discord para alertas |

---

## Endpoints

### Alunos

#### `GET /api/faltas/alunos`
Lista todos os alunos cadastrados.

**Resposta 200:**
```json
[
  { "id": 1, "nome": "João Silva", "email": "joao@escola.com" },
  { "id": 2, "nome": "Maria Souza", "email": "maria@escola.com" }
]
```

---

#### `POST /api/faltas/alunos`
Cadastra um novo aluno.

**Body:**
```json
{ "nome": "Carlos Lima", "email": "carlos@escola.com" }
```

**Resposta 201:**
```json
{ "id": 3, "nome": "Carlos Lima", "email": "carlos@escola.com" }
```

**Erros:**
| Código | Motivo |
|---|---|
| 400 | Nome ou e-mail ausente |

---

#### `GET /api/faltas/alunos/{id}/resumo`
Retorna o resumo de faltas de um aluno nos últimos 7 dias.

**Resposta 200:**
```json
{
  "id": 1,
  "nome": "João Silva",
  "email": "joao@escola.com",
  "totalFaltasUltimos7Dias": 8,
  "limiteExcedido": false,
  "registros": [
    { "data": "2026-04-07", "quantidadeFaltas": 3 },
    { "data": "2026-04-05", "quantidadeFaltas": 5 }
  ]
}
```

**Erros:**
| Código | Motivo |
|---|---|
| 404 | Aluno não encontrado |

---

### Faltas

#### `POST /api/faltas`
Registra as faltas de um aluno em uma data específica.

Após o registro, o sistema calcula automaticamente o total de faltas dos **últimos 7 dias**. Se o total atingir ou ultrapassar **7 faltas**, um alerta é enviado ao canal do Discord configurado.

**Body:**
```json
{ "alunoId": 1, "data": "2026-04-07", "quantidadeFaltas": 3 }
```

**Resposta 201:** retorna o registro criado.

**Erros:**
| Código | Motivo |
|---|---|
| 400 | Quantidade de faltas menor ou igual a zero |
| 400 | Formato de data inválido (use `YYYY-MM-DD`) |
| 404 | Aluno não encontrado |

---

### Configuração

#### `GET /api/configuracao`
Retorna a URL do webhook do Discord atualmente configurada.

**Resposta 200:**
```json
{ "discordWebhookUrl": "https://discord.com/api/webhooks/..." }
```

---

#### `PUT /api/configuracao/webhook`
Salva ou atualiza a URL do webhook do Discord.

**Body:**
```json
{ "url": "https://discord.com/api/webhooks/ID/TOKEN" }
```

**Resposta 200:**
```json
{ "mensagem": "Webhook atualizado com sucesso." }
```

---

## Regras de Negócio

- O sistema considera os **últimos 7 dias** incluindo o dia do registro atual.
- O **limite padrão** é de **7 faltas** em 7 dias (constante `LimiteFaltasSemana` em `FaltaService.cs`).
- Ao atingir o limite, o `DiscordService` consulta a URL do webhook no banco e envia uma mensagem no formato:
  > ⚠️ **Alerta de Faltas** | O aluno **[Nome]** atingiu **[N] faltas** nos últimos 7 dias.
- Se nenhum webhook estiver configurado, o alerta é ignorado e um aviso é registrado nos logs.

---

## Variáveis de Ambiente

| Variável | Descrição | Obrigatória |
|---|---|---|
| `DATABASE_URL` | URL de conexão do PostgreSQL (formato `postgres://user:pass@host:port/db`) | Sim (produção) |
| `PORT` | Porta em que a API vai escutar | Não (padrão: `5000`) |

> Em desenvolvimento, a string de conexão é lida de `appsettings.json` (`ConnectionStrings:DefaultConnection`).

---

## Como Rodar Localmente

### Pré-requisitos
- [.NET 9 SDK](https://dotnet.microsoft.com/download/dotnet/9.0)
- PostgreSQL rodando localmente

### Passos

```bash
# 1. Clonar o repositório
git clone https://github.com/Pixel-DefaultBR/EduTracker-Backend.git
cd EduTracker-Backend

# 2. Configurar a string de conexão em appsettings.json
# "DefaultConnection": "Host=localhost;Port=5432;Database=edutracker;Username=postgres;Password=sua_senha"

# 3. Restaurar dependências
dotnet restore

# 4. Rodar (as migrations são aplicadas automaticamente na inicialização)
dotnet run
```

A API estará disponível em `http://localhost:5000`.  
O Swagger estará em `http://localhost:5000/swagger`.

---

## Deploy (Railway)

O projeto está configurado para deploy automático via **Dockerfile** no Railway.

1. Criar um novo serviço no Railway apontando para este repositório
2. Adicionar um plugin **PostgreSQL** — o Railway injeta `DATABASE_URL` automaticamente
3. A aplicação aplica as migrations e inicia automaticamente

> O backend tenta conectar ao banco com até **10 tentativas** (intervalo de 3s) antes de falhar, evitando erros de race condition na inicialização.
