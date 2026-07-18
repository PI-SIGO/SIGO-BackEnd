# Rotas da API SIGO v1

Este documento descreve o contrato implementado. Todas as rotas de negócio usam exclusivamente o prefixo versionado `/api/v1`.

## Convenções

- Autenticação: JWT Bearer.
- Perfis: `Admin`, `Oficina`, `Funcionario` e `Cliente`.
- Coleções: aceitam `?page=1&pageSize=20`; `pageSize` deve estar entre 1 e 100.
- Coleções retornam `items`, `page`, `pageSize`, `totalItems` e `totalPages`. Uma coleção vazia retorna `200`, nunca `404`.
- Recurso criado: `201 Created`.
- Atualização: `200 OK` com o recurso atualizado.
- Exclusão ou inativação: `204 No Content`.
- Erros: `ProblemDetails` (RFC 7807), normalmente `401`, `403`, `404`, `409`, `422` ou `429`.
- Cliente, oficina e funcionário usam exclusão lógica. Seus dados permanecem no banco, mas contas e sessões inativas deixam de funcionar.

## Cadastro e vínculo do cliente

| Método e rota | Acesso | Funcionamento |
|---|---|---|
| `POST /api/v1/clientes/cadastros` | Público | Recebe CPF, nome, e-mail e senha. Se o CPF já foi cadastrado por uma oficina, cria a conta no mesmo `ClienteId`; veículos e pedidos existentes aparecem automaticamente. Se o CPF não existe, cria um cliente básico e a conta. |
| `POST /api/v1/clientes` | Oficina, Funcionário | Cadastro completo feito pela oficina: recebe CPF, nome, e-mail, observação, razão, nascimento, sexo, endereço e até cinco telefones. CPF novo cria um `Cliente` ativo sem credencial; CPF existente reutiliza o mesmo `ClienteId`. O vínculo `ClienteOficina` é criado imediatamente. A oficina não define senha, situação ou IDs pelo corpo da requisição. |
| `GET /api/v1/clientes/me/vinculos` | Cliente | Lista as oficinas relacionadas à conta do cliente e informa se cada vínculo está ativo. |
| `DELETE /api/v1/clientes/me/vinculos/{oficinaId}` | Cliente | Revoga o vínculo com a oficina sem apagar cliente, veículos ou histórico. A oficina não pode recuperar o acesso repetindo o pré-cadastro. |

O fluxo direto por CPF foi escolhido para a demonstração acadêmica. Ele não comprova a identidade civil do titular e precisará de verificação adicional antes de uso em produção.

## Autenticação

| Método e rota | Acesso | Funcionamento |
|---|---|---|
| `POST /api/v1/clientes/login` | Público | Valida `cpf` e `senha`; retorna `accessToken` e `tokenType`. Exige cliente e conta ativos. O e-mail continua sendo apenas dado de contato. |
| `PUT /api/v1/clientes/me/senha` | Cliente | Confere a senha atual, altera o hash e invalida os JWTs anteriores. |
| `POST /api/v1/oficinas/login` | Público | Retorna JWT somente para oficina ativa. |
| `POST /api/v1/funcionarios/login` | Público | Retorna JWT somente para funcionário ativo em oficina ativa; administrador ativo não depende de oficina. |

JWTs são revalidados contra o estado atual. Inativação, mudança de perfil ou transferência de funcionário invalida imediatamente uma sessão incompatível.

## Clientes

| Método e rota | Acesso | Funcionamento |
|---|---|---|
| `GET /api/v1/clientes` | Admin, Oficina, Funcionário | Admin lista clientes ativos; oficina e funcionário recebem somente clientes com vínculo ativo com sua oficina. |
| `GET /api/v1/clientes/{id}` | Admin, Oficina, Funcionário, Cliente | Cliente acessa apenas o próprio cadastro; oficina e funcionário apenas cliente vinculado. Inclui telefones e veículos. |
| `GET /api/v1/clientes/nome/{nome}` | Admin, Oficina, Funcionário | Pesquisa parcial e paginada, sempre dentro do escopo da oficina quando aplicável. |
| `GET /api/v1/clientes/oficinas/{oficinaId}` | Admin, Oficina, Funcionário | Lista os clientes da oficina. Não-admin só pode informar o próprio `oficinaId`. |
| `PUT /api/v1/clientes/{id}` | Admin, Cliente | Atualiza o perfil e telefones. CPF, e-mail e situação não fazem parte do contrato editável; senha usa a rota dedicada. |
| `DELETE /api/v1/clientes/{id}` | Admin, Cliente | Inativa cliente, conta e todos os vínculos, incrementa a versão do token e preserva o histórico. Cliente só inativa a própria conta. |

## Oficinas

