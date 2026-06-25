using Microsoft.EntityFrameworkCore;
using SIGO.Objects.Enums;
using SIGO.Objects.Models;
using SIGO.Security;

namespace SIGO.Data.Seed
{
    public sealed class DevelopmentDatabaseSeeder
    {
        public const string ClienteEmail = "cliente.seed@sigo.local";
        public const string OficinaEmail = "oficina.seed@sigo.local";
        public const string FuncionarioEmail = "funcionario.seed@sigo.local";
        public const string FullTestClienteEmail = "cliente.fulltest@sigo.local";
        public const string FullTestOficinaEmail = "oficina.fulltest@sigo.local";
        public const string FullTestFuncionarioEmail = "funcionario.fulltest@sigo.local";

        public const string ClientePassword = "Cliente@123";
        public const string OficinaPassword = "Oficina@123";
        public const string FuncionarioPassword = "Funcionario@123";
        public const string FullTestClientePassword = "ClienteTeste@123";
        public const string FullTestOficinaPassword = "OficinaTeste@123";
        public const string FullTestFuncionarioPassword = "FuncionarioTeste@123";

        private readonly AppDbContext _context;
        private readonly IPasswordHasher _passwordHasher;
        private readonly ILogger<DevelopmentDatabaseSeeder> _logger;

        public DevelopmentDatabaseSeeder(
            AppDbContext context,
            IPasswordHasher passwordHasher,
            ILogger<DevelopmentDatabaseSeeder> logger)
        {
            _context = context;
            _passwordHasher = passwordHasher;
            _logger = logger;
        }

        public async Task SeedAsync(CancellationToken cancellationToken = default)
        {
            await _context.Database.MigrateAsync(cancellationToken);

            var oficina = await SeedOficina(cancellationToken);
            var cliente = await SeedCliente(cancellationToken);
            var funcionario = await SeedFuncionario(oficina.Id, cancellationToken);
            var veiculo = await SeedVeiculo(cliente.Id, cancellationToken);
            var servico = await SeedServico(oficina.Id, cancellationToken);

            await SeedClienteOficina(oficina.Id, cliente.Id, cancellationToken);
            await SeedFuncionarioServico(funcionario.Id, servico.Id, cancellationToken);
            await SeedFullTestDataset(cancellationToken);

            _logger.LogInformation(
                "Development seed completed. ClienteId={ClienteId} OficinaId={OficinaId} FuncionarioId={FuncionarioId} VeiculoId={VeiculoId}",
                cliente.Id,
                oficina.Id,
                funcionario.Id,
                veiculo.Id);
        }

        private async Task SeedFullTestDataset(CancellationToken cancellationToken)
        {
            var oficina = await SeedFullTestOficina(cancellationToken);
            var cliente = await SeedFullTestCliente(cancellationToken);
            var funcionario = await SeedFullTestFuncionario(oficina.Id, cancellationToken);
            var marca = await SeedFullTestMarca(cancellationToken);
            var veiculo = await SeedFullTestVeiculo(cliente.Id, marca, cancellationToken);
            var servico = await SeedFullTestServico(oficina.Id, cancellationToken);
            var peca = await SeedFullTestPeca(oficina.Id, marca.Id, cancellationToken);

            await SeedFullTestTelefone(cliente.Id, cancellationToken);
            await SeedClienteOficina(oficina.Id, cliente.Id, cancellationToken);
            await SeedFuncionarioServico(funcionario.Id, servico.Id, cancellationToken);
            var pedido = await SeedFullTestPedido(
                cliente.Id,
                funcionario.Id,
                oficina.Id,
                veiculo.Id,
                cancellationToken);

            await SeedFullTestPedidoServico(pedido.Id, servico.Id, cancellationToken);
            await SeedFullTestPedidoPeca(pedido.Id, peca.Id, cancellationToken);
            await SeedFullTestRegistroServico(veiculo.Id, servico.Id, cancellationToken);
            await SeedFullTestImagem(veiculo.Id, cancellationToken);
            await SeedFullTestCompartilhamento(cliente.Id, oficina.Id, cancellationToken);

            _logger.LogInformation(
                "Full test seed completed. Cliente={ClienteEmail} Senha={ClientePassword} ClienteId={ClienteId} VeiculoId={VeiculoId}",
                FullTestClienteEmail,
                FullTestClientePassword,
                cliente.Id,
                veiculo.Id);
        }

