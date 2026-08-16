# CONTEXTO DO PROJETO SIGO (BackEnd)

## 1) Visao geral

O projeto e uma API REST em ASP.NET Core para gestao de oficina/mecanica.
Ele cobre cadastro e operacao de:

- Cliente
- Telefone
- Servico
- Marca
- Veiculo
- Cor
- Funcionario
- Peca
- Oficina
- Pedido

Tambem possui integracao com ViaCEP para consulta de endereco por CEP.

---

## 2) Estrutura da solucao

- Solucao: `SIGO.sln`
- Projeto API: `SIGO/SIGO.csproj` (`net8.0`)
- Projeto de testes: `SIGO.Tests/SIGO.Tests.csproj` (`net8.0`)

Pastas principais da API:

- `Controllers/` (endpoints HTTP)
- `Services/` (regras de negocio)
- `Data/Repositories/` (acesso a dados)
- `Data/Builders/` (mapeamento EF Fluent API)
- `Objects/Models/` (entidades)
- `Objects/Dtos/` (DTOs e AutoMapper profile)
- `Integracao/` (cliente externo ViaCEP)
- `Utils/` (sanitizacao)
- `Migrations/` (migracoes EF Core)

---

## 3) Stack tecnica

- ASP.NET Core Web API
- Entity Framework Core + Npgsql (PostgreSQL)
- AutoMapper
- Refit (integracao HTTP)
- Swagger (Swashbuckle)
- xUnit + Moq (testes automatizados)

Arquivos de configuracao relevantes:

- `SIGO/Program.cs`
- `SIGO/appsettings.json`
- `NuGet.config` (na raiz da solucao)

---

## 4) Arquitetura (padrao em camadas)

Fluxo padrao da requisicao:

1. Controller recebe a requisicao HTTP
2. Service aplica validacao/regra de negocio
3. Repository acessa banco via EF Core
4. DTO <-> Entity via AutoMapper
5. Sucesso retorna DTO direto, `PagedResponse<T>` para colecoes, `201` em criacao e `204` em exclusao/inativacao

Erros sao normalizados como `ProblemDetails` (RFC 7807).

---

## 5) Banco de dados e modelo

`AppDbContext` registra os DbSets:

- Clientes
- ClienteContas
- ClienteContatos
- Telefones
- Servicos
- Marcas
- Veiculos
- Funcionarios
- Pecas
- Oficinas
- Pedidos
- RegistrosServicos
- AuditoriasSeguranca

Relacionamentos compostos configurados no `OnModelCreating`:

- `Funcionario_Servico` (N:N)
- `Pedido_Peca` (N:N)
- `Pedido_Servico` (N:N)

Migracao pendente atual:

- `20260713150936_ClienteDirectRegistrationV1`
- `20260713172020_ClienteLinkRevocationSafety`

---

## 6) Endpoints (resumo funcional)

Base única das rotas de negócio: `/api/v1`.

Contrato completo: `docs/api-v1-routes.md`.

Controllers existentes:

- `CepController` -> consulta CEP
- `ClienteController` -> consulta, atualizacao, inativacao, login e senha
- `AuthController` -> solicitacao e conclusao da recuperacao de senha por e-mail
- `ClienteRegistrationController` -> cadastro publico direto em `POST /api/v1/clientes/cadastros`
- `ClienteVinculoController` -> cadastro completo pela oficina com vinculo direto e consulta/inativacao de vinculos
- `FuncionarioController` -> CRUD + busca por nome/id
- `MarcaController` -> CRUD + busca por nome/id
- `OficinaController` -> CRUD + busca por nome
- `PecaController` -> CRUD
- `PedidoController` -> CRUD
- `ReportController` -> PDF do historico do veiculo
- `ServicoController` -> CRUD + busca por nome/id
- `TelefoneController` -> CRUD + busca por nome/id
- `VeiculoController` -> CRUD + busca por placa/tipo

---

## 7) Integracao externa

ViaCEP:

- Interface de integracao: `IViaCepIntegracao`
- Cliente Refit: `IViaCepIntegracaoRefit`
- Implementacao: `ViaCepIntegracao`
- Controller: `CepController`

Comportamento:

- Normaliza CEP para apenas digitos
- Valida tamanho de 8 digitos
- Chama `https://viacep.com.br/ws/{cep}/json/`
- Retorna `null` para CEP invalido/nao encontrado

---

## 8) Regras de negocio importantes

Ja existem validacoes de documento em services:

- `ClienteService`: validacao CPF/CNPJ + unicidade
- `FuncionarioService`: validacao CPF + unicidade
- `OficinaService`: validacao CNPJ + unicidade

Sanitizacao:

- Email (trim + lowercase)
- Apenas digitos para campos numericos/documentos/telefone

Cadastro direto do cliente:

1. O cliente envia CPF, nome, email e senha para `POST /api/v1/clientes/cadastros`.
2. Se o CPF ainda nao existir, o sistema cria o `Cliente` e sua `ClienteConta`.
3. Se uma oficina ja tiver criado um `Cliente` com esse CPF, mas ele ainda nao tiver conta, o sistema cria a `ClienteConta` apontando diretamente para o mesmo `Cliente.Id`.
4. Como veiculos, pedidos e demais dados continuam associados ao mesmo `Cliente.Id`, nada precisa ser copiado ou transferido para a nova conta.
5. O email e registrado em `ClienteContato` como dado de contato. O cadastro direto
   nao exige confirmacao; o envio de link existe apenas quando o titular solicita
   recuperacao de senha.