| Método e rota | Acesso | Funcionamento |
|---|---|---|
| `GET /api/v1/oficinas` | Admin | Lista oficinas ativas com paginação. |
| `GET /api/v1/oficinas/{id}` | Admin, Oficina | Admin acessa qualquer oficina ativa; oficina acessa apenas a própria. |
| `GET /api/v1/oficinas/nome/{nome}` | Admin | Pesquisa oficinas ativas pelo nome. |
| `POST /api/v1/oficinas` | Público | Cadastra oficina, normaliza CNPJ e armazena somente o hash da senha. |
| `PUT /api/v1/oficinas/{id}` | Admin, Oficina | Admin altera o cadastro administrativo. Oficina altera somente o próprio perfil permitido; funcionário não pode editar oficina. |
| `DELETE /api/v1/oficinas/{id}` | Admin, Oficina | Inativa logicamente. Oficina só pode inativar a si mesma; funcionários vinculados deixam de autenticar enquanto a oficina estiver inativa. |

## Funcionários

| Método e rota | Acesso | Funcionamento |
|---|---|---|
| `GET /api/v1/funcionarios` | Admin, Oficina, Funcionário | Admin lista ativos; demais listam somente ativos da própria oficina. |
| `GET /api/v1/funcionarios/{id}` | Admin, Oficina, Funcionário | Respeita o escopo da oficina. |
| `GET /api/v1/funcionarios/nome/{nome}` | Admin, Oficina, Funcionário | Pesquisa paginada dentro do mesmo escopo. |
| `POST /api/v1/funcionarios` | Admin, Oficina | Admin escolhe oficina e perfil permitido; oficina cria somente `Funcionario` na própria oficina. |
| `PUT /api/v1/funcionarios/{id}` | Admin, Oficina | Admin altera qualquer ativo; oficina altera somente funcionário da própria oficina. Funcionário comum não edita colegas. |
| `DELETE /api/v1/funcionarios/{id}` | Admin, Oficina | Inativa logicamente dentro do escopo autorizado. Funcionário comum não pode inativar colegas. |

## Veículos e imagens

| Método e rota | Acesso | Funcionamento |
|---|---|---|
| `GET /api/v1/veiculos` | Todos os perfis | Admin vê todos; cliente vê os próprios; oficina e funcionário veem veículos de clientes vinculados. |
| `GET /api/v1/veiculos/{id}` | Todos os perfis | Busca unitária aplicando o mesmo escopo. |
| `GET /api/v1/veiculos/placa/{placa}` | Todos os perfis | Pesquisa parcial e paginada por placa. |
| `GET /api/v1/veiculos/tipo/{tipo}` | Todos os perfis | Pesquisa parcial e paginada por tipo. |
| `POST /api/v1/veiculos` | Cliente | Cadastra um veículo para o cliente autenticado; o `ClienteId` é obtido do JWT. |
| `POST /api/v1/clientes/{clienteId}/veiculos` | Admin, Oficina, Funcionário | Cadastra um veículo para o cliente indicado na rota. Oficina e funcionário só podem usar cliente vinculado à própria oficina. |
| `PUT /api/v1/veiculos/{id}` | Admin, Oficina, Funcionário, Cliente | Atualiza somente os dados básicos e nunca transfere o veículo para outro cliente. |
| `DELETE /api/v1/veiculos/{id}` | Admin, Cliente | Cliente remove somente veículo próprio. |
| `POST /api/v1/veiculos/{id}/imagens` | Admin, Oficina, Funcionário, Cliente | `multipart/form-data`, campo `imagens`; aceita 1 a 5 JPEG/PNG/WebP, até 5 MB cada, com validação da assinatura real. Oficina e funcionário só adicionam imagens a veículos visíveis para sua oficina. |
| `GET /api/v1/veiculos/{veiculoId}/imagens/{nomeArquivo}` | Todos os perfis | Entrega o arquivo, com suporte a Range, após validar o escopo. |
| `DELETE /api/v1/veiculos/{veiculoId}/imagens/{imagemId}` | Admin, Cliente | Remove metadado e arquivo; cliente somente em veículo próprio. |

Nas respostas de oficina e funcionário, os pedidos e registros de serviço aninhados no veículo são filtrados pela oficina do JWT. Cliente vê o histórico completo dos próprios veículos; Admin mantém a visão global.

O corpo usado em criação e edição contém somente nome, tipo, placa, chassi, ano, quilometragem, combustível, seguro e cor. `ClienteId`, pedidos, marcas, imagens, registros de serviço e status não são aceitos no cadastro; associação e estado inicial são definidos pelo backend.

A cor continua sendo texto no veículo; não existe catálogo ou controller de cores.

## Telefones, marcas e CEP

