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
5. Resposta retorna em objeto padrao (`Response` + `ResponseEnum`) em varios endpoints

Observacao:

- Nem todos os controllers seguem exatamente o mesmo padrao de resposta (alguns retornam objeto anonimo).

---

## 5) Banco de dados e modelo

`AppDbContext` registra os DbSets:

- Clientes
- Telefones
- Servicos
- Marcas
- Veiculos
- Cores
- Funcionarios
- Pecas
- Oficinas
- Pedidos

Relacionamentos compostos configurados no `OnModelCreating`:

- `Funcionario_Servico` (N:N)
- `Pedido_Peca` (N:N)
- `Pedido_Servico` (N:N)

Migracao principal atual:

- `20260323165610_PrimeiraMigration`

---

## 6) Endpoints (resumo funcional)

Base route: `api/[controller]`

Controllers existentes:

- `CepController` -> consulta CEP
- `ClienteController` -> CRUD + busca por nome/id
- `CorController` -> CRUD + busca por nome
- `FuncionarioController` -> CRUD + busca por nome/id
- `MarcaController` -> CRUD + busca por nome/id
- `OficinaController` -> CRUD + busca por nome
- `PecaController` -> CRUD
- `PedidoController` -> CRUD
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

- Ausencia de modulo de autenticacao/autorizacao completo (login/JWT/refresh ainda nao consolidado)
- Existe variacao de padrao de resposta entre endpoints
- Arquivo `appsettings.json` contem placeholder de senha (`CHANGE_ME`) e deve ser configurado por ambiente

---

## 11) Como rodar localmente (resumo)

1. Ajustar connection string em `SIGO/appsettings.json` ou variavel de ambiente.
2. Aplicar migracoes (se necessario).
3. Subir API:

```powershell
dotnet run --project SIGO\SIGO.csproj
```

4. Swagger em ambiente Development.

---

## 12) Sugestao de evolucao (roadmap curto)

1. Padronizar contrato de resposta HTTP para todos os controllers.
2. Implementar autenticacao (Identity/JWT/refresh token).
3. Aumentar cobertura de testes para demais modulos alem de Pedido.
4. Revisar politicas de seguranca (rate limit em login, secrets por ambiente, auditoria).

