using AutoMapper;
using SIGO.Data.Interfaces;
using SIGO.Data.Repositories;
using SIGO.Objects.Dtos.Entities;
using SIGO.Objects.Models;
using SIGO.Services.Interfaces;

namespace SIGO.Services.Entities
{
    public class TelefoneService : GenericService<Telefone, TelefoneDTO>, ITelefoneService
    {
        private readonly ITelefoneRepository _telefoneRepository;
        private readonly IMapper _mapper;

        public TelefoneService(ITelefoneRepository telefoneRepository, IMapper mapper)
            : base(telefoneRepository, mapper)
        {
            _telefoneRepository = telefoneRepository;
            _mapper = mapper;
        }

        public async Task<IEnumerable<TelefoneDTO>> GetTelefoneByNome(string nome)
        {
            var entities = await _telefoneRepository.GetTelefoneByNome(nome);
            return _mapper.Map<IEnumerable<TelefoneDTO>>(entities);
        }

        public async Task<IEnumerable<TelefoneDTO>> GetTelefoneByNomeForOficina(string nome, int oficinaId)
        {
            var entities = await _telefoneRepository.GetTelefoneByNomeForOficina(nome, oficinaId);
            return _mapper.Map<IEnumerable<TelefoneDTO>>(entities);
        }

        public async Task<TelefoneDTO> CreateTelefone(TelefoneDTO telefoneDTO)
        {
            telefoneDTO.Id = 0;
            var entity = _mapper.Map<Telefone>(telefoneDTO);
            await _telefoneRepository.Add(entity);
            return _mapper.Map<TelefoneDTO>(entity);
        }

        public async Task<TelefoneDTO> UpdateTelefone(TelefoneDTO telefoneDTO, int id)
        {
            var entity = await _telefoneRepository.GetById(id);
            if (entity is null)
                throw new KeyNotFoundException($"Telefone com id {id} não encontrado.");

            entity.DDD = telefoneDTO.DDD;
            entity.Numero = telefoneDTO.Numero;
            entity.ClienteId = telefoneDTO.ClienteId;
            await _telefoneRepository.SaveChanges();
            return _mapper.Map<TelefoneDTO>(entity);
        }
    }
}