| Método e rota | Acesso | Funcionamento |
|---|---|---|
| `GET /api/v1/telefones/{id}` | Todos os perfis | Cliente acessa telefone próprio; oficina e funcionário apenas de cliente vinculado. |
| `GET /api/v1/telefones/nome/{nome}` | Admin, Oficina, Funcionário | Pesquisa paginada pelo nome do cliente, respeitando oficina. |
| `POST /api/v1/telefones` | Admin, Cliente | Cria telefone; cliente só pode usar o próprio `ClienteId`. |
| `PUT /api/v1/telefones/{id}` | Admin, Cliente | Atualiza dentro do mesmo escopo. |
| `DELETE /api/v1/telefones/{id}` | Admin, Cliente | Exclui dentro do mesmo escopo. |
| `GET /api/v1/marcas` | Todos os perfis | Lista o catálogo global com paginação. |
| `GET /api/v1/marcas/{id}` | Todos os perfis | Busca marca por ID. |
| `GET /api/v1/marcas/nome/{nome}` | Todos os perfis | Pesquisa paginada por nome. |
| `POST /api/v1/marcas` | Admin, Oficina | Cria marca. |
| `PUT /api/v1/marcas/{id}` | Admin, Oficina | Atualiza ou retorna `404` se não existir. |
| `DELETE /api/v1/marcas/{id}` | Admin, Oficina | Exclui ou retorna `404` se não existir. |
| `GET /api/v1/ceps/{cep}` | Todos os perfis | Normaliza oito dígitos e consulta ViaCEP; não grava dados. |

## Peças e serviços

Todas estas rotas aceitam `Admin`, `Oficina` e `Funcionario`. Fora do perfil Admin, o backend força ou valida a oficina do JWT.

| Método e rota | Funcionamento |
|---|---|
| `GET /api/v1/pecas` | Lista paginada da oficina. |
| `GET /api/v1/pecas/{id}` | Busca peça dentro do escopo. |
| `POST /api/v1/pecas` | Cria peça na oficina autorizada. |
| `PUT /api/v1/pecas/{id}` | Atualiza sem permitir transferência indevida entre oficinas. |
| `DELETE /api/v1/pecas/{id}` | Exclui a peça e retorna `204`. |
| `GET /api/v1/servicos` | Lista paginada da oficina. |
| `GET /api/v1/servicos/{id}` | Busca serviço com associações de funcionários. |
| `GET /api/v1/servicos/nome/{nome}` | Pesquisa paginada por nome. |
| `POST /api/v1/servicos` | Cria serviço e valida funcionários associados na mesma oficina. |
| `PUT /api/v1/servicos/{id}` | Atualiza e sincroniza `Funcionario_Servicos` em transação. |
| `DELETE /api/v1/servicos/{id}` | Exclui o serviço e retorna `204`. |

## Pedidos

| Método e rota | Acesso | Funcionamento |
|---|---|---|
| `GET /api/v1/pedidos` | Admin, Oficina, Funcionário, Cliente | Admin vê todos; oficina e funcionário veem os pedidos da oficina do JWT; cliente vê seus pedidos. |
| `GET /api/v1/pedidos/{id}` | Admin, Oficina, Funcionário, Cliente | Busca unitária dentro do escopo. |
| `GET /api/v1/pedidos/me/servicos` | Cliente | Lista paginada dos serviços encontrados nos pedidos do cliente. |
| `GET /api/v1/pedidos/me/funcionarios` | Cliente | Lista paginada dos funcionários relacionados aos pedidos do cliente. |
| `POST /api/v1/pedidos` | Admin, Oficina, Funcionário | Valida cliente vinculado, funcionário ativo, veículo do cliente, peças e serviços na mesma oficina. Para funcionário, a oficina sempre vem do JWT. |
| `PUT /api/v1/pedidos/{id}` | Admin, Oficina, Funcionário | Repete as validações e sincroniza peças e serviços em uma transação. Funcionário só altera pedidos da oficina do JWT. |
| `DELETE /api/v1/pedidos/{id}` | Admin, Oficina | Exclui dentro do escopo e retorna `204`. |

## Relatório de histórico do veículo

| Método e rota | Acesso | Funcionamento |
|---|---|---|
| `GET /api/v1/relatorios/veiculos/{veiculoId}` | Todos os perfis | Gera PDF do histórico. Aceita `from`, `to` e `tipo`; os filtros são aplicados tanto a registros quanto a pedidos. |

`RegistroServico` é um dado interno usado na composição do relatório; não possui controller nem rotas CRUD públicas.

## Infraestrutura

| Método e rota | Acesso | Funcionamento |
|---|---|---|
| `GET /health/live` | Público | Confirma que o processo está vivo. |
| `GET /health/ready` | Público | Confirma também a conectividade com o banco. |
| `GET /swagger/v1/swagger.json` | Público em Development | Documento OpenAPI. |
| `GET /swagger` | Público em Development | Interface Swagger UI. |
