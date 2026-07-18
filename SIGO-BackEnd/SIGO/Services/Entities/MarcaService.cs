using AutoMapper;
using SIGO.Data.Interfaces;
using SIGO.Objects.Models;
using SIGO.Objects.Dtos.Entities;
using SIGO.Services.Interfaces;

namespace SIGO.Services.Entities
{
    public class MarcaService : IMarcaService
    {
        private readonly IMarcaRepository _marcaRepository;
        private readonly IMapper _mapper;

        public MarcaService(IMarcaRepository marcaRepository, IMapper mapper)
        {
            _marcaRepository = marcaRepository;
            _mapper = mapper;
        }

        public async Task<IEnumerable<MarcaDTO>> GetAll()
        {
            var marcas = await _marcaRepository.Get();
            return _mapper.Map<IEnumerable<MarcaDTO>>(marcas);
        }

        public async Task<MarcaDTO?> GetById(int idMarca)
        {
            var marca = await _marcaRepository.GetById(idMarca);
            return marca == null ? null : _mapper.Map<MarcaDTO>(marca);
        }

        public async Task<IEnumerable<MarcaDTO>> GetByName(string nomeMarca)
        {
            var marcas = await _marcaRepository.GetByName(nomeMarca);
            return _mapper.Map<IEnumerable<MarcaDTO>>(marcas);
        }

        public async Task Create(MarcaDTO marcaDTO)
        {
            await CreateMarca(marcaDTO);
        }

        public async Task<MarcaDTO> CreateMarca(MarcaDTO marcaDTO)
        {
            marcaDTO.Id = 0;
            var marca = _mapper.Map<Marca>(marcaDTO);
            await _marcaRepository.Add(marca);
            return _mapper.Map<MarcaDTO>(marca);
        }

        public async Task Update(MarcaDTO marcaDTO, int idMarca)
        {
            await UpdateMarca(marcaDTO, idMarca);
        }

        public async Task<MarcaDTO> UpdateMarca(MarcaDTO marcaDTO, int idMarca)
        {
            var marca = await _marcaRepository.GetById(idMarca);
            if (marca == null)
                throw new KeyNotFoundException($"Marca com id {idMarca} não encontrada.");

            marcaDTO.Id = idMarca;
            _mapper.Map(marcaDTO, marca);
            await _marcaRepository.Update(marca);
            return _mapper.Map<MarcaDTO>(marca);
        }

        public async Task Remove(int idMarca)
        {
            var marca = await _marcaRepository.GetById(idMarca);
            if (marca == null)
                throw new KeyNotFoundException($"Marca com id {idMarca} não encontrada.");

            await _marcaRepository.Remove(marca);
        }
    }
}
