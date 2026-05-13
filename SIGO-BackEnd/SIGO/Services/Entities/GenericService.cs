using AutoMapper;
using SIGO.Data.Interfaces;
using SIGO.Services.Interfaces;

namespace SIGO.Services.Entities
{
    public class GenericService<T, TDto> : IGenericService<T, TDto> where T : class where TDto : class
    {
        private readonly IGenericRepository<T> _repository;
        private readonly IMapper _mapper;

        public GenericService(IGenericRepository<T> repository, IMapper mapper)
        {
            _repository = repository;
            _mapper = mapper;
        }

        public virtual async Task<IEnumerable<TDto>> GetAll()
        {
            var entities = await _repository.Get();
            return _mapper.Map<IEnumerable<TDto>>(entities);
        }

        public async Task<TDto> GetById(int id)
        {
            var entity = await _repository.GetById(id);
            return _mapper.Map<TDto>(entity);
        }

        public virtual async Task Create(TDto entityDTO)
        {

            var entity = _mapper.Map<T>(entityDTO);
            await _repository.Add(entity);
        }

        public virtual async Task Update(TDto entityDTO, int id)
        {
            var existingEntity = await _repository.GetById(id);

            if (existingEntity == null)
            {
                throw new KeyNotFoundException($"Entity with id {id} not found.");
            }

            var idProperty = typeof(TDto).GetProperty("Id");
            if (idProperty != null && idProperty.CanWrite)
            {
                idProperty.SetValue(entityDTO, id);
            }

            var entity = _mapper.Map<T>(entityDTO);
            await _repository.Update(entity);
        }

        public async Task Remove(int id)
        {
            var entity = await _repository.GetById(id);
            if (entity == null)
            {
                throw new KeyNotFoundException($"Entidade com id: {id} não encontrado");
            }

            await _repository.Remove(entity);
        }
    }
}
