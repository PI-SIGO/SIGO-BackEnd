# SIGO Backend - System Context And Function Guide

This file explains how the SIGO backend works, how each main function is organized, and how access control is applied in the system.

The project is an ASP.NET Core Web API that manages clients, employees, workshops, vehicles, parts, services, orders, brands, phones, address lookup by CEP, authentication, and role-based access.

## 1. General Architecture

The backend follows a common layered structure:

| Layer | Main folders | Responsibility |
|---|---|---|
| API layer | `SIGO/Controllers` | Receives HTTP requests, validates basic input, checks authorization, calls services, and returns HTTP responses. |
| Service layer | `SIGO/Services` | Contains business rules, validation rules, mapping between DTOs and models, and calls repositories. |
| Data layer | `SIGO/Data` | Contains Entity Framework Core database context, entity configuration, repository interfaces, and repository implementations. |
| Domain/data objects | `SIGO/Objects` | Contains database models, DTOs, enums, and shared response contracts. |
| Security | `SIGO/Security` | Contains role names and authorization policy names. |
| External integrations | `SIGO/Integracao` | Contains the ViaCEP integration used to search address data by CEP. |
| Utilities | `SIGO/Utils` | Contains helper functions, currently used to sanitize values like CPF, CNPJ, CEP, and phone number. |
| Tests | `SIGO.Tests` | Contains unit tests for controllers and services. |

The request flow is normally:

1. Frontend sends an HTTP request to one controller.
2. ASP.NET Core validates JWT authentication and authorization attributes.
3. The controller validates null input, route ownership, and request permissions.
4. The controller calls a service.
5. The service applies business rules and calls a repository.
6. The repository uses Entity Framework Core to read or write PostgreSQL data.
7. The service maps database models to DTOs using AutoMapper.
8. The controller returns `Ok`, `BadRequest`, `NotFound`, `Forbid`, or `StatusCode(500)`.

## 2. Application Startup

Main file: `SIGO/Program.cs`

`Program.cs` configures the whole API.

### Registered ASP.NET Core services

| Service | What it does |
|---|---|
| `AddControllers()` | Enables controller-based API endpoints. |
| `AddEndpointsApiExplorer()` | Lets Swagger discover API endpoints. |
| `AddSwaggerGen(...)` | Enables Swagger/OpenAPI documentation and configures JWT as an HTTP bearer scheme only on authorized operations. |
| `AddAutoMapper(...)` | Registers AutoMapper profiles for model/DTO conversion. |
| `AddCors(...)` | Allows frontend requests from `localhost` and `127.0.0.1`. |
| `AddDbContext<AppDbContext>()` | Connects Entity Framework Core to PostgreSQL using `DefaultConnection`. |
| `AddHealthChecks()` | Registers liveness and database readiness checks. |
| `AddRateLimiter(...)` | Registers rate limits for login, public registration, and client sharing code redemption. |
| `AddAuthentication().AddJwtBearer(...)` | Enables JWT Bearer authentication. |
| `AddAuthorization(...)` | Defines role-based authorization policies. |
| `AddRefitClient<IViaCepIntegracaoRefit>()` | Registers the ViaCEP HTTP client. |

### Dependency injection registrations

The system registers each service with its repository:

| Interface | Implementation |
|---|---|
| `IClienteService` | `ClienteService` |
| `IClienteRepository` | `ClienteRepository` |
| `ITelefoneService` | `TelefoneService` |
| `ITelefoneRepository` | `TelefoneRepository` |
| `IServicoService` | `ServicoService` |
| `IServicoRepository` | `ServicoRepository` |
| `IMarcaService` | `MarcaService` |
| `IMarcaRepository` | `MarcaRepository` |
| `IVeiculoService` | `VeiculoService` |
| `IVeiculoRepository` | `VeiculoRepository` |
| `IFuncionarioService` | `FuncionarioService` |
| `IFuncionarioRepository` | `FuncionarioRepository` |
| `IPecaService` | `PecaService` |
| `IPecaRepository` | `PecaRepository` |
| `IOficinaService` | `OficinaService` |
| `IOficinaRepository` | `OficinaRepository` |
| `IPedidoService` | `PedidoService` |
| `IPedidoRepository` | `PedidoRepository` |
| `IViaCepIntegracao` | `ViaCepIntegracao` |

### HTTP pipeline order

The middleware order is:

1. Forwarded headers.
2. Correlation ID, request logging, and global exception middleware.
3. Swagger only in development.
4. HTTPS redirection.
5. Routing.
6. CORS policy.
7. Authentication.
8. Rate limiting.
9. Authorization.
10. Anonymous health checks and controller endpoint mapping.

This order matters because authentication must happen before authorization.

## 3. Authentication And Authorization

Main files:

| File | Purpose |
|---|---|
| `SIGO/Security/AuthorizationConstants.cs` | Defines role names and policy names. |
| `SIGO/Program.cs` | Configures JWT validation and policies. |
| Login methods in controllers | Generate JWT tokens. |

### Roles

The system has these roles:

| Role | Meaning |
|---|---|
| `Admin` | Administrator role generated from a funcionario whose `Cargo` is `ADMIN` or `ADMINISTRADOR`. |
| `Funcionario` | Employee role. Has operational access only. |
| `Oficina` | Workshop role. Has operational access where endpoint rules allow it. |
| `Cliente` | Client role. Has self-service access only. |

### Policies

| Policy | Allowed roles | Meaning |
|---|---|---|
| `FullAccess` | `Admin` | Can access restricted management areas. |
| `OperationalAccess` | `Admin`, `Funcionario`, `Oficina` | Can access operational CRUD areas like clients, parts, services, and vehicles according to endpoint rules. |
| `SelfServiceAccess` | `Admin`, `Funcionario`, `Oficina`, `Cliente` | Allows authenticated access where clients may only see or change their own data. |

### JWT configuration

