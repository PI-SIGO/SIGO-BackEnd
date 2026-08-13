using AutoMapper;
using SIGO.Data.Interfaces;
using SIGO.Objects.Dtos.Entities;
using SIGO.Objects.Models;
using SIGO.Services.Interfaces;
using SIGO.Validation;

namespace SIGO.Services.Entities
{
    public class PedidoService : GenericService<Pedido, PedidoDTO>, IPedidoService
    {
        private readonly IPedidoRepository _pedidoRepository;
        private readonly IMapper _mapper;
        private readonly IClienteRepository? _clienteRepository;
        private readonly IFuncionarioRepository? _funcionarioRepository;
        private readonly IVeiculoRepository? _veiculoRepository;
        private readonly IPecaRepository? _pecaRepository;
        private readonly IServicoRepository? _servicoRepository;

        public PedidoService(
            IPedidoRepository pedidoRepository,
            IMapper mapper,
            IClienteRepository? clienteRepository = null,
            IFuncionarioRepository? funcionarioRepository = null,
            IVeiculoRepository? veiculoRepository = null,
            IPecaRepository? pecaRepository = null,
            IServicoRepository? servicoRepository = null)
            : base(pedidoRepository, mapper)
        {
            _pedidoRepository = pedidoRepository;
            _mapper = mapper;
            _clienteRepository = clienteRepository;
            _funcionarioRepository = funcionarioRepository;
            _veiculoRepository = veiculoRepository;
            _pecaRepository = pecaRepository;
            _servicoRepository = servicoRepository;
        }

        public async Task<IEnumerable<PedidoDTO>> GetByOficina(int oficinaId)
        {
            var pedidos = await _pedidoRepository.GetByOficina(oficinaId);
            return _mapper.Map<IEnumerable<PedidoDTO>>(pedidos);
        }

        public async Task<IEnumerable<PedidoDTO>> GetByCliente(int clienteId)
        {
            var pedidos = await _pedidoRepository.GetByCliente(clienteId);
            return _mapper.Map<IEnumerable<PedidoDTO>>(pedidos);
        }

        public override async Task<PedidoDTO> GetById(int id)
        {
            var pedido = await _pedidoRepository.GetByIdWithDetails(id);
            return _mapper.Map<PedidoDTO>(pedido);
        }

        public async Task<PedidoDTO?> GetByIdForOficina(int id, int oficinaId)
        {
            var pedido = await _pedidoRepository.GetByIdForOficina(id, oficinaId);
            return _mapper.Map<PedidoDTO?>(pedido);
        }

        public override Task Create(PedidoDTO pedidoDTO)
        {
            return CreateCoreAsync(pedidoDTO, pedidoDTO.idOficina);
        }

        public Task CreateForOficina(PedidoDTO pedidoDTO, int oficinaId)
        {
            pedidoDTO.idOficina = oficinaId;
            return CreateCoreAsync(pedidoDTO, oficinaId);
        }

        public override async Task Update(PedidoDTO pedidoDTO, int id)
        {
            var existing = await _pedidoRepository.GetByIdWithDetails(id);
            if (existing is null)
                throw new KeyNotFoundException($"Pedido com id {id} nao encontrado.");

            var oficinaId = pedidoDTO.idOficina > 0
                ? pedidoDTO.idOficina
                : existing.idOficina;
            pedidoDTO.idOficina = oficinaId;
            await UpdateCoreAsync(existing, pedidoDTO, id, oficinaId);
        }

        public async Task UpdateForOficina(PedidoDTO pedidoDTO, int id, int oficinaId)
        {
            var existing = await _pedidoRepository.GetByIdForOficina(id, oficinaId);
            if (existing is null)
                throw new KeyNotFoundException($"Pedido com id {id} nao encontrado.");

            pedidoDTO.idOficina = oficinaId;
            await UpdateCoreAsync(existing, pedidoDTO, id, oficinaId);
        }

        public async Task<PedidoDTO> UpdateStatus(
            int id,
            SIGO.Objects.Enums.Status status,
            CancellationToken cancellationToken = default)
        {
            EnsureValidStatus(status);
            var existing = await _pedidoRepository.GetByIdWithDetails(id);
            if (existing is null)
                throw new KeyNotFoundException($"Pedido com id {id} nao encontrado.");

            existing.Status = status;
            await _pedidoRepository.SaveChanges(cancellationToken);
            return _mapper.Map<PedidoDTO>(existing);
        }

