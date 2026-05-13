using AutoMapper;
using SIGO.Data.Interfaces;
using SIGO.Objects.Dtos.Entities;
using SIGO.Objects.Models;
using SIGO.Services.Interfaces;
using SIGO.Validation;

namespace SIGO.Services.Entities
{
    public class VeiculoService : GenericService<Veiculo, VeiculoDTO>, IVeiculoService
    {
        private readonly IVeiculoRepository _veiculoRepository;
        private readonly IMapper _mapper;
        private readonly IClienteRepository? _clienteRepository;

        public VeiculoService(
            IVeiculoRepository veiculoRepository,
            IMapper mapper,
            IClienteRepository? clienteRepository = null)
            : base(veiculoRepository, mapper)
        {
            _veiculoRepository = veiculoRepository;
            _mapper = mapper;
            _clienteRepository = clienteRepository;
        }

        public async Task<IEnumerable<VeiculoDTO>> GetByPlaca(string placa)
        {
            var entity = await _veiculoRepository.GetByPlaca(placa);
            return _mapper.Map<IEnumerable<VeiculoDTO>>(entity);
        }

        public async Task<IEnumerable<VeiculoDTO>> GetByPlacaForCliente(string placa, int clienteId)
        {
            var entity = await _veiculoRepository.GetByPlacaForCliente(placa, clienteId);
            return _mapper.Map<IEnumerable<VeiculoDTO>>(entity);
        }

        public async Task<IEnumerable<VeiculoDTO>> GetByPlacaForOficina(string placa, int oficinaId)
        {
            var entity = await _veiculoRepository.GetByPlacaForOficina(placa, oficinaId);
            return _mapper.Map<IEnumerable<VeiculoDTO>>(entity);
        }

        public async Task<IEnumerable<VeiculoDTO>> GetByTipo(string tipo)
        {
            var entities = await _veiculoRepository.GetByTipo(tipo);
            return _mapper.Map<IEnumerable<VeiculoDTO>>(entities);
        }

        public async Task<IEnumerable<VeiculoDTO>> GetByTipoForCliente(string tipo, int clienteId)
        {
            var entities = await _veiculoRepository.GetByTipoForCliente(tipo, clienteId);
            return _mapper.Map<IEnumerable<VeiculoDTO>>(entities);
        }

        public async Task<IEnumerable<VeiculoDTO>> GetByTipoForOficina(string tipo, int oficinaId)
        {
            var entities = await _veiculoRepository.GetByTipoForOficina(tipo, oficinaId);
            return _mapper.Map<IEnumerable<VeiculoDTO>>(entities);
        }

        public async Task<IEnumerable<VeiculoDTO>> GetByCliente(int clienteId)
        {
            var entities = await _veiculoRepository.GetByCliente(clienteId);
            return _mapper.Map<IEnumerable<VeiculoDTO>>(entities);
        }

        public async Task<IEnumerable<VeiculoDTO>> GetByOficina(int oficinaId)
        {
            var entities = await _veiculoRepository.GetByOficina(oficinaId);
            return _mapper.Map<IEnumerable<VeiculoDTO>>(entities);
        }

        public async Task<VeiculoDTO?> GetById(int id)
        {
            var entity = await _veiculoRepository.GetById(id);
            return _mapper.Map<VeiculoDTO?>(entity);
        }

        public async Task<VeiculoDTO?> GetByIdForCliente(int id, int clienteId)
        {
            var entity = await _veiculoRepository.GetByIdForCliente(id, clienteId);
            return _mapper.Map<VeiculoDTO?>(entity);
        }

        public async Task<VeiculoDTO?> GetByIdForOficina(int id, int oficinaId)
        {
            var entity = await _veiculoRepository.GetByIdForOficina(id, oficinaId);
            return _mapper.Map<VeiculoDTO?>(entity);
        }

        public async Task CreateForCliente(VeiculoDTO veiculoDto, int clienteId)
        {
            veiculoDto.ClienteId = clienteId;
            await base.Create(veiculoDto);
        }

        public async Task CreateForOficina(VeiculoDTO veiculoDto, int oficinaId)
        {
            await EnsureClienteVinculado(veiculoDto.ClienteId, oficinaId);
            await base.Create(veiculoDto);
        }

        public async Task UpdateVeiculo(VeiculoDTO veiculoDto, int id)
        {
            var existingEntity = await _veiculoRepository.GetById(id);

            if (existingEntity == null)
                throw new KeyNotFoundException($"Veículo com id {id} não encontrado.");

            ApplyUpdate(existingEntity, veiculoDto, preserveClienteIdWhenMissing: true);
            await _veiculoRepository.SaveChanges();
        }

        public async Task UpdateVeiculoForCliente(VeiculoDTO veiculoDto, int id, int clienteId)
        {
            var existingEntity = await _veiculoRepository.GetByIdForCliente(id, clienteId);

            if (existingEntity == null)
                throw new KeyNotFoundException($"Veículo com id {id} não encontrado.");

            veiculoDto.ClienteId = clienteId;
            ApplyUpdate(existingEntity, veiculoDto, preserveClienteIdWhenMissing: false);
            await _veiculoRepository.SaveChanges();
        }

        public async Task UpdateVeiculoForOficina(VeiculoDTO veiculoDto, int id, int oficinaId)
        {
            var existingEntity = await _veiculoRepository.GetByIdForOficina(id, oficinaId);

            if (existingEntity == null)
                throw new KeyNotFoundException($"Veículo com id {id} não encontrado.");

            await EnsureClienteVinculado(veiculoDto.ClienteId, oficinaId);

            ApplyUpdate(existingEntity, veiculoDto, preserveClienteIdWhenMissing: false);
            await _veiculoRepository.SaveChanges();
        }

        private async Task EnsureClienteVinculado(int clienteId, int oficinaId)
        {
            if (_clienteRepository == null)
                return;

            var clienteVinculado = await _clienteRepository.ExistsInOficina(clienteId, oficinaId);
            if (!clienteVinculado)
            {
                throw new BusinessValidationException(new[]
                {
                    new ValidationError(nameof(VeiculoDTO.ClienteId), "Cliente não está vinculado à oficina autenticada.")
                });
            }
        }

        private static void ApplyUpdate(Veiculo existing, VeiculoDTO veiculoDto, bool preserveClienteIdWhenMissing)
        {
            existing.NomeVeiculo = veiculoDto.NomeVeiculo;
            existing.TipoVeiculo = veiculoDto.TipoVeiculo;
            existing.PlacaVeiculo = veiculoDto.PlacaVeiculo;
            existing.ChassiVeiculo = veiculoDto.ChassiVeiculo;
            existing.AnoFab = veiculoDto.AnoFab;
            existing.Quilometragem = veiculoDto.Quilometragem;
            existing.Combustivel = veiculoDto.Combustivel;
            existing.Seguro = veiculoDto.Seguro;
            existing.Cor = veiculoDto.Cor;

            if (!preserveClienteIdWhenMissing || veiculoDto.ClienteId > 0)
                existing.ClienteId = veiculoDto.ClienteId;
        }

    }
}