JWT validation checks:

| Validation | Source |
|---|---|
| Issuer | `Jwt:Issuer` in configuration |
| Audience | `Jwt:Audience` in configuration |
| Lifetime | Token expiration |
| Signing key | `Jwt:Key` in configuration |
| Role claim | `ClaimTypes.Role` |
| Name claim | `ClaimTypes.Name` |

### OpenAPI security contract

Swagger is enabled only in development. The OpenAPI document declares JWT authentication with the HTTP bearer security scheme.

The bearer requirement is added per operation. Endpoints with `[Authorize]` are documented as protected, while endpoints with `[AllowAnonymous]` such as login and public registration are documented without the bearer requirement.

### Login flow

There are three login endpoints:

| Endpoint | Controller | Role produced |
|---|---|---|
| `POST /api/clientes/login` | `ClienteController` | `Cliente` |
| `POST /api/funcionarios/login` | `FuncionarioController` | `Admin` or `Funcionario` |
| `POST /api/oficinas/login` | `OficinaController` | `Oficina` |

Login process:

1. The request sends `Email` and `Password`.
2. The service verifies the password through the configured password hasher.
3. The service checks if a matching user exists in the database.
4. If no user is found, the API returns `BadRequest` with "Email ou senha incorretos".
5. If the user is found, the controller generates a JWT token.
6. The token contains:
   - User id as `ClaimTypes.NameIdentifier`
   - User name as `ClaimTypes.Name`
   - Email as `ClaimTypes.Email`
   - Role as `ClaimTypes.Role`
   - A unique JWT id as `JwtRegisteredClaimNames.Jti`
7. The token expires in 2 hours.

Current password storage behavior: passwords are stored as SHA-256 hashes. There is no password salt or ASP.NET Core Identity password hasher in the current implementation.

## 4. Access Matrix

This table describes the current behavior implemented by controllers.

| Feature | Oficina | Admin | Funcionario | Cliente |
|---|---:|---:|---:|---:|
| Register client | Yes | Yes | Yes | Yes, public/self register |
| Edit client | Yes | Yes | Yes | Only own register |
| Delete client | Yes | Yes | No | Only own register |
| List all clients | Yes | Yes | Yes | No |
| View own client data | Yes | Yes | Yes | Yes |
| Manage funcionario | Yes | Yes | No | No |
| Manage oficina | Yes | Yes | No | No |
| Manage marca | Yes | Yes | No | No |
| List vehicle | Yes | Yes | Yes | Only own vehicles |
| Register vehicle | Yes | Yes | No | No |
| Edit vehicle | Yes | Yes | Yes | No |
| Delete vehicle | Yes | Yes | Yes | No |
| List parts | Yes | Yes | Yes | No |
| Register parts | Yes | Yes | Yes | No |
| Edit parts | Yes | Yes | Yes | No |
| Delete parts | Yes | Yes | Yes | No |
| List services | Yes | Yes | Yes | No, except own service history through pedidos |
| Register services | Yes | Yes | Yes | No |
| Edit services | Yes | Yes | Yes | No |
| Delete services | Yes | Yes | Yes | No |
| List pedidos | Yes | Yes | No | Only own pedidos |
| Register pedidos | Yes | Yes | No | No |
| Edit pedidos | Yes | Yes | No | No |
| Delete pedidos | Yes | Yes | No | No |
| List own pedido services | Yes indirectly | Yes indirectly | No | Yes |
| List own pedido employees | Yes indirectly | Yes indirectly | No | Yes |
| Phone CRUD | Yes | Yes | Yes | Only own phone records |
| CEP lookup | Yes | Yes | Yes | Yes |

## 5. Shared Response Contract

Main files:

| File | Purpose |
|---|---|
| `SIGO/Objects/Contracts/Response.cs` | Standard response object. |
| `SIGO/Objects/Contracts/ResponseEnum.cs` | Response status codes. |
| `SIGO/Objects/Contracts/Login.cs` | Login request body. |

### `Response`

Most controllers return:

| Property | Meaning |
|---|---|
| `Code` | Enum value like `SUCCESS`, `INVALID`, `NOT_FOUND`, or `ERROR`. |
| `Message` | Human-readable message. |
| `Data` | Payload returned to the frontend. |

### `ResponseEnum`

| Value | Meaning |
|---|---|
| `SUCCESS = 1` | Operation worked. |
| `INVALID = 2` | Input is invalid. |
| `NOT_FOUND = 3` | Record was not found. |
| `CONFLICT = 4` | Conflict state. Defined but not heavily used. |
| `UNAUTHORIZED = 5` | Unauthorized state. Defined but not heavily used. |
| `ERROR = 6` | Unexpected server error. |

## 6. Data Model Context

Main files:

| File | Purpose |
|---|---|
| `SIGO/Data/AppDbContext.cs` | EF Core database context. |
| `SIGO/Data/Builders/*.cs` | Entity configuration. |
| `SIGO/Objects/Models/*.cs` | Database entity models. |
| `SIGO/Objects/Dtos/Entities/*.cs` | DTOs used by controllers and services. |

### `AppDbContext`

The database context exposes these tables:

| DbSet | Model | Table |
|---|---|---|
| `Clientes` | `Cliente` | `cliente` |
| `Telefones` | `Telefone` | `telefone` |
| `Servicos` | `Servico` | `servico` |
| `Marcas` | `Marca` | `marca` |
| `Veiculos` | `Veiculo` | `veiculo` |
| `Funcionarios` | `Funcionario` | `funcionario` |
| `Pecas` | `Peca` | `peca` |
| `Oficinas` | `Oficina` | `oficina` |
| `Pedidos` | `Pedido` | `pedido` |

### Important relationships