        public async Task<PedidoDTO> UpdateStatusForOficina(
            int id,
            SIGO.Objects.Enums.Status status,
            int oficinaId,
            CancellationToken cancellationToken = default)
        {
            EnsureValidStatus(status);
            var existing = await _pedidoRepository.GetByIdForOficina(id, oficinaId);
            if (existing is null)
                throw new KeyNotFoundException($"Pedido com id {id} nao encontrado para esta oficina.");

            existing.Status = status;
            await _pedidoRepository.SaveChanges(cancellationToken);
            return _mapper.Map<PedidoDTO>(existing);
        }

        private async Task CreateCoreAsync(PedidoDTO pedidoDTO, int oficinaId)
        {
            NormalizeCollections(pedidoDTO);
            var catalog = await ValidateTenantReferencesAsync(pedidoDTO, oficinaId);
            ApplyFinancialValues(pedidoDTO, catalog);

            pedidoDTO.Id = 0;
            pedidoDTO.idOficina = oficinaId;
            pedidoDTO.Status = SIGO.Objects.Enums.Status.Pendente;
            NormalizeLineIdentifiers(pedidoDTO, pedidoId: 0);

            var entity = _mapper.Map<Pedido>(pedidoDTO);
            await _pedidoRepository.Add(entity);

            pedidoDTO.Id = entity.Id;
            NormalizeLineIdentifiers(pedidoDTO, entity.Id);
        }

        private async Task UpdateCoreAsync(
            Pedido existing,
            PedidoDTO pedidoDTO,
            int id,
            int oficinaId)
        {
            NormalizeCollections(pedidoDTO);
            var catalog = await ValidateTenantReferencesAsync(pedidoDTO, oficinaId);
            ApplyFinancialValues(pedidoDTO, catalog);

            pedidoDTO.Id = id;
            pedidoDTO.idOficina = oficinaId;
            pedidoDTO.Status = existing.Status;
            NormalizeLineIdentifiers(pedidoDTO, id);
            ApplyUpdate(existing, pedidoDTO);

            var pieces = pedidoDTO.Pedido_Pecas.Select(piece => new Pedido_Peca
            {
                IdPedido = id,
                IdPeca = piece.IdPeca,
                Quantidade = piece.Quantidade,
                ValorUnitario = piece.ValorUnitario,
                DataInstalacao = piece.DataInstalacao,
                Estado = piece.Estado,
                Observacao = piece.Observacao
            }).ToArray();
            var services = pedidoDTO.Pedido_Servicos.Select(service => new Pedido_Servico
            {
                IdPedido = id,
                IdServico = service.IdServico,
                QuantVezes = service.QuantVezes,
                ValorUnitario = service.ValorUnitario
            }).ToArray();

            await _pedidoRepository.SaveWithDetailsAsync(existing, pieces, services);
        }

        private async Task<OrderCatalog> ValidateTenantReferencesAsync(
            PedidoDTO pedidoDTO,
            int oficinaId)
        {
            var errors = new List<ValidationError>();

            if (oficinaId <= 0)
                errors.Add(new ValidationError(nameof(PedidoDTO.idOficina), "Oficina invalida."));

            if (_clienteRepository is not null)
            {
                var linked = await _clienteRepository.ExistsInOficina(pedidoDTO.idCliente, oficinaId);
                if (!linked)
                {
                    errors.Add(new ValidationError(
                        nameof(PedidoDTO.idCliente),
                        "Cliente nao esta vinculado a oficina do pedido."));
                }
            }

            if (_funcionarioRepository is not null)
            {
                var belongsToOffice = await _funcionarioRepository.ExistsInOficina(
                    pedidoDTO.idFuncionario,
                    oficinaId);
                if (!belongsToOffice)
                {
                    errors.Add(new ValidationError(
                        nameof(PedidoDTO.idFuncionario),
                        "Funcionario nao pertence a oficina do pedido."));
                }
            }

            if (_veiculoRepository is not null)
            {
                var vehicle = await _veiculoRepository.GetById(pedidoDTO.idVeiculo);
                if (vehicle is null)
                {
                    errors.Add(new ValidationError(nameof(PedidoDTO.idVeiculo), "Veiculo nao encontrado."));
                }
                else if (vehicle.ClienteId != pedidoDTO.idCliente)
                {
                    errors.Add(new ValidationError(
                        nameof(PedidoDTO.idVeiculo),
                        "Veiculo nao pertence ao cliente informado."));
                }
            }

            var pieces = await AddPieceErrorsAsync(pedidoDTO, oficinaId, errors);
            var services = await AddServiceErrorsAsync(pedidoDTO, oficinaId, errors);

            if (errors.Count > 0)
                throw new BusinessValidationException(errors);

            return new OrderCatalog(pieces, services);
        }