        private async Task<Oficina> SeedFullTestOficina(CancellationToken cancellationToken)
        {
            var oficina = await _context.Oficinas
                .FirstOrDefaultAsync(o => o.Email == FullTestOficinaEmail, cancellationToken);

            if (oficina is null)
            {
                oficina = new Oficina
                {
                    Nome = "Oficina Full Teste",
                    CNPJ = "44555666000190",
                    Email = FullTestOficinaEmail,
                    Numero = 500,
                    Rua = "Rua Teste Oficina",
                    Cidade = "Sao Paulo",
                    Cep = 01001000,
                    Bairro = "Centro",
                    Estado = "SP",
                    Pais = "Brasil",
                    Complemento = "Seed full",
                    Situacao = Situacao.ATIVO,
                    Senha = _passwordHasher.Hash(FullTestOficinaPassword)
                };

                await _context.Oficinas.AddAsync(oficina, cancellationToken);
            }
            else
            {
                oficina.Nome = "Oficina Full Teste";
                oficina.CNPJ = "44555666000190";
                oficina.Numero = 500;
                oficina.Rua = "Rua Teste Oficina";
                oficina.Cidade = "Sao Paulo";
                oficina.Cep = 01001000;
                oficina.Bairro = "Centro";
                oficina.Estado = "SP";
                oficina.Pais = "Brasil";
                oficina.Complemento = "Seed full";
                oficina.Situacao = Situacao.ATIVO;
                oficina.Senha = _passwordHasher.Hash(FullTestOficinaPassword);
            }

            await _context.SaveChangesAsync(cancellationToken);
            return oficina;
        }

        private async Task<Cliente> SeedFullTestCliente(CancellationToken cancellationToken)
        {
            var cliente = await _context.Clientes
                .FirstOrDefaultAsync(c => c.Email == FullTestClienteEmail, cancellationToken);

            if (cliente is null)
            {
                cliente = new Cliente
                {
                    Nome = "Cliente Full Teste",
                    Email = FullTestClienteEmail,
                    Senha = _passwordHasher.Hash(FullTestClientePassword),
                    Cpf_Cnpj = "11144477735",
                    Obs = "Cliente completo criado para testar listagem de veiculo com relacionamentos.",
                    Razao = "Cliente Full Teste",
                    DataNasc = new DateOnly(1992, 6, 15),
                    Sexo = Sexo.Outro,
                    Numero = 123,
                    Rua = "Rua Teste Cliente",
                    Cidade = "Sao Paulo",
                    Cep = "01001000",
                    Bairro = "Centro",
                    Estado = "SP",
                    Pais = "Brasil",
                    Complemento = "Apto 101",
                    TipoCliente = TipoCliente.FISICO,
                    Situacao = Situacao.ATIVO
                };

                await _context.Clientes.AddAsync(cliente, cancellationToken);
            }
            else
            {
                cliente.Nome = "Cliente Full Teste";
                cliente.Senha = _passwordHasher.Hash(FullTestClientePassword);
                cliente.Cpf_Cnpj = "11144477735";
                cliente.Obs = "Cliente completo criado para testar listagem de veiculo com relacionamentos.";
                cliente.Razao = "Cliente Full Teste";
                cliente.DataNasc = new DateOnly(1992, 6, 15);
                cliente.Sexo = Sexo.Outro;
                cliente.Numero = 123;
                cliente.Rua = "Rua Teste Cliente";
                cliente.Cidade = "Sao Paulo";
                cliente.Cep = "01001000";
                cliente.Bairro = "Centro";
                cliente.Estado = "SP";
                cliente.Pais = "Brasil";
                cliente.Complemento = "Apto 101";
                cliente.TipoCliente = TipoCliente.FISICO;
                cliente.Situacao = Situacao.ATIVO;
            }

            await _context.SaveChangesAsync(cancellationToken);
            return cliente;
        }