| Relationship | Type | Delete behavior |
|---|---|---|
| `Cliente` to `Veiculo` | One client has many vehicles | Cascade |
| `Marca` to `Peca` | One brand has many parts | Cascade |
| `Pedido` to `Cliente` | One pedido belongs to one client | Restrict |
| `Pedido` to `Funcionario` | One pedido belongs to one employee | Restrict |
| `Pedido` to `Oficina` | One pedido belongs to one workshop | Restrict |
| `Pedido` to `Veiculo` | One pedido belongs to one vehicle | Restrict |
| `Funcionario_Servico` | Many-to-many join between funcionario and servico | Composite key |
| `Pedido_Peca` | Many-to-many join between pedido and peca | Composite key |
| `Pedido_Servico` | Many-to-many join between pedido and servico | Composite key |

### Main entities

#### Cliente

Represents a client/customer.

Important fields:

| Field | Meaning |
|---|---|
| `Id` | Client id. |
| `Nome` | Client name. |
| `Email` | Client email. |
| `Senha` | Password hash. |
| `Cpf_Cnpj` | CPF or CNPJ document. |
| `Obs` | Observation. |
| `Razao` | Company/legal name. |
| `DataNasc` | Birth date. |
| `Sexo` | Gender enum. |
| Address fields | `Numero`, `Rua`, `Cidade`, `Cep`, `Bairro`, `Estado`, `Pais`, `Complemento`. |
| `TipoCliente` | Physical or legal client. |
| `Situacao` | Active or inactive. |
| `Telefones` | Client phone list. |
| `Veiculos` | Client vehicle list. |

#### Funcionario

Represents an employee.

Important fields:

| Field | Meaning |
|---|---|
| `Id` | Employee id. |
| `Nome` | Employee name. |
| `Cpf` | CPF document. |
| `Cargo` | Employee role/job. Used to decide if login role is `Admin` or `Funcionario`. |
| `Email` | Login email. |
| `Senha` | Password hash. |
| `Situacao` | Active/inactive. |

#### Oficina

Represents a workshop/company.

Important fields:

| Field | Meaning |
|---|---|
| `Id` | Workshop id. |
| `Nome` | Workshop name. |
| `CNPJ` | Company document. |
| `Email` | Login email. |
| Address fields | `Numero`, `Rua`, `Cidade`, `Cep`, `Bairro`, `Estado`, `Pais`, `Complemento`. |
| `Senha` | Password hash. |
| `Situacao` | Active/inactive. |

#### Veiculo

Represents a client vehicle.

Important fields:

| Field | Meaning |
|---|---|
| `Id` | Vehicle id. |
| `NomeVeiculo` | Vehicle name. |
| `TipoVeiculo` | Vehicle type. |
| `PlacaVeiculo` | License plate. |
| `ChassiVeiculo` | Chassis. |
| `AnoFab` | Manufacturing year. |
| `Quilometragem` | Mileage. |
| `Combustivel` | Fuel type. |
| `Seguro` | Insurance information. |
| `Cor` | Vehicle color. |
| `Status` | Vehicle status enum. |
| `ClienteId` | Owner client id. |

#### Marca

Represents a brand.

Important fields:

| Field | Meaning |
|---|---|
| `Id` | Brand id. |
| `Nome` | Brand name. |
| `Desc` | Description. |
| `TipoMarca` | Brand type. |
| `Pecas` | Parts linked to the brand. |

#### Peca

Represents a car part.

Important fields:

| Field | Meaning |
|---|---|
| `Id` | Part id. |
| `Nome` | Part name. |
| `Tipo` | Part type. |
| `Descricao` | Description. |
| `Valor` | Price. |
| `Quantidade` | Quantity in stock. |
| `Garantia` | Warranty date. |
| `Unidade` | Unit information. |
| `IdMarca` | Linked brand id. |
| `DataAquisicao` | Acquisition date. |
| `Fornecedor` | Supplier. |

#### Servico

Represents a service offered by the workshop.

Important fields:

| Field | Meaning |
|---|---|
| `Id` | Service id. |
| `Nome` | Service name. |
| `Descricao` | Description. |
| `Valor` | Service price. |
| `Garantia` | Warranty date. |
| `Funcionario_Servicos` | Employees that can perform the service. |

#### Pedido

Represents an order/service request for a vehicle.

Important fields:

| Field | Meaning |
|---|---|
| `Id` | Pedido id. |
| `idCliente` | Client id. |
| `idFuncionario` | Employee id responsible for the pedido. |
| `idOficina` | Workshop id. |
| `idVeiculo` | Vehicle id. |
| `ValorTotal` | Total value. |
| Discount fields | Discount values and percentages for total, service, and parts. |
| `Observacao` | Observation. |
| `DataInicio` | Start date. |
| `DataFim` | End date. |
| `Pedido_Pecas` | Parts used in the pedido. |
| `Pedido_Servicos` | Services included in the pedido. |

#### Telefone

Represents a client phone number.

Important fields:

| Field | Meaning |
|---|---|
| `Id` | Phone id. |
| `Numero` | Phone number. |
| `DDD` | Area code. |
| `ClienteId` | Owner client id. |

## 7. Enums

Main folder: `SIGO/Objects/Enums`

| Enum | Values | Meaning |
|---|---|---|
| `Sexo` | `Masculino = 1`, `Feminino = 2`, `Outro = 3` | Client gender. |
| `Situacao` | `ATIVO = 1`, `INATIVO = 2` | Active/inactive state. |
| `Status` | `Pendente = 0`, `AguardandoPecas = 1`, `EmAndamento = 2`, `Concluido = 3` | Vehicle/order status concept. |
| `TipoCliente` | `FISICO = 1`, `JURIDICO = 2` | Physical or legal person. |

## 8. AutoMapper

Main file: `SIGO/Objects/Dtos/Mappings/MappingsProfile.cs`

AutoMapper converts database models to DTOs and DTOs to database models.

Important mappings:

| Mapping | Detail |
|---|---|
| `Cliente` to `ClienteDTO` | Maps `Telefones`; reverse map enabled. |
| `Telefone` to `TelefoneDTO` | Reverse map enabled. |
| `Marca` to `MarcaDTO` | Reverse map enabled. |
| `Veiculo` to `VeiculoDTO` | Ignores navigation properties `Cliente` and `Marcas` when mapping into entity. |
| `Servico` to `ServicoDTO` | Reverse map enabled. |
| `Funcionario` to `FuncionarioDTO` | Reverse map enabled. |
| `Oficina` to `OficinaDTO` | Reverse map enabled. |
| `Peca` to `PecaDTO` | Reverse map enabled. |
| `Pedido` to `PedidoDTO` | Reverse map enabled. |
| Join entities | `Funcionario_Servico`, `Pedido_Peca`, `Pedido_Servico` have DTO mappings. |

## 9. Generic CRUD Infrastructure

Main files:

| File | Purpose |
|---|---|
| `SIGO/Services/Entities/GenericService.cs` | Generic business methods for CRUD. |
| `SIGO/Data/Repositories/GenericRepository.cs` | Generic EF Core methods for CRUD. |

### GenericService methods

| Method | What it does |
|---|---|
| `GetAll()` | Calls repository `Get()`, maps entities to DTOs, and returns all records. |
| `GetById(id)` | Calls repository `GetById(id)`, maps entity to DTO, and returns it. |
| `Create(entityDTO)` | Maps DTO to entity and calls repository `Add(entity)`. |
| `Update(entityDTO, id)` | Checks if entity exists, forces DTO `Id` to route id when possible, maps to entity, and calls repository `Update(entity)`. |
| `Remove(id)` | Loads entity by id, throws if not found, and calls repository `Remove(entity)`. |

### GenericRepository methods

| Method | What it does |
|---|---|
| `Get()` | Returns all rows from the table. |
| `GetById(id)` | Uses EF Core `FindAsync(id)`. |
| `Add(entity)` | Adds entity and saves changes. |
| `Update(entity)` | Detaches any tracked entity with the same `Id`, marks the new entity as modified, and saves changes. |
| `Remove(entity)` | Removes entity and saves changes. |
| `SaveChanges()` | Calls `SaveChangesAsync()`. |

## 10. Controller Function Guide

## 10.1 CEP Controller

Main file: `SIGO/Controllers/CepController.cs`

Base route: `/api/ceps`

Authorization: `SelfServiceAccess`

| Endpoint | Function | Access | Behavior |
|---|---|---|---|
| `GET /api/ceps/{cep}` | `ListarDadosEndereco` | Admin, Oficina, Funcionario, Cliente | Calls ViaCEP integration. If CEP is invalid or not found, returns `BadRequest("CEP nao encontrado!")`. Otherwise returns ViaCEP address data. |

### ViaCEP integration

Main files:

| File | Purpose |
|---|---|
| `SIGO/Integracao/ViaCepIntegracao.cs` | Validates CEP and processes ViaCEP response. |
| `SIGO/Integracao/Prefit/IViaCepIntegracaoRefit.cs` | Defines HTTP call to `/ws/{cep}/json/`. |

`ObterDadosViaCep(cep)`:

1. Rejects null, empty, or whitespace CEP.
2. Removes all non-numeric characters.
3. Requires exactly 8 digits.
4. Calls ViaCEP.
5. Returns null if ViaCEP returns error.
6. Returns `ViaCepResponse` when found.

## 10.2 Cliente Controller

Main file: `SIGO/Controllers/ClienteController.cs`

Base route: `/api/clientes`

Default authorization: `SelfServiceAccess`

| Endpoint | Function | Access | Behavior |
|---|---|---|---|
| `GET /api/clientes` | `GetAll` | Admin, Oficina, Funcionario | Lists all clients through `ClienteService.GetAll()`. Includes phones and vehicles because repository uses `Include`. |
| `GET /api/clientes/{id}` | `GetByIdWithDetails` | Admin, Oficina, Funcionario, Cliente | Gets one client with phones and vehicles. If role is Cliente, the `id` must match the JWT user id. |
| `GET /api/clientes/nome/{nome}` | `GetByNameWithDetails` | Admin, Oficina, Funcionario | Searches clients by name using `Contains(nome)`. Returns 404 if empty. |
| `POST /api/clientes` | `Post` | Anonymous | Registers a client. Forces `Id = 0`, sanitizes CPF/CNPJ, CEP, and phone numbers, validates CPF/CNPJ, hashes password, creates record. |
| `PUT /api/clientes/{id}` | `Put` | Admin, Oficina, Funcionario, Cliente | Updates a client. Cliente can only update own id. Checks existence, sanitizes values, validates CPF/CNPJ uniqueness, updates data and phone list. |
| `DELETE /api/clientes/{id}` | `Delete` | Admin, Oficina, Cliente | Deletes a client. Cliente can only delete own id. |
| `POST /api/clientes/login` | `Login` | Anonymous | Hashes password, validates credentials, returns JWT token with role `Cliente`. |

Important private functions:

| Function | What it does |
|---|---|
| `GenerateSha256Hash(input)` | Hashes a password using SHA-256 and returns lowercase hex. |
| `GenerateJwtToken(clienteDTO)` | Builds the JWT for a client. |
| `SanitizeCliente(clienteDTO)` | Keeps only digits in CPF/CNPJ, CEP, and phone numbers. |
| `IsCliente()` | Checks if current JWT role is `Cliente`. |
| `GetCurrentUserId()` | Reads `ClaimTypes.NameIdentifier` and converts it to `int`. |

## 10.3 Funcionario Controller

Main file: `SIGO/Controllers/FuncionarioController.cs`

Base route: `/api/funcionarios`

Default authorization: `FullAccess`