        private async Task<IReadOnlyList<Peca>> AddPieceErrorsAsync(
            PedidoDTO pedidoDTO,
            int oficinaId,
            ICollection<ValidationError> errors)
        {
            var ids = pedidoDTO.Pedido_Pecas.Select(piece => piece.IdPeca).ToArray();
            if (ids.Distinct().Count() != ids.Length)
            {
                errors.Add(new ValidationError(
                    nameof(PedidoDTO.Pedido_Pecas),
                    "Uma peca nao pode aparecer mais de uma vez no pedido."));
                return Array.Empty<Peca>();
            }

            if (ids.Length == 0)
                return Array.Empty<Peca>();
            if (_pecaRepository is null)
            {
                errors.Add(new ValidationError(
                    nameof(PedidoDTO.Pedido_Pecas),
                    "Repositorio de pecas indisponivel para validar o pedido."));
                return Array.Empty<Peca>();
            }

            var pieces = await _pecaRepository.GetByIdsAsync(ids);
            var foundIds = pieces.Select(piece => piece.Id).ToHashSet();
            foreach (var missingId in ids.Where(id => !foundIds.Contains(id)))
            {
                errors.Add(new ValidationError(
                    nameof(PedidoDTO.Pedido_Pecas),
                    $"Peca {missingId} nao encontrada."));
            }

            foreach (var piece in pieces.Where(piece => piece.IdOficina != oficinaId))
            {
                errors.Add(new ValidationError(
                    nameof(PedidoDTO.Pedido_Pecas),
                    $"Peca {piece.Id} nao pertence a oficina do pedido."));
            }

            return pieces;
        }

        private async Task<IReadOnlyList<Servico>> AddServiceErrorsAsync(
            PedidoDTO pedidoDTO,
            int oficinaId,
            ICollection<ValidationError> errors)
        {
            var ids = pedidoDTO.Pedido_Servicos.Select(service => service.IdServico).ToArray();
            if (ids.Distinct().Count() != ids.Length)
            {
                errors.Add(new ValidationError(
                    nameof(PedidoDTO.Pedido_Servicos),
                    "Um servico nao pode aparecer mais de uma vez no pedido."));
                return Array.Empty<Servico>();
            }

            if (ids.Length == 0)
                return Array.Empty<Servico>();
            if (_servicoRepository is null)
            {
                errors.Add(new ValidationError(
                    nameof(PedidoDTO.Pedido_Servicos),
                    "Repositorio de servicos indisponivel para calcular o pedido."));
                return Array.Empty<Servico>();
            }

            var services = await _servicoRepository.GetByIdsAsync(ids);
            var foundIds = services.Select(service => service.Id).ToHashSet();
            foreach (var missingId in ids.Where(id => !foundIds.Contains(id)))
            {
                errors.Add(new ValidationError(
                    nameof(PedidoDTO.Pedido_Servicos),
                    $"Servico {missingId} nao encontrado."));
            }

            foreach (var service in services.Where(service => service.IdOficina != oficinaId))
            {
                errors.Add(new ValidationError(
                    nameof(PedidoDTO.Pedido_Servicos),
                    $"Servico {service.Id} nao pertence a oficina do pedido."));
            }

            return services;
        }

