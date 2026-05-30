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

        public const string ClientePassword = "Cliente@123";
        public const string OficinaPassword = "Oficina@123";
        public const string FuncionarioPassword = "Funcionario@123";

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

            _logger.LogInformation(
                "Development seed completed. ClienteId={ClienteId} OficinaId={OficinaId} FuncionarioId={FuncionarioId} VeiculoId={VeiculoId}",
                cliente.Id,
                oficina.Id,
                funcionario.Id,
                veiculo.Id);
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