| Endpoint | Function | Access | Behavior |
|---|---|---|---|
| `GET /api/funcionarios` | `GetAll` | Admin, Oficina | Lists all employees. |
| `GET /api/funcionarios/{id}` | `GetFuncionarioById` | Admin, Oficina | Gets one employee by id. |
| `GET /api/funcionarios/nome/{nome}` | `GetFuncionarioByNome` | Admin, Oficina | Searches employees by name using `Contains(nome)`. |
| `POST /api/funcionarios` | `Post` | Admin, Oficina | Registers employee. Forces `Id = 0`, sanitizes CPF, validates CPF, hashes password, creates employee. |
| `PUT /api/funcionarios/{id}` | `Put` | Admin, Oficina | Updates employee. Checks existence, sanitizes CPF, validates CPF uniqueness, then updates. |
| `DELETE /api/funcionarios/{id}` | `DeleteFuncionario` | Admin, Oficina | Deletes employee. |
| `POST /api/funcionarios/login` | `Login` | Anonymous | Hashes password, validates credentials, returns JWT. |

Important private functions:

| Function | What it does |
|---|---|
| `GenerateSha256Hash(input)` | Hashes password. |
| `GenerateJwtToken(funcionarioDTO)` | Generates JWT for employee. |
| `ResolveRoleFromCargo(cargo)` | If cargo is `ADMIN` or `ADMINISTRADOR`, returns role `Admin`; otherwise returns `Funcionario`. |
| `SanitizeFuncionario(funcionarioDTO)` | Keeps only digits in CPF. |

## 10.4 Oficina Controller

Main file: `SIGO/Controllers/OficinaController.cs`

Base route: `/api/oficinas`

Default authorization: `FullAccess`

| Endpoint | Function | Access | Behavior |
|---|---|---|---|
| `GET /api/oficinas` | `Get` | Admin, Oficina | Lists all workshops. Current response message says "Cores listadas com sucesso", but the data is oficinas. |
| `GET /api/oficinas/nome/{nome}` | `GetByName` | Admin, Oficina | Searches workshops by name. Current response messages mention "cor", but the data is oficinas. |
| `POST /api/oficinas` | `Create` | Anonymous | Registers workshop. Sanitizes CNPJ, validates CNPJ uniqueness, hashes password, creates workshop. |
| `PUT /api/oficinas/{id}` | `Update` | Admin, Oficina | Updates workshop. Forces DTO id from route, sanitizes CNPJ, validates CNPJ uniqueness, updates. |
| `DELETE /api/oficinas/{id}` | `Delete` | Admin, Oficina | Deletes workshop. |
| `POST /api/oficinas/login` | `Login` | Anonymous | Hashes password, validates credentials, returns JWT with role `Oficina`. |

Important private functions:

| Function | What it does |
|---|---|
| `GenerateSha256Hash(input)` | Hashes password. |
| `GenerateJwtToken(oficinaDTO)` | Generates JWT with role `Oficina`. |
| `SanitizeOficina(oficinaDTO)` | Keeps only digits in CNPJ. |

## 10.5 Marca Controller

Main file: `SIGO/Controllers/MarcaController.cs`

Base route: `/api/marcas`

Authorization: `FullAccess`

| Endpoint | Function | Access | Behavior |
|---|---|---|---|
| `GET /api/marcas` | `GetAll` | Admin, Oficina | Lists all brands. |
| `GET /api/marcas/{id}` | `GetById` | Admin, Oficina | Gets one brand by id. |
| `GET /api/marcas/nome/{nomeMarca}` | `GetByName` | Admin, Oficina | Searches brands by name. |
| `POST /api/marcas` | `Add` | Admin, Oficina | Creates brand. |
| `PUT /api/marcas/{id}` | `Update` | Admin, Oficina | Updates brand. |
| `DELETE /api/marcas/{id}` | `Remove` | Admin, Oficina | Removes brand. |

## 10.6 Peca Controller

Main file: `SIGO/Controllers/PecaController.cs`

Base route: `/api/pecas`

Authorization: `OperationalAccess`

| Endpoint | Function | Access | Behavior |
|---|---|---|---|
| `GET /api/pecas` | `GetAll` | Admin, Oficina, Funcionario | Lists all parts. |
| `GET /api/pecas/{id}` | `Get` | Admin, Oficina, Funcionario | Gets one part by id. |
| `POST /api/pecas` | `Post` | Admin, Oficina, Funcionario | Creates part. Forces `Id = 0`. |
| `PUT /api/pecas/{id}` | `Put` | Admin, Oficina, Funcionario | Checks if part exists, then updates. |
| `DELETE /api/pecas/{id}` | `Delete` | Admin, Oficina, Funcionario | Checks if part exists, then deletes. |

## 10.7 Servico Controller

Main file: `SIGO/Controllers/ServicoController.cs`

Base route: `/api/servicos`

Authorization: `OperationalAccess`

| Endpoint | Function | Access | Behavior |
|---|---|---|---|
| `GET /api/servicos` | `GetAll` | Admin, Oficina, Funcionario | Lists services. Includes linked `Funcionario_Servicos`. |
| `GET /api/servicos/{id}` | `GetByIdWithDetails` | Admin, Oficina, Funcionario | Gets service by id with linked employee/service rows. |
| `GET /api/servicos/nome/{nome}` | `GetByNameWithDetails` | Admin, Oficina, Funcionario | Searches services by name. |
| `POST /api/servicos` | `Post` | Admin, Oficina, Funcionario | Creates service. Forces `Id = 0`. |
| `PUT /api/servicos/{id}` | `Put` | Admin, Oficina, Funcionario | Sets DTO id from route, checks existence, then updates. |
| `DELETE /api/servicos/{id}` | `Delete` | Admin, Oficina, Funcionario | Checks existence, then deletes. |

## 10.8 Telefone Controller

Main file: `SIGO/Controllers/TelefoneController.cs`

Base route: `/api/telefones`

Default authorization: `SelfServiceAccess`

