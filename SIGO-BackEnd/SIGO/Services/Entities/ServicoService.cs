using AutoMapper;
using SIGO.Data.Interfaces;
using SIGO.Objects.Dtos.Entities;
using SIGO.Objects.Models;
using SIGO.Services.Interfaces;
using SIGO.Validation;

namespace SIGO.Services.Entities
{
    public class ServicoService : GenericService<Servico, ServicoDTO>, IServicoService
    {
        private readonly IServicoRepository _servicoRepository;
        private readonly IMapper _mapper;

        public ServicoService(IServicoRepository servicoRepository, IMapper mapper)
            : base(servicoRepository, mapper)
        {
            _servicoRepository = servicoRepository;
            _mapper = mapper;
        }
        public override async Task<IEnumerable<ServicoDTO>> GetAll()
        {
            var entities = await _servicoRepository.Get();
            return _mapper.Map<IEnumerable<ServicoDTO>>(entities);
        }

        public async Task<ServicoDTO?> GetByIdWithDetails(int id)
        {
            var entity = await _servicoRepository.GetByIdWithDetails(id);
            return _mapper.Map<ServicoDTO?>(entity);
        }

        public async Task<IEnumerable<ServicoDTO>> GetByNameWithDetails(string nome)
        {
            var entities = await _servicoRepository.GetByNameWithDetails(nome);
            return _mapper.Map<IEnumerable<ServicoDTO>>(entities);
        }

        public async Task<IEnumerable<ServicoDTO>> GetByNameWithDetailsForOficina(string nome, int oficinaId)
        {
            var entities = await _servicoRepository.GetByNameWithDetailsForOficina(nome, oficinaId);
            return _mapper.Map<IEnumerable<ServicoDTO>>(entities);
        }

        public async Task<ServicoDTO?> GetById(int id)
        {
            var entity = await _servicoRepository.GetById(id);
            return _mapper.Map<ServicoDTO?>(entity);
        }

        public async Task<IEnumerable<ServicoDTO>> GetByOficina(int oficinaId)
        {
            var entities = await _servicoRepository.GetByOficina(oficinaId);
            return _mapper.Map<IEnumerable<ServicoDTO>>(entities);
        }

        public async Task<ServicoDTO?> GetByIdForOficina(int id, int oficinaId)
        {
            var entity = await _servicoRepository.GetByIdForOficina(id, oficinaId);
            return _mapper.Map<ServicoDTO?>(entity);
        }

        public async Task<ServicoDTO?> GetByIdWithDetailsForOficina(int id, int oficinaId)
        {
            var entity = await _servicoRepository.GetByIdWithDetailsForOficina(id, oficinaId);
            return _mapper.Map<ServicoDTO?>(entity);
        }

        public override async Task Create(ServicoDTO servicoDTO)
        {
            EnsureOficinaOwner(servicoDTO.IdOficina);
            var servico = _mapper.Map<Servico>(servicoDTO);
            await _servicoRepository.Add(servico);
        }

        public async Task CreateForOficina(ServicoDTO servicoDTO, int oficinaId)
        {
            servicoDTO.IdOficina = oficinaId;
            await Create(servicoDTO);
        }

        public async Task UpdateForOficina(ServicoDTO servicoDTO, int id, int oficinaId)
        {
            var existing = await _servicoRepository.GetByIdForOficina(id, oficinaId);
            if (existing is null)
                throw new KeyNotFoundException($"Serviço com id {id} não encontrado.");

            servicoDTO.IdOficina = oficinaId;
            await Update(servicoDTO, id);
        }

        public override async Task Update(ServicoDTO servicoDTO, int id)
        {
            var existing = await _servicoRepository.GetById(id);
            if (existing is null)
                throw new KeyNotFoundException($"Serviço com id {id} não encontrado.");

            existing.Nome = servicoDTO.Nome;
            existing.Descricao = servicoDTO.Descricao;
            existing.Valor = servicoDTO.Valor;
            existing.Garantia = servicoDTO.Garantia;
            EnsureOficinaOwner(servicoDTO.IdOficina);
            existing.IdOficina = servicoDTO.IdOficina;
            await _servicoRepository.SaveChanges();
        }

        private static void EnsureOficinaOwner(int? oficinaId)
        {
            if (!oficinaId.HasValue || oficinaId.Value <= 0)
            {
                throw new BusinessValidationException(new[]
                {
                    new ValidationError(nameof(ServicoDTO.IdOficina), "Serviço deve estar vinculado a uma oficina.")
                });
            }
        }
    }
}
