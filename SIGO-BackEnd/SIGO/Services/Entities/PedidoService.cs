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

        public PedidoService(
            IPedidoRepository pedidoRepository,
            IMapper mapper,
            IClienteRepository? clienteRepository = null,
            IFuncionarioRepository? funcionarioRepository = null,
            IVeiculoRepository? veiculoRepository = null)
            : base(pedidoRepository, mapper)
        {
            _pedidoRepository = pedidoRepository;
            _mapper = mapper;
            _clienteRepository = clienteRepository;
            _funcionarioRepository = funcionarioRepository;
            _veiculoRepository = veiculoRepository;
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

        public async Task<PedidoDTO?> GetByIdForOficina(int id, int oficinaId)
        {
            var pedido = await _pedidoRepository.GetByIdForOficina(id, oficinaId);
            return _mapper.Map<PedidoDTO?>(pedido);
        }

        public async Task CreateForOficina(PedidoDTO pedidoDTO, int oficinaId)
        {
            await ValidateTenantReferences(pedidoDTO, oficinaId);
            pedidoDTO.idOficina = oficinaId;
            await base.Create(pedidoDTO);
        }

        public async Task UpdateForOficina(PedidoDTO pedidoDTO, int id, int oficinaId)
        {
            var existing = await _pedidoRepository.GetByIdForOficina(id, oficinaId);
            if (existing == null)
                throw new KeyNotFoundException($"Pedido com id {id} não encontrado.");

            await ValidateTenantReferences(pedidoDTO, oficinaId);
            pedidoDTO.Id = id;
            pedidoDTO.idOficina = oficinaId;
            ApplyUpdate(existing, pedidoDTO);
            await _pedidoRepository.SaveChanges();
        }

        public override async Task Update(PedidoDTO pedidoDTO, int id)
        {
            var existing = await _pedidoRepository.GetById(id);
            if (existing == null)
                throw new KeyNotFoundException($"Pedido com id {id} não encontrado.");

            pedidoDTO.Id = id;
            ApplyUpdate(existing, pedidoDTO);
            await _pedidoRepository.SaveChanges();
        }

        private async Task ValidateTenantReferences(PedidoDTO pedidoDTO, int oficinaId)
        {
            var errors = new List<ValidationError>();

            if (_clienteRepository != null)
            {
                var clienteVinculado = await _clienteRepository.ExistsInOficina(pedidoDTO.idCliente, oficinaId);
                if (!clienteVinculado)
                    errors.Add(new ValidationError(nameof(PedidoDTO.idCliente), "Cliente não está vinculado à oficina autenticada."));
            }

            if (_funcionarioRepository != null)
            {
                var funcionarioVinculado = await _funcionarioRepository.ExistsInOficina(pedidoDTO.idFuncionario, oficinaId);
                if (!funcionarioVinculado)
                    errors.Add(new ValidationError(nameof(PedidoDTO.idFuncionario), "Funcionário não pertence à oficina autenticada."));
            }

            if (_veiculoRepository != null)
            {
                var veiculo = await _veiculoRepository.GetById(pedidoDTO.idVeiculo);
                if (veiculo is null)
                {
                    errors.Add(new ValidationError(nameof(PedidoDTO.idVeiculo), "Veículo não encontrado."));
                }
                else if (veiculo.ClienteId != pedidoDTO.idCliente)
                {
                    errors.Add(new ValidationError(nameof(PedidoDTO.idVeiculo), "Veículo não pertence ao cliente informado."));
                }
            }

            if (errors.Count > 0)
                throw new BusinessValidationException(errors);
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
    }
}