        private async Task<Funcionario> SeedFullTestFuncionario(int oficinaId, CancellationToken cancellationToken)
        {
            var funcionario = await _context.Funcionarios
                .FirstOrDefaultAsync(f => f.Email == FullTestFuncionarioEmail, cancellationToken);

            if (funcionario is null)
            {
                funcionario = new Funcionario
                {
                    Nome = "Funcionario Full Teste",
                    Cpf = "98765432100",
                    Cargo = "Mecanico Senior",
                    Email = FullTestFuncionarioEmail,
                    Senha = _passwordHasher.Hash(FullTestFuncionarioPassword),
                    Role = SystemRoles.Funcionario,
                    Situacao = Situacao.ATIVO,
                    IdOficina = oficinaId
                };

                await _context.Funcionarios.AddAsync(funcionario, cancellationToken);
            }
            else
            {
                funcionario.Nome = "Funcionario Full Teste";
                funcionario.Cpf = "98765432100";
                funcionario.Cargo = "Mecanico Senior";
                funcionario.Senha = _passwordHasher.Hash(FullTestFuncionarioPassword);
                funcionario.Role = SystemRoles.Funcionario;
                funcionario.Situacao = Situacao.ATIVO;
                funcionario.IdOficina = oficinaId;
            }

            await _context.SaveChangesAsync(cancellationToken);
            return funcionario;
        }

        private async Task<Marca> SeedFullTestMarca(CancellationToken cancellationToken)
        {
            var marca = await _context.Marcas
                .FirstOrDefaultAsync(m => m.Nome == "Marca Full Teste", cancellationToken);

            if (marca is null)
            {
                marca = new Marca
                {
                    Nome = "Marca Full Teste",
                    Desc = "Marca criada para teste completo.",
                    TipoMarca = "Automovel"
                };

                await _context.Marcas.AddAsync(marca, cancellationToken);
            }
            else
            {
                marca.Desc = "Marca criada para teste completo.";
                marca.TipoMarca = "Automovel";
            }

            await _context.SaveChangesAsync(cancellationToken);
            return marca;
        }

        private async Task<Veiculo> SeedFullTestVeiculo(
            int clienteId,
            Marca marca,
            CancellationToken cancellationToken)
        {
            var veiculo = await _context.Veiculos
                .Include(v => v.Marcas)
                .FirstOrDefaultAsync(v => v.PlacaVeiculo == "TST1A23", cancellationToken);

            if (veiculo is null)
            {
                veiculo = new Veiculo
                {
                    NomeVeiculo = "Veiculo Full Teste",
                    TipoVeiculo = "SUV",
                    PlacaVeiculo = "TST1A23",
                    ChassiVeiculo = "9BWZZZ377VT999999",
                    AnoFab = 2024,
                    Quilometragem = 12345,
                    Combustivel = "Flex",
                    Seguro = "Seguro teste ativo",
                    Cor = "Azul",
                    Status = Status.EmAndamento,
                    ClienteId = clienteId
                };

                await _context.Veiculos.AddAsync(veiculo, cancellationToken);
                await _context.SaveChangesAsync(cancellationToken);
            }
            else
            {
                veiculo.NomeVeiculo = "Veiculo Full Teste";
                veiculo.TipoVeiculo = "SUV";
                veiculo.ChassiVeiculo = "9BWZZZ377VT999999";
                veiculo.AnoFab = 2024;
                veiculo.Quilometragem = 12345;
                veiculo.Combustivel = "Flex";
                veiculo.Seguro = "Seguro teste ativo";
                veiculo.Cor = "Azul";
                veiculo.Status = Status.EmAndamento;
                veiculo.ClienteId = clienteId;
            }

            if (!veiculo.Marcas.Any(m => m.Id == marca.Id))
                veiculo.Marcas.Add(marca);

            await _context.SaveChangesAsync(cancellationToken);
            return veiculo;
        }

