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
    }
}
