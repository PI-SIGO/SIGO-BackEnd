using AutoMapper;
using SIGO.Data.Interfaces;
using SIGO.Objects.Dtos.Entities;
using SIGO.Objects.Models;
using SIGO.Services.Interfaces;

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
    }
}