        private async Task<Servico> SeedFullTestServico(int oficinaId, CancellationToken cancellationToken)
        {
            var servico = await _context.Servicos
                .FirstOrDefaultAsync(
                    s => s.Nome == "Revisao Full Teste" && s.IdOficina == oficinaId,
                    cancellationToken);

            if (servico is null)
            {
                servico = new Servico
                {
                    Nome = "Revisao Full Teste",
                    Descricao = "Servico completo para teste da listagem de veiculos.",
                    Valor = 350m,
                    Garantia = DateOnly.FromDateTime(DateTime.UtcNow.Date.AddDays(90)),
                    IdOficina = oficinaId
                };

                await _context.Servicos.AddAsync(servico, cancellationToken);
            }
            else
            {
                servico.Descricao = "Servico completo para teste da listagem de veiculos.";
                servico.Valor = 350m;
                servico.Garantia = DateOnly.FromDateTime(DateTime.UtcNow.Date.AddDays(90));
                servico.IdOficina = oficinaId;
            }

            await _context.SaveChangesAsync(cancellationToken);
            return servico;
        }

        private async Task<Peca> SeedFullTestPeca(
            int oficinaId,
            int marcaId,
            CancellationToken cancellationToken)
        {
            var peca = await _context.Pecas
                .FirstOrDefaultAsync(
                    p => p.Nome == "Filtro Full Teste" && p.IdOficina == oficinaId,
                    cancellationToken);

            if (peca is null)
            {
                peca = new Peca
                {
                    Nome = "Filtro Full Teste",
                    Tipo = "Filtro",
                    Descricao = "Peca completa para teste da listagem de veiculos.",
                    Valor = 89.90m,
                    Quantidade = 10,
                    Garantia = DateOnly.FromDateTime(DateTime.UtcNow.Date.AddDays(180)),
                    Unidade = 1,
                    IdMarca = marcaId,
                    DataAquisicao = DateOnly.FromDateTime(DateTime.UtcNow.Date),
                    Fornecedor = "Fornecedor Full Teste",
                    IdOficina = oficinaId
                };

                await _context.Pecas.AddAsync(peca, cancellationToken);
            }
            else
            {
                peca.Tipo = "Filtro";
                peca.Descricao = "Peca completa para teste da listagem de veiculos.";
                peca.Valor = 89.90m;
                peca.Quantidade = 10;
                peca.Garantia = DateOnly.FromDateTime(DateTime.UtcNow.Date.AddDays(180));
                peca.Unidade = 1;
                peca.IdMarca = marcaId;
                peca.DataAquisicao = DateOnly.FromDateTime(DateTime.UtcNow.Date);
                peca.Fornecedor = "Fornecedor Full Teste";
                peca.IdOficina = oficinaId;
            }

            await _context.SaveChangesAsync(cancellationToken);
            return peca;
        }

        private async Task SeedFullTestTelefone(int clienteId, CancellationToken cancellationToken)
        {
            var telefone = await _context.Telefones
                .FirstOrDefaultAsync(t => t.ClienteId == clienteId && t.Numero == "988887777", cancellationToken);

            if (telefone is null)
            {
                await _context.Telefones.AddAsync(new Telefone
                {
                    ClienteId = clienteId,
                    DDD = 11,
                    Numero = "988887777"
                }, cancellationToken);
            }
            else
            {
                telefone.DDD = 11;
            }

            await _context.SaveChangesAsync(cancellationToken);
        }

        private async Task<Pedido> SeedFullTestPedido(
            int clienteId,
            int funcionarioId,
            int oficinaId,
            int veiculoId,
            CancellationToken cancellationToken)
        {
            var pedido = await _context.Pedidos
                .FirstOrDefaultAsync(
                    p => p.idCliente == clienteId &&
                         p.idVeiculo == veiculoId &&
                         p.Observacao == "Pedido Full Teste",
                    cancellationToken);

            if (pedido is null)
            {
                pedido = new Pedido
                {
                    idCliente = clienteId,
                    idFuncionario = funcionarioId,
                    idOficina = oficinaId,
                    idVeiculo = veiculoId,
                    ValorTotal = 439.90m,
                    DescontoReais = 10m,
                    DescontoPorcentagem = 0m,
                    DescontoTotalReais = 10m,
                    DescontoServicoPorcentagem = 0m,
                    DescontoServicoReais = 0m,
                    DescontoPecaPorcentagem = 0m,
                    descontoPecaReais = 0m,
                    Observacao = "Pedido Full Teste",
                    DataInicio = DateOnly.FromDateTime(DateTime.UtcNow.Date),
                    DataFim = DateOnly.FromDateTime(DateTime.UtcNow.Date.AddDays(2))
                };

                await _context.Pedidos.AddAsync(pedido, cancellationToken);
            }
            else
            {
                pedido.idFuncionario = funcionarioId;
                pedido.idOficina = oficinaId;
                pedido.ValorTotal = 439.90m;
                pedido.DescontoReais = 10m;
                pedido.DescontoPorcentagem = 0m;
                pedido.DescontoTotalReais = 10m;
                pedido.DescontoServicoPorcentagem = 0m;
                pedido.DescontoServicoReais = 0m;
                pedido.DescontoPecaPorcentagem = 0m;
                pedido.descontoPecaReais = 0m;
                pedido.DataInicio = DateOnly.FromDateTime(DateTime.UtcNow.Date);
                pedido.DataFim = DateOnly.FromDateTime(DateTime.UtcNow.Date.AddDays(2));
            }

            await _context.SaveChangesAsync(cancellationToken);
            return pedido;
        }