        private static void ApplyFinancialValues(PedidoDTO pedidoDTO, OrderCatalog catalog)
        {
            var errors = new List<ValidationError>();
            if (pedidoDTO.Pedido_Pecas.Any(line => line.ValorUnitario < 0m))
            {
                errors.Add(new ValidationError(
                    nameof(PedidoDTO.Pedido_Pecas),
                    "O valor unitario da peca nao pode ser negativo."));
            }

            foreach (var line in pedidoDTO.Pedido_Pecas)
                line.ValorUnitario = RoundMoney(line.ValorUnitario);

            var servicesById = catalog.Services.ToDictionary(service => service.Id);
            foreach (var line in pedidoDTO.Pedido_Servicos)
                line.ValorUnitario = RoundMoney(servicesById[line.IdServico].Valor);

            pedidoDTO.DescontoReais = RoundMoney(pedidoDTO.DescontoReais);
            pedidoDTO.DescontoServicoReais = RoundMoney(pedidoDTO.DescontoServicoReais);
            pedidoDTO.descontoPecaReais = RoundMoney(pedidoDTO.descontoPecaReais);
            pedidoDTO.DescontoPorcentagem = RoundPercentage(pedidoDTO.DescontoPorcentagem);
            pedidoDTO.DescontoServicoPorcentagem = RoundPercentage(
                pedidoDTO.DescontoServicoPorcentagem);
            pedidoDTO.DescontoPecaPorcentagem = RoundPercentage(
                pedidoDTO.DescontoPecaPorcentagem);

            if (pedidoDTO.ValorTotal < 0m)
            {
                errors.Add(new ValidationError(
                    nameof(PedidoDTO.ValorTotal),
                    "O valor total nao pode ser negativo."));
            }

            AddInvalidMoneyError(
                pedidoDTO.DescontoReais,
                nameof(PedidoDTO.DescontoReais),
                errors);
            AddInvalidMoneyError(
                pedidoDTO.DescontoServicoReais,
                nameof(PedidoDTO.DescontoServicoReais),
                errors);
            AddInvalidMoneyError(
                pedidoDTO.descontoPecaReais,
                nameof(PedidoDTO.descontoPecaReais),
                errors);
            AddInvalidPercentageError(
                pedidoDTO.DescontoPorcentagem,
                nameof(PedidoDTO.DescontoPorcentagem),
                errors);
            AddInvalidPercentageError(
                pedidoDTO.DescontoServicoPorcentagem,
                nameof(PedidoDTO.DescontoServicoPorcentagem),
                errors);
            AddInvalidPercentageError(
                pedidoDTO.DescontoPecaPorcentagem,
                nameof(PedidoDTO.DescontoPecaPorcentagem),
                errors);
            AddAmbiguousDiscountError(
                pedidoDTO.DescontoReais,
                pedidoDTO.DescontoPorcentagem,
                nameof(PedidoDTO.DescontoReais),
                "Informe o desconto geral em reais ou porcentagem, nunca os dois.",
                errors);
            AddAmbiguousDiscountError(
                pedidoDTO.DescontoServicoReais,
                pedidoDTO.DescontoServicoPorcentagem,
                nameof(PedidoDTO.DescontoServicoReais),
                "Informe o desconto de servicos em reais ou porcentagem, nunca os dois.",
                errors);
            AddAmbiguousDiscountError(
                pedidoDTO.descontoPecaReais,
                pedidoDTO.DescontoPecaPorcentagem,
                nameof(PedidoDTO.descontoPecaReais),
                "Informe o desconto de pecas em reais ou porcentagem, nunca os dois.",
                errors);

            var servicesSubtotal = RoundMoney(pedidoDTO.Pedido_Servicos.Sum(item => item.Subtotal));
            var piecesSubtotal = RoundMoney(pedidoDTO.Pedido_Pecas.Sum(item => item.Subtotal));
            AddExcessiveFixedDiscountError(
                pedidoDTO.DescontoServicoReais,
                servicesSubtotal,
                nameof(PedidoDTO.DescontoServicoReais),
                "O desconto de servicos nao pode superar o subtotal dos servicos.",
                errors);
            AddExcessiveFixedDiscountError(
                pedidoDTO.descontoPecaReais,
                piecesSubtotal,
                nameof(PedidoDTO.descontoPecaReais),
                "O desconto de pecas nao pode superar o subtotal das pecas.",
                errors);

            var servicesDiscount = CalculateDiscount(
                servicesSubtotal,
                pedidoDTO.DescontoServicoReais,
                pedidoDTO.DescontoServicoPorcentagem);
            var piecesDiscount = CalculateDiscount(
                piecesSubtotal,
                pedidoDTO.descontoPecaReais,
                pedidoDTO.DescontoPecaPorcentagem);
            var subtotalAfterCategoryDiscounts = RoundMoney(
                servicesSubtotal + piecesSubtotal - servicesDiscount - piecesDiscount);

            AddExcessiveFixedDiscountError(
                pedidoDTO.DescontoReais,
                subtotalAfterCategoryDiscounts,
                nameof(PedidoDTO.DescontoReais),
                "O desconto geral nao pode superar o valor restante do pedido.",
                errors);

            if (errors.Count > 0)
                throw new BusinessValidationException(errors);

            var generalDiscount = CalculateDiscount(
                subtotalAfterCategoryDiscounts,
                pedidoDTO.DescontoReais,
                pedidoDTO.DescontoPorcentagem);

            pedidoDTO.DescontoTotalReais = RoundMoney(
                servicesDiscount + piecesDiscount + generalDiscount);
        }