| Endpoint | Function | Access | Behavior |
|---|---|---|---|
| `GET /api/telefones/{id}` | `Get` | Admin, Oficina, Funcionario, Cliente | Gets phone by id. If role is Cliente, the phone must belong to the logged client. |
| `GET /api/telefones/nome/{nome}` | `GetByNameWithDetails` | Admin, Oficina, Funcionario | Searches phones by client name. |
| `POST /api/telefones` | `Post` | Admin, Oficina, Funcionario, Cliente | Creates phone. Sanitizes phone number. If Cliente, `ClienteId` must match JWT user id. |
| `PUT /api/telefones/{id}` | `Put` | Admin, Oficina, Funcionario, Cliente | Updates phone. If Cliente, existing phone and requested `ClienteId` must belong to the logged client. |
| `DELETE /api/telefones/{id}` | `Delete` | Admin, Oficina, Funcionario, Cliente | Deletes phone. If Cliente, phone must belong to the logged client. |

Important private functions:

| Function | What it does |
|---|---|
| `SanitizeTelefone(telefoneDTO)` | Keeps only digits in phone number. |
| `IsCliente()` | Checks current role. |
| `GetCurrentUserId()` | Reads logged user id from JWT. |

## 10.9 Veiculo Controller

Main file: `SIGO/Controllers/VeiculoController.cs`

Base route: `/api/veiculos`

Default authorization: `SelfServiceAccess`

| Endpoint | Function | Access | Behavior |
|---|---|---|---|
| `GET /api/veiculos` | `Get` | Admin, Oficina, Funcionario, Cliente | Lists vehicles. If Cliente, filters to vehicles where `ClienteId` equals logged client id. |
| `GET /api/veiculos/placa/{placa}` | `GetByPlaca` | Admin, Oficina, Funcionario, Cliente | Searches vehicles by plate. If Cliente, filters to own vehicles. |
| `GET /api/veiculos/tipo/{tipo}` | `GetByTipo` | Admin, Oficina, Funcionario, Cliente | Searches vehicles by type. If Cliente, filters to own vehicles. |
| `POST /api/veiculos` | `Create` | Admin, Oficina | Creates vehicle. |
| `PUT /api/veiculos/{id}` | `Update` | Admin, Oficina, Funcionario | Updates vehicle. Uses `UpdateVeiculo`, which currently updates selected fields. |
| `DELETE /api/veiculos/{id}` | `Delete` | Admin, Oficina, Funcionario | Removes vehicle. |

Current `VeiculoRepository.UpdateVeiculo` updates:

| Field | Updated |
|---|---|
| `PlacaVeiculo` | Yes |
| `AnoFab` | Yes |
| `Id` | Preserved |

Other vehicle fields are not updated by this specific repository method.

## 10.10 Pedido Controller

Main file: `SIGO/Controllers/PedidoController.cs`

Base route: `/api/pedidos`

Default authorization: `SelfServiceAccess`

| Endpoint | Function | Access | Behavior |
|---|---|---|---|
| `GET /api/pedidos` | `GetAll` | Admin, Oficina, Cliente | Lists pedidos. If Cliente, returns only pedidos where `idCliente` equals logged client id. |
| `GET /api/pedidos/{id}` | `GetById` | Admin, Oficina, Cliente | Gets one pedido. If Cliente, pedido must belong to logged client. |
| `GET /api/pedidos/me/servicos` | `GetMyServices` | Cliente | Finds all pedidos for logged client, extracts service ids from `Pedido_Servicos`, loads all services, and returns matching services. |
| `GET /api/pedidos/me/funcionarios` | `GetMyEmployees` | Cliente | Finds all pedidos for logged client, extracts employee ids, loads all employees, and returns matching employees. |
| `POST /api/pedidos` | `Post` | Admin, Oficina | Creates pedido. Forces `Id = 0`. |
| `PUT /api/pedidos/{id}` | `Put` | Admin, Oficina | Checks if pedido exists, then updates. |
| `DELETE /api/pedidos/{id}` | `Delete` | Admin, Oficina | Checks if pedido exists, then removes. |

Important private functions:

| Function | What it does |
|---|---|
| `IsCliente()` | Checks if current JWT role is `Cliente`. |
| `GetCurrentUserId()` | Gets logged client id from JWT. |

## 11. Service Function Guide

## 11.1 ClienteService

Main file: `SIGO/Services/Entities/ClienteService.cs`

| Function | What it does |
|---|---|
| `GetAll()` | Loads all clients from repository including phones and vehicles, maps to DTOs. |
| `GetByIdWithDetails(id)` | Loads one client with phones and vehicles. |
| `GetByNameWithDetails(nome)` | Loads clients whose name contains the search term, including phones and vehicles. |
| `GetById(id)` | Loads one client without details. |
| `Create(clienteDTO)` | Validates name/email uniqueness, validates CPF/CNPJ, normalizes CPF/CNPJ to digits, maps DTO to entity, and saves. |
| `Update(clienteDTO, id)` | Checks client exists, validates name/email uniqueness, validates CPF/CNPJ, normalizes document, updates client, then synchronizes phone list by updating existing phones or adding new phones. |
| `Login(login)` | Loads matching client by email/password hash. Clears password before mapping to DTO. |
| `ValidarCpfCnpj(documento, ignoreId)` | Validates CPF or CNPJ format/check digits and verifies it is not already registered. |
| `ValidarNomeEmail(nome, email, ignoreId)` | Checks duplicated client name and email. |

CPF/CNPJ validation:

1. Removes non-digits.
2. CPF must have 11 digits and valid check digits.
3. CNPJ must have 14 digits and valid check digits.
4. Documents with all equal digits are rejected.

## 11.2 FuncionarioService

Main file: `SIGO/Services/Entities/FuncionarioService.cs`