        private async Task SeedFullTestPedidoServico(
            int pedidoId,
            int servicoId,
            CancellationToken cancellationToken)
        {
            var pedidoServico = await _context.Set<Pedido_Servico>()
                .FirstOrDefaultAsync(
                    ps => ps.IdPedido == pedidoId && ps.IdServico == servicoId,
                    cancellationToken);

            if (pedidoServico is null)
            {
                await _context.Set<Pedido_Servico>().AddAsync(new Pedido_Servico
                {
                    IdPedido = pedidoId,
                    IdServico = servicoId,
                    QuantVezes = 1
                }, cancellationToken);
            }
            else
            {
                pedidoServico.QuantVezes = 1;
            }

            await _context.SaveChangesAsync(cancellationToken);
        }

        private async Task SeedFullTestPedidoPeca(
            int pedidoId,
            int pecaId,
            CancellationToken cancellationToken)
        {
            var pedidoPeca = await _context.Set<Pedido_Peca>()
                .FirstOrDefaultAsync(
                    pp => pp.IdPedido == pedidoId && pp.IdPeca == pecaId,
                    cancellationToken);

            if (pedidoPeca is null)
            {
                await _context.Set<Pedido_Peca>().AddAsync(new Pedido_Peca
                {
                    IdPedido = pedidoId,
                    IdPeca = pecaId,
                    Quantidade = 1,
                    DataInstalacao = DateOnly.FromDateTime(DateTime.UtcNow.Date),
                    Estado = "Nova",
                    Observacao = "Peca instalada pelo seed full."
                }, cancellationToken);
            }
            else
            {
                pedidoPeca.Quantidade = 1;
                pedidoPeca.DataInstalacao = DateOnly.FromDateTime(DateTime.UtcNow.Date);
                pedidoPeca.Estado = "Nova";
                pedidoPeca.Observacao = "Peca instalada pelo seed full.";
            }

            await _context.SaveChangesAsync(cancellationToken);
        }

        private async Task SeedFullTestRegistroServico(
            int veiculoId,
            int servicoId,
            CancellationToken cancellationToken)
        {
            var registro = await _context.RegistroServicos
                .Include(r => r.PecasSubstituidas)
                .FirstOrDefaultAsync(
                    r => r.VeiculoId == veiculoId && r.Descricao == "Registro Full Teste",
                    cancellationToken);

            if (registro is null)
            {
                registro = new RegistroServico
                {
                    VeiculoId = veiculoId,
                    ServicoId = servicoId,
                    DataServico = DateTime.UtcNow,
                    Descricao = "Registro Full Teste",
                    Quilometragem = 12345,
                    Responsavel = "Funcionario Full Teste",
                    PecasSubstituidas = new List<PecaSubstituida>
                    {
                        new()
                        {
                            Nome = "Filtro Full Teste",
                            Quantidade = 1,
                            Observacao = "Substituida durante seed full."
                        }
                    }
                };

                await _context.RegistroServicos.AddAsync(registro, cancellationToken);
            }
            else
            {
                registro.ServicoId = servicoId;
                registro.DataServico = DateTime.UtcNow;
                registro.Quilometragem = 12345;
                registro.Responsavel = "Funcionario Full Teste";

                var pecaSubstituida = registro.PecasSubstituidas
                    .FirstOrDefault(p => p.Nome == "Filtro Full Teste");

                if (pecaSubstituida is null)
                {
                    registro.PecasSubstituidas.Add(new PecaSubstituida
                    {
                        Nome = "Filtro Full Teste",
                        Quantidade = 1,
                        Observacao = "Substituida durante seed full."
                    });
                }
                else
                {
                    pecaSubstituida.Quantidade = 1;
                    pecaSubstituida.Observacao = "Substituida durante seed full.";
                }
            }

            await _context.SaveChangesAsync(cancellationToken);
        }