6. Se o CPF ja possuir conta, o cliente estiver inativo ou o email pertencer a outra conta, o cadastro retorna conflito.

Cadastro completo direto pela oficina:

1. Oficina ou funcionario envia CPF, nome e os dados disponiveis do perfil, incluindo endereco, nascimento, sexo, email e telefones, para `POST /api/v1/clientes`.
2. O sistema cria o cliente completo, ativo e sem senha/conta quando o CPF ainda nao existe ou reutiliza exatamente o mesmo `Cliente.Id` quando ele ja existe.
3. Se o CPF existente ainda nao possui conta, apenas campos vazios sao completados. Se ja possui conta, os dados controlados pelo titular nao sao sobrescritos pela oficina.
4. Um vinculo novo recebe `ClienteOficina.Ativo = true` imediatamente. Nao existem aprovacao, consentimento assistido ou vinculo pendente nesta versao.
5. Quando o titular criar sua conta posteriormente, perfil, telefones, veiculos e pedidos ja aparecem porque continuam ligados ao mesmo `Cliente.Id`.
6. Se o cliente revogar o vinculo, `RevogadoEm` e persistido e um novo cadastro da mesma oficina retorna `409`; a oficina nao recupera acesso unilateralmente.

Login do cliente:

1. O cliente autentica em `POST /api/v1/clientes/login` usando `cpf` e `senha`.
2. O CPF e normalizado e localiza o mesmo `Cliente.Id` usado por veiculos, pedidos e vinculos.
3. Oficina e funcionario continuam autenticando com email e senha em suas rotas proprias.

Recuperacao de senha:

1. `POST /api/v1/auth/forgot-password` recebe somente o e-mail e responde de forma
   generica, exista ou nao uma conta.
2. A API procura Cliente, Funcionario e Oficina automaticamente. Emails compartilhados
   entre tipos de conta geram um token independente para cada conta.
3. Somente o hash SHA-256 do token aleatorio e persistido. O link expira, e de uso
   unico e aponta para o frontend.
4. `POST /api/v1/auth/reset-password` valida o token, grava a nova senha com BCrypt e
   invalida o token na mesma transacao.

Cadastro de veiculo:

1. O cliente usa `POST /api/v1/veiculos`; o backend obtem o `ClienteId` pelo JWT.
2. Oficina ou funcionario usa `POST /api/v1/clientes/{clienteId}/veiculos`; o backend exige vinculo ativo com a oficina do JWT.
3. O corpo aceita somente os dados basicos do veiculo. `ClienteId`, status, marcas, pedidos, imagens e registros de servico nao sao campos de cadastro.
4. A ligacao usa diretamente `Veiculo.ClienteId`; nao existe nem e necessaria uma tabela intermediaria `ClienteVeiculo`.

Esse fluxo simplificado comprova apenas que a pessoa conhece o CPF. Ele e adequado para a demonstracao academica atual, mas nao deve ser tratado como verificacao segura de identidade em producao.

Na migracao, senhas legadas nao viram contas automaticamente porque o schema antigo nao informa se o cadastro foi feito pelo cliente ou pela oficina. O CPF legado permanece disponivel para um cadastro direto; a senha anterior fica inativa e e substituida quando o cliente cria sua `ClienteConta`.

---

## 9) Testes automatizados

Projeto: `SIGO.Tests`

Cobertura atual encontrada:

- `PedidoControllerTests`
- `PedidoServiceTests`

Comando base:

```powershell
dotnet test SIGO.Tests\SIGO.Tests.csproj
```

Com TRX:

```powershell
dotnet test SIGO.Tests\SIGO.Tests.csproj --logger "trx;LogFileName=resultado.trx" --results-directory SIGO.Tests\TestResults
```

---

## 10) Estado atual do projeto (operacional)

Pontos positivos:

- API organizada em camadas
- Integracao ViaCEP funcional
- CRUDs principais implementados
- Testes automatizados existentes

Pontos de atencao:

- O cadastro direto por CPF nao verifica a identidade do titular por email, documento ou atendimento presencial
- A paginacao limita a resposta, mas os repositorios atuais ainda materializam a colecao antes de paginar
- Segredos e connection string devem ser configurados por ambiente ou user-secrets

---

## 11) Como rodar localmente (resumo)

1. Configurar `ConnectionStrings__DefaultConnection` e `Jwt__Key` por variavel de ambiente ou user-secrets; segredos nao ficam em `appsettings.json`.
2. Aplicar migracoes (se necessario).
3. Subir API:

```powershell
dotnet run --project SIGO\SIGO.csproj
```

4. Swagger em ambiente Development.

---

## 12) Sugestao de evolucao (roadmap curto)

1. Levar `Count`, `Skip` e `Take` para consultas SQL em todos os repositorios paginados.
2. Adicionar verificacao forte de identidade antes de usar o vinculo por CPF em producao.
3. Implementar refresh token com rotacao e revogacao.
4. Evoluir auditoria e observabilidade para ambiente de producao.