        private static decimal CalculateDiscount(
            decimal baseValue,
            decimal fixedDiscount,
            decimal percentageDiscount)
        {
            if (fixedDiscount > 0m)
                return fixedDiscount;

            return RoundMoney(baseValue * percentageDiscount / 100m);
        }

        private static void AddAmbiguousDiscountError(
            decimal fixedDiscount,
            decimal percentageDiscount,
            string field,
            string message,
            ICollection<ValidationError> errors)
        {
            if (fixedDiscount > 0m && percentageDiscount > 0m)
                errors.Add(new ValidationError(field, message));
        }

        private static void AddInvalidMoneyError(
            decimal value,
            string field,
            ICollection<ValidationError> errors)
        {
            if (value < 0m)
                errors.Add(new ValidationError(field, "O desconto nao pode ser negativo."));
        }

        private static void AddInvalidPercentageError(
            decimal value,
            string field,
            ICollection<ValidationError> errors)
        {
            if (value is < 0m or > 100m)
                errors.Add(new ValidationError(field, "O percentual deve estar entre 0 e 100."));
        }

        private static void AddExcessiveFixedDiscountError(
            decimal fixedDiscount,
            decimal baseValue,
            string field,
            string message,
            ICollection<ValidationError> errors)
        {
            if (fixedDiscount > baseValue)
                errors.Add(new ValidationError(field, message));
        }

        private static decimal RoundMoney(decimal value) =>
            Math.Round(value, 2, MidpointRounding.AwayFromZero);

        private static decimal RoundPercentage(decimal value) =>
            Math.Round(value, 2, MidpointRounding.AwayFromZero);

        private static void NormalizeCollections(PedidoDTO pedidoDTO)
        {
            pedidoDTO.Pedido_Pecas ??= new List<Pedido_PecaDTO>();
            pedidoDTO.Pedido_Servicos ??= new List<Pedido_ServicoDTO>();
        }

        private static void NormalizeLineIdentifiers(PedidoDTO pedidoDTO, int pedidoId)
        {
            foreach (var piece in pedidoDTO.Pedido_Pecas)
                piece.IdPedido = pedidoId;

            foreach (var service in pedidoDTO.Pedido_Servicos)
                service.IdPedido = pedidoId;
        }

        private static void ApplyUpdate(Pedido existing, PedidoDTO pedidoDTO)
        {
            if (pedidoDTO.idCliente > 0)
                existing.idCliente = pedidoDTO.idCliente;
            if (pedidoDTO.idFuncionario > 0)
                existing.idFuncionario = pedidoDTO.idFuncionario;
            if (pedidoDTO.idOficina > 0)
                existing.idOficina = pedidoDTO.idOficina;
            if (pedidoDTO.idVeiculo > 0)
                existing.idVeiculo = pedidoDTO.idVeiculo;

            existing.ValorTotal = pedidoDTO.ValorTotal;
            existing.DescontoReais = pedidoDTO.DescontoReais;
            existing.DescontoPorcentagem = pedidoDTO.DescontoPorcentagem;
            existing.DescontoTotalReais = pedidoDTO.DescontoTotalReais;
            existing.DescontoServicoPorcentagem = pedidoDTO.DescontoServicoPorcentagem;
            existing.DescontoServicoReais = pedidoDTO.DescontoServicoReais;
            existing.DescontoPecaPorcentagem = pedidoDTO.DescontoPecaPorcentagem;
            existing.descontoPecaReais = pedidoDTO.descontoPecaReais;
            existing.Observacao = pedidoDTO.Observacao;
            existing.DataInicio = pedidoDTO.DataInicio;
            existing.DataFim = pedidoDTO.DataFim;
        }

        private static void EnsureValidStatus(SIGO.Objects.Enums.Status status)
        {
            if (Enum.IsDefined(status))
                return;

            throw new BusinessValidationException(new[]
            {
                new ValidationError(nameof(PedidoDTO.Status), "Status de pedido invalido.")
            });
        }

        private sealed record OrderCatalog(
            IReadOnlyList<Peca> Pieces,
            IReadOnlyList<Servico> Services);
    }
}