        private async Task SeedFullTestImagem(int veiculoId, CancellationToken cancellationToken)
        {
            var nomeArquivo = $"seed-full-veiculo-{veiculoId}.png";
            var imagem = await _context.VeiculoImagens
                .FirstOrDefaultAsync(i => i.NomeArquivo == nomeArquivo, cancellationToken);

            if (imagem is null)
            {
                imagem = new VeiculoImagem
                {
                    VeiculoId = veiculoId,
                    Url = $"/api/veiculos/{veiculoId}/imagens/{nomeArquivo}",
                    NomeArquivo = nomeArquivo,
                    NomeOriginal = "veiculo-full-teste.png",
                    ContentType = "image/png",
                    TamanhoBytes = 128,
                    CriadoEm = DateTime.UtcNow
                };

                await _context.VeiculoImagens.AddAsync(imagem, cancellationToken);
            }
            else
            {
                imagem.VeiculoId = veiculoId;
                imagem.Url = $"/api/veiculos/{veiculoId}/imagens/{nomeArquivo}";
                imagem.NomeOriginal = "veiculo-full-teste.png";
                imagem.ContentType = "image/png";
                imagem.TamanhoBytes = 128;
                imagem.CriadoEm = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync(cancellationToken);
        }

        private async Task SeedFullTestCompartilhamento(
            int clienteId,
            int oficinaId,
            CancellationToken cancellationToken)
        {
            const string codigoHash = "seed-full-test-share-hash";
            var compartilhamento = await _context.CompartilhamentosCliente
                .FirstOrDefaultAsync(c => c.CodigoHash == codigoHash, cancellationToken);

            if (compartilhamento is null)
            {
                await _context.CompartilhamentosCliente.AddAsync(new CompartilhamentoCliente
                {
                    ClienteId = clienteId,
                    CodigoHash = codigoHash,
                    ExpiraEm = DateTime.UtcNow.AddDays(7),
                    UsadoEm = null,
                    Ativo = true
                }, cancellationToken);
            }
            else
            {
                compartilhamento.ClienteId = clienteId;
                compartilhamento.ExpiraEm = DateTime.UtcNow.AddDays(7);
                compartilhamento.UsadoEm = null;
                compartilhamento.Ativo = true;
            }

            var tentativa = await _context.CompartilhamentosClienteTentativas
                .FirstOrDefaultAsync(
                    t => t.OficinaId == oficinaId &&
                         t.CodigoHash == codigoHash &&
                         t.Motivo == "seed-full",
                    cancellationToken);

            if (tentativa is null)
            {
                await _context.CompartilhamentosClienteTentativas.AddAsync(new CompartilhamentoClienteTentativa
                {
                    OficinaId = oficinaId,
                    CodigoHash = codigoHash,
                    IpAddress = "127.0.0.1",
                    Sucesso = true,
                    Motivo = "seed-full",
                    TentadoEm = DateTime.UtcNow
                }, cancellationToken);
            }
            else
            {
                tentativa.IpAddress = "127.0.0.1";
                tentativa.Sucesso = true;
                tentativa.TentadoEm = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync(cancellationToken);
        }

        private async Task<Oficina> SeedOficina(CancellationToken cancellationToken)
        {
            var oficina = await _context.Oficinas
                .FirstOrDefaultAsync(o => o.Email == OficinaEmail, cancellationToken);

            if (oficina is null)
            {
                oficina = new Oficina
                {
                    Nome = "Oficina Seed",
                    CNPJ = "11222333000181",
                    Email = OficinaEmail,
                    Numero = 100,
                    Rua = "Rua Seed Oficina",
                    Cidade = "Belo Horizonte",
                    Cep = 30140071,
                    Bairro = "Centro",
                    Estado = "MG",
                    Pais = "Brasil",
                    Complemento = "Seed",
                    Situacao = Situacao.ATIVO,
                    Senha = _passwordHasher.Hash(OficinaPassword)
                };

                await _context.Oficinas.AddAsync(oficina, cancellationToken);
            }
            else
            {
                oficina.Nome = "Oficina Seed";
                oficina.CNPJ = "11222333000181";
                oficina.Numero = 100;
                oficina.Rua = "Rua Seed Oficina";
                oficina.Cidade = "Belo Horizonte";
                oficina.Cep = 30140071;
                oficina.Bairro = "Centro";
                oficina.Estado = "MG";
                oficina.Pais = "Brasil";
                oficina.Complemento = "Seed";
                oficina.Situacao = Situacao.ATIVO;
                oficina.Senha = _passwordHasher.Hash(OficinaPassword);
            }

            await _context.SaveChangesAsync(cancellationToken);
            return oficina;
        }

        private async Task<Cliente> SeedCliente(CancellationToken cancellationToken)
        {
            var cliente = await _context.Clientes
                .FirstOrDefaultAsync(c => c.Email == ClienteEmail, cancellationToken);

            if (cliente is null)
            {
                cliente = new Cliente
                {
                    Nome = "Cliente Seed",
                    Email = ClienteEmail,
                    Senha = _passwordHasher.Hash(ClientePassword),
                    Cpf_Cnpj = "39053344705",
                    Obs = "Cliente criado pelo seeder de desenvolvimento.",
                    Razao = "Cliente Seed",
                    DataNasc = new DateOnly(1990, 1, 1),
                    Sexo = Sexo.Outro,
                    Numero = 200,
                    Rua = "Rua Seed Cliente",
                    Cidade = "Belo Horizonte",
                    Cep = "30140071",
                    Bairro = "Centro",
                    Estado = "MG",
                    Pais = "Brasil",
                    Complemento = "Seed",
                    TipoCliente = TipoCliente.FISICO,
                    Situacao = Situacao.ATIVO
                };

                await _context.Clientes.AddAsync(cliente, cancellationToken);
            }
            else
            {
                cliente.Nome = "Cliente Seed";
                cliente.Senha = _passwordHasher.Hash(ClientePassword);
                cliente.Cpf_Cnpj = "39053344705";
                cliente.Obs = "Cliente criado pelo seeder de desenvolvimento.";
                cliente.Razao = "Cliente Seed";
                cliente.DataNasc = new DateOnly(1990, 1, 1);
                cliente.Sexo = Sexo.Outro;
                cliente.Numero = 200;
                cliente.Rua = "Rua Seed Cliente";
                cliente.Cidade = "Belo Horizonte";
                cliente.Cep = "30140071";
                cliente.Bairro = "Centro";
                cliente.Estado = "MG";
                cliente.Pais = "Brasil";
                cliente.Complemento = "Seed";
                cliente.TipoCliente = TipoCliente.FISICO;
                cliente.Situacao = Situacao.ATIVO;
            }

            await _context.SaveChangesAsync(cancellationToken);
            return cliente;
        }

        private async Task<Funcionario> SeedFuncionario(int oficinaId, CancellationToken cancellationToken)
        {
            var funcionario = await _context.Funcionarios
                .FirstOrDefaultAsync(f => f.Email == FuncionarioEmail, cancellationToken);

            if (funcionario is null)
            {
                funcionario = new Funcionario
                {
                    Nome = "Funcionario Seed",
                    Cpf = "52998224725",
                    Cargo = "Mecanico",
                    Email = FuncionarioEmail,
                    Senha = _passwordHasher.Hash(FuncionarioPassword),
                    Role = SystemRoles.Funcionario,
                    Situacao = Situacao.ATIVO,
                    IdOficina = oficinaId
                };

                await _context.Funcionarios.AddAsync(funcionario, cancellationToken);
            }
            else
            {
                funcionario.Nome = "Funcionario Seed";
                funcionario.Cpf = "52998224725";
                funcionario.Cargo = "Mecanico";
                funcionario.Senha = _passwordHasher.Hash(FuncionarioPassword);
                funcionario.Role = SystemRoles.Funcionario;
                funcionario.Situacao = Situacao.ATIVO;
                funcionario.IdOficina = oficinaId;
            }

            await _context.SaveChangesAsync(cancellationToken);
            return funcionario;
        }

        private async Task<Veiculo> SeedVeiculo(int clienteId, CancellationToken cancellationToken)
        {
            var veiculo = await _context.Veiculos
                .FirstOrDefaultAsync(v => v.PlacaVeiculo == "SED1A23", cancellationToken);

            if (veiculo is null)
            {
                veiculo = new Veiculo
                {
                    NomeVeiculo = "Veiculo Seed",
                    TipoVeiculo = "Carro",
                    PlacaVeiculo = "SED1A23",
                    ChassiVeiculo = "9BWZZZ377VT004251",
                    AnoFab = 2020,
                    Quilometragem = 50000,
                    Combustivel = "Flex",
                    Seguro = "Nao informado",
                    Cor = "Prata",
                    Status = Status.Pendente,
                    ClienteId = clienteId
                };

                await _context.Veiculos.AddAsync(veiculo, cancellationToken);
            }
            else
            {
                veiculo.NomeVeiculo = "Veiculo Seed";
                veiculo.TipoVeiculo = "Carro";
                veiculo.ChassiVeiculo = "9BWZZZ377VT004251";
                veiculo.AnoFab = 2020;
                veiculo.Quilometragem = 50000;
                veiculo.Combustivel = "Flex";
                veiculo.Seguro = "Nao informado";
                veiculo.Cor = "Prata";
                veiculo.Status = Status.Pendente;
                veiculo.ClienteId = clienteId;
            }

            await _context.SaveChangesAsync(cancellationToken);
            return veiculo;
        }

        private async Task<Servico> SeedServico(int oficinaId, CancellationToken cancellationToken)
        {
            var servico = await _context.Servicos
                .FirstOrDefaultAsync(s => s.Nome == "Diagnostico Seed" && s.IdOficina == oficinaId, cancellationToken);

            if (servico is null)
            {
                servico = new Servico
                {
                    Nome = "Diagnostico Seed",
                    Descricao = "Servico criado pelo seeder de desenvolvimento.",
                    Valor = 150m,
                    Garantia = DateOnly.FromDateTime(DateTime.UtcNow.Date.AddDays(30)),
                    IdOficina = oficinaId
                };

                await _context.Servicos.AddAsync(servico, cancellationToken);
            }
            else
            {
                servico.Descricao = "Servico criado pelo seeder de desenvolvimento.";
                servico.Valor = 150m;
                servico.Garantia = DateOnly.FromDateTime(DateTime.UtcNow.Date.AddDays(30));
            }

            await _context.SaveChangesAsync(cancellationToken);
            return servico;
        }

        private async Task SeedClienteOficina(int oficinaId, int clienteId, CancellationToken cancellationToken)
        {
            var relacionamento = await _context.ClienteOficinas
                .FirstOrDefaultAsync(
                    co => co.OficinaId == oficinaId && co.ClienteId == clienteId,
                    cancellationToken);

            var agora = DateTime.UtcNow;

            if (relacionamento is null)
            {
                await _context.ClienteOficinas.AddAsync(new ClienteOficina
                {
                    OficinaId = oficinaId,
                    ClienteId = clienteId,
                    Ativo = true,
                    CreatedAt = agora,
                    UpdatedAt = agora
                }, cancellationToken);
            }
            else
            {
                relacionamento.Ativo = true;
                relacionamento.UpdatedAt = agora;
            }

            await _context.SaveChangesAsync(cancellationToken);
        }

        private async Task SeedFuncionarioServico(
            int funcionarioId,
            int servicoId,
            CancellationToken cancellationToken)
        {
            var vinculado = await _context.Set<Funcionario_Servico>()
                .AnyAsync(
                    fs => fs.IdFuncionario == funcionarioId && fs.IdServico == servicoId,
                    cancellationToken);

            if (vinculado)
                return;

            await _context.Set<Funcionario_Servico>().AddAsync(new Funcionario_Servico
            {
                IdFuncionario = funcionarioId,
                IdServico = servicoId,
                TempoDec = "01:00"
            }, cancellationToken);

            await _context.SaveChangesAsync(cancellationToken);
        }
    }
}