| Function | What it does |
|---|---|
| `Login(login)` | Loads matching employee by email/password hash. Clears password before mapping to DTO. |
| `GetFuncionarioByNome(nome)` | Searches employee by name. |
| `Create(funcionarioDTO)` | Validates CPF, normalizes CPF to digits, then creates. |
| `Update(funcionarioDTO, id)` | Validates CPF, normalizes CPF, then updates. |
| `ValidarCpf(cpf, ignoreId)` | Validates CPF format/check digits and verifies uniqueness. |

## 11.3 OficinaService

Main file: `SIGO/Services/Entities/OficinaService.cs`

| Function | What it does |
|---|---|
| `Login(login)` | Loads matching workshop by email/password hash. Clears password before mapping to DTO. |
| `GetByName(nomeOficina)` | Searches workshop by name. |
| `Create(oficinaDTO)` | Validates CNPJ, normalizes CNPJ to digits, then creates. |
| `Update(oficinaDTO, id)` | Validates CNPJ, normalizes CNPJ, then updates. |
| `ValidarCnpj(cnpj, ignoreId)` | Validates CNPJ format/check digits and verifies uniqueness. |

## 11.4 MarcaService

Main file: `SIGO/Services/Entities/MarcaService.cs`

| Function | What it does |
|---|---|
| `GetAll()` | Loads all brands and maps to DTOs. |
| `GetById(idMarca)` | Loads one brand by id. |
| `GetByName(nomeMarca)` | Searches brands by name. |
| `Create(marcaDTO)` | Maps DTO to model, adds it, and saves. |
| `Update(marcaDTO, idMarca)` | Loads existing brand, maps DTO into it, updates, and saves. If brand does not exist, returns without throwing. |
| `Remove(idMarca)` | Loads existing brand, removes it, and saves. If brand does not exist, returns without throwing. |

## 11.5 VeiculoService

Main file: `SIGO/Services/Entities/VeiculoService.cs`

| Function | What it does |
|---|---|
| `GetByPlaca(placa)` | Searches vehicles by plate and maps to DTOs. |
| `GetByTipo(tipo)` | Searches vehicles by type and maps to DTOs. |
| `GetById(id)` | Loads one vehicle with client included. |
| `UpdateVeiculo(veiculoDto, id)` | Checks vehicle exists, maps DTO to model, forces model id from route, and calls repository update. |

## 11.6 ServicoService

Main file: `SIGO/Services/Entities/ServicoService.cs`

| Function | What it does |
|---|---|
| `GetAll()` | Loads all services with linked `Funcionario_Servicos`. |
| `GetByIdWithDetails(id)` | Loads one service with linked `Funcionario_Servicos`. |
| `GetByNameWithDetails(nome)` | Searches services by name with linked `Funcionario_Servicos`. |
| `GetById(id)` | Loads one service without details. |
| `Create(servicoDTO)` | Maps DTO to model, adds service, maps saved entity back to DTO. |

Other update/delete behavior comes from `GenericService`.

## 11.7 TelefoneService

Main file: `SIGO/Services/Entities/TelefoneService.cs`

| Function | What it does |
|---|---|
| `GetTelefoneByNome(nome)` | Finds phone records where the related client name contains the search term. |

Other CRUD behavior comes from `GenericService`.

## 11.8 PecaService

Main file: `SIGO/Services/Entities/PecaService.cs`

`PecaService` currently only inherits generic CRUD behavior from `GenericService`.

## 11.9 PedidoService

Main file: `SIGO/Services/Entities/PedidoService.cs`

`PedidoService` currently only inherits generic CRUD behavior from `GenericService`.

## 12. Repository Function Guide

## 12.1 ClienteRepository

Main file: `SIGO/Data/Repositories/ClienteRepository.cs`

| Function | What it does |
|---|---|
| `Get()` | Loads clients including phones and vehicles. |
| `GetByIdWithDetails(id)` | Loads one client including phones and vehicles. |
| `GetByNameWithDetails(nome)` | Searches clients by name including phones and vehicles. |
| `GetById(id)` | Loads one client without details. |
| `Add(cliente)` | Adds a client and saves. |
| `Login(login)` | Finds client by email and hashed password. Uses `AsNoTracking`. |
| `ExistsByCpfCnpj(cpfCnpj, ignoreId)` | Checks duplicated CPF/CNPJ after removing punctuation. |
| `ExistsByNome(nome, ignoreId)` | Checks duplicated name case-insensitively. |
| `ExistsByEmail(email, ignoreId)` | Checks duplicated email case-insensitively. |

## 12.2 FuncionarioRepository

Main file: `SIGO/Data/Repositories/FuncionarioRepository.cs`

| Function | What it does |
|---|---|
| `Login(login)` | Finds employee by email and hashed password. |
| `GetFuncionarioByNome(nome)` | Searches employee by name. |
| `ExistsByCpf(cpf, ignoreId)` | Checks duplicated CPF after removing punctuation. |

## 12.3 OficinaRepository

Main file: `SIGO/Data/Repositories/OficinaRepository.cs`

| Function | What it does |
|---|---|
| `Login(login)` | Finds workshop by email and hashed password. |
| `GetByName(nomeOficina)` | Searches workshop by name. |
| `ExistsByCnpj(cnpj, ignoreId)` | Checks duplicated CNPJ after removing punctuation. |

## 12.4 MarcaRepository

Main file: `SIGO/Data/Repositories/MarcaRepository.cs`

| Function | What it does |
|---|---|
| `Get()` | Loads all brands. |
| `GetByName(nomeMarca)` | Searches brands by name. |
| `GetById(idMarca)` | Loads one brand by id. |

## 12.5 VeiculoRepository

Main file: `SIGO/Data/Repositories/VeiculoRepository.cs`

