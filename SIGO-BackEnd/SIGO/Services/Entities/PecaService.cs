using AutoMapper;
using SIGO.Data.Interfaces;
using SIGO.Objects.Dtos.Entities;
using SIGO.Objects.Models;
using SIGO.Services.Interfaces;
using SIGO.Validation;

namespace SIGO.Services.Entities
{
    public class PecaService : GenericService<Peca, PecaDTO>, IPecaService
    {
        private readonly IPecaRepository _pecaRepository;
        private readonly IMapper _mapper;

        public PecaService(IPecaRepository pecaRepositoy, IMapper mapper)
            : base(pecaRepositoy, mapper)
        {
            _pecaRepository = pecaRepositoy;
            _mapper = mapper;
        }

        public async Task<IEnumerable<PecaDTO>> GetByOficina(int oficinaId)
        {
            var entities = await _pecaRepository.GetByOficina(oficinaId);
            return _mapper.Map<IEnumerable<PecaDTO>>(entities);
        }

        public async Task<PecaDTO?> GetByIdForOficina(int id, int oficinaId)
        {
            var entity = await _pecaRepository.GetByIdForOficina(id, oficinaId);
            return _mapper.Map<PecaDTO?>(entity);
        }

        public async Task CreateForOficina(PecaDTO pecaDTO, int oficinaId)
        {
            pecaDTO.IdOficina = oficinaId;
            await base.Create(pecaDTO);
        }

        public override async Task Create(PecaDTO pecaDTO)
        {
            EnsureOficinaOwner(pecaDTO.IdOficina);
            await base.Create(pecaDTO);
        }

        public async Task UpdateForOficina(PecaDTO pecaDTO, int id, int oficinaId)
        {
            var existing = await _pecaRepository.GetByIdForOficina(id, oficinaId);
            if (existing is null)
                throw new KeyNotFoundException($"Peça com id {id} não encontrada.");

            pecaDTO.IdOficina = oficinaId;
            await Update(pecaDTO, id);
        }

        public override async Task Update(PecaDTO pecaDTO, int id)
        {
            var existing = await _pecaRepository.GetById(id);
            if (existing is null)
                throw new KeyNotFoundException($"Peça com id {id} não encontrada.");

            existing.Nome = pecaDTO.Nome;
            existing.Tipo = pecaDTO.Tipo;
            existing.Descricao = pecaDTO.Descricao;
            existing.Valor = pecaDTO.Valor;
            existing.Quantidade = pecaDTO.Quantidade;
            existing.Garantia = pecaDTO.Garantia;
            existing.Unidade = pecaDTO.Unidade;
            existing.IdMarca = pecaDTO.IdMarca;
            existing.DataAquisicao = pecaDTO.DataAquisicao;
            existing.Fornecedor = pecaDTO.Fornecedor;
            EnsureOficinaOwner(pecaDTO.IdOficina);
            existing.IdOficina = pecaDTO.IdOficina;
            await _pecaRepository.SaveChanges();
        }

        private static void EnsureOficinaOwner(int? oficinaId)
        {
            if (!oficinaId.HasValue || oficinaId.Value <= 0)
            {
                throw new BusinessValidationException(new[]
                {
                    new ValidationError(nameof(PecaDTO.IdOficina), "Peça deve estar vinculada a uma oficina.")
                });
            }
        }
    }
}