| Function | What it does |
|---|---|
| `Get()` | Loads all vehicles including client. |
| `GetByPlaca(placa)` | Searches vehicles by plate including client. |
| `GetByTipo(tipo)` | Searches vehicles by type including client. |
| `GetById(id)` | Loads one vehicle including client. |
| `UpdateVeiculo(veiculo)` | Finds existing vehicle, updates selected fields, and saves. |

## 12.6 ServicoRepository

Main file: `SIGO/Data/Repositories/ServicoRepository.cs`

| Function | What it does |
|---|---|
| `Get()` | Loads all services including `Funcionario_Servicos`. |
| `GetByIdWithDetails(id)` | Loads one service including `Funcionario_Servicos`. |
| `GetByNameWithDetails(nome)` | Searches services by name including `Funcionario_Servicos`. |
| `GetById(id)` | Loads one service without details. |
| `Add(servicos)` | Adds service and saves. |

## 12.7 TelefoneRepository

Main file: `SIGO/Data/Repositories/TelefoneRepository.cs`

| Function | What it does |
|---|---|
| `GetTelefoneByNome(nome)` | Searches phone records by related client name and projects directly to `TelefoneDTO`. |

## 12.8 PecaRepository

Main file: `SIGO/Data/Repositories/PecaRepository.cs`

`PecaRepository` currently only inherits generic CRUD behavior from `GenericRepository`.

## 12.9 PedidoRepository

Main file: `SIGO/Data/Repositories/PedidoRepository.cs`

`PedidoRepository` currently only inherits generic CRUD behavior from `GenericRepository`.

## 13. Sanitization Helpers

Main file: `SIGO/Utils/SanitizeHelper.cs`

The helper is used to remove formatting characters from values that should contain only numbers.

Common examples:

| Original | Sanitized |
|---|---|
| `123.456.789-09` | `12345678909` |
| `12.345.678/0001-99` | `12345678000199` |
| `(11) 99999-9999` | `11999999999` |
| `01001-000` | `01001000` |

Controllers currently use sanitization for:

| Controller | Fields |
|---|---|
| `ClienteController` | CPF/CNPJ, CEP, client phone numbers |
| `FuncionarioController` | CPF |
| `OficinaController` | CNPJ |
| `TelefoneController` | Phone number |

## 14. Current Tests

Main folder: `SIGO.Tests`

The test project uses xUnit and Moq.

Current test files:

| Test file | What it verifies |
|---|---|
| `ClienteControllerTests.cs` | Client self-access rules and forbidden access when a client tries to access another client. |
| `VeiculoControllerTests.cs` | Vehicle access rules, especially filtering for Cliente and operational permissions. |
| `TelefoneControllerTests.cs` | Phone CRUD ownership rules for Cliente and access for other roles. |
| `PedidoControllerTests.cs` | Pedido access rules for Cliente, Oficina, and restricted creation/edit/deletion. |
| `PedidoServiceTests.cs` | Service behavior related to pedidos. |

The test suite was previously executed with:

```powershell
dotnet test SIGO.Tests\SIGO.Tests.csproj --no-restore -c Release -p:UseAppHost=false
```

Expected current result from the last verified run:

```text
20 tests passed
```

## 15. Important Current Implementation Notes

These notes describe current behavior in the codebase. They are not frontend requirements, but they matter when using or changing the API.

| Area | Current behavior |
|---|---|
| Logs | The custom log controller/middleware was removed. The project currently relies on default ASP.NET Core logging configuration. |
| Pedido creation logs | There is no custom audit message like "employee registered pedido for vehicle/client" in the current code. |
| Oficina messages | Some oficina endpoints return messages mentioning "cor/cores" even though they manage oficinas. |
| Vehicle update | `UpdateVeiculo` only updates selected fields, not the full vehicle DTO. |
| Passwords | Passwords are verified through the configured password hasher. |
| Client ownership | Controllers use JWT `NameIdentifier` to ensure Cliente can only access own resources. |
| Funcionario role | A funcionario becomes `Admin` only when `Cargo` is `ADMIN` or `ADMINISTRADOR`. |
| Public registration | Client and workshop creation endpoints are currently anonymous. |

## 16. Practical Examples For The Frontend

### Login as client

1. Call `POST /api/clientes/login` with email and password.
2. Receive JWT token in `response.Data`.
3. Send token in all protected requests:

```http
Authorization: Bearer {token}
```

The client can then:

| Need | Endpoint |
|---|---|
| See own register | `GET /api/clientes/{ownId}` |
| Edit own register | `PUT /api/clientes/{ownId}` |
| Delete own register | `DELETE /api/clientes/{ownId}` |
| See own vehicles | `GET /api/veiculos` |
| Search own vehicle by plate | `GET /api/veiculos/placa/{placa}` |
| Search own vehicle by type | `GET /api/veiculos/tipo/{tipo}` |
| See own pedidos | `GET /api/pedidos` |
| See services performed on own car | `GET /api/pedidos/me/servicos` |
| See employees that worked on own car | `GET /api/pedidos/me/funcionarios` |
| Manage own phone | `GET/POST/PUT/DELETE /api/telefones` |

### Login as funcionario

1. Call `POST /api/funcionarios/login`.
2. If `Cargo` is not `ADMIN` or `ADMINISTRADOR`, token role is `Funcionario`.

The employee can:

| Need | Endpoint group |
|---|---|
| Client list/register/edit | `/api/clientes` |
| Vehicle list/edit/delete | `/api/veiculos` |
| Part CRUD | `/api/pecas` |
| Service CRUD | `/api/servicos` |
| Phone CRUD | `/api/telefones` |

The employee cannot manage:

| Restricted area | Reason |
|---|---|
| Pedidos | Pedido create/update/delete requires Admin or Oficina. |
| Funcionarios | Requires FullAccess. |
| Oficinas | Requires FullAccess. |
| Marcas | Requires FullAccess. |

### Login as oficina

1. Call `POST /api/oficinas/login`.
2. Token role is `Oficina`.

The workshop can access the complete system according to current policy design.

