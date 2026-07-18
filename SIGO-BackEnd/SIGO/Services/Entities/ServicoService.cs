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
        private readonly IFuncionarioRepository? _funcionarioRepository;

        public ServicoService(
            IServicoRepository servicoRepository,
            IMapper mapper,
            IFuncionarioRepository? funcionarioRepository = null)
            : base(servicoRepository, mapper)
        {
            _servicoRepository = servicoRepository;
            _mapper = mapper;
            _funcionarioRepository = funcionarioRepository;
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

        public async Task<IEnumerable<ServicoDTO>> GetByNameWithDetailsForOficina(
            string nome,
            int oficinaId)
        {
            var entities = await _servicoRepository.GetByNameWithDetailsForOficina(nome, oficinaId);
            return _mapper.Map<IEnumerable<ServicoDTO>>(entities);
        }

        public override async Task<ServicoDTO?> GetById(int id)
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

        public async Task CreateForOficina(ServicoDTO servicoDTO, int oficinaId)
        {
            servicoDTO.IdOficina = oficinaId;
            await Create(servicoDTO);
        }

        public override async Task Create(ServicoDTO servicoDTO)
        {
            var oficinaId = EnsureOficinaOwner(servicoDTO.IdOficina);
            NormalizeEmployees(servicoDTO);
            await ValidateEmployeesAsync(servicoDTO.Funcionario_Servicos, oficinaId);

            servicoDTO.Id = 0;
            foreach (var employee in servicoDTO.Funcionario_Servicos)
                employee.IdServico = 0;

            var entity = _mapper.Map<Servico>(servicoDTO);
            await _servicoRepository.Add(entity);

            servicoDTO.Id = entity.Id;
            foreach (var employee in servicoDTO.Funcionario_Servicos)
                employee.IdServico = entity.Id;
        }

        public async Task UpdateForOficina(ServicoDTO servicoDTO, int id, int oficinaId)
        {
            var existing = await _servicoRepository.GetByIdForOficina(id, oficinaId);
            if (existing is null)
                throw new KeyNotFoundException($"Servico com id {id} nao encontrado.");

            servicoDTO.IdOficina = oficinaId;
            await Update(servicoDTO, id);
        }

        public override async Task Update(ServicoDTO servicoDTO, int id)
        {
            var existing = await _servicoRepository.GetById(id);
            if (existing is null)
                throw new KeyNotFoundException($"Servico com id {id} nao encontrado.");

            var oficinaId = EnsureOficinaOwner(servicoDTO.IdOficina);
            NormalizeEmployees(servicoDTO);
            await ValidateEmployeesAsync(servicoDTO.Funcionario_Servicos, oficinaId);

            servicoDTO.Id = id;
            existing.Id = id;
            existing.Nome = servicoDTO.Nome;
            existing.Descricao = servicoDTO.Descricao;
            existing.Valor = servicoDTO.Valor;
            existing.Garantia = servicoDTO.Garantia;
            existing.IdOficina = oficinaId;

            var employees = servicoDTO.Funcionario_Servicos.Select(employee => new Funcionario_Servico
            {
                IdFuncionario = employee.IdFuncionario,
                IdServico = id,
                TempoDec = employee.TempoDec
            }).ToArray();

            await _servicoRepository.SaveWithEmployeesAsync(existing, employees);
            foreach (var employee in servicoDTO.Funcionario_Servicos)
                employee.IdServico = id;
        }

        private async Task ValidateEmployeesAsync(
            IReadOnlyCollection<Funcionario_ServicoDTO> employees,
            int oficinaId)
        {
            var duplicate = employees
                .GroupBy(employee => employee.IdFuncionario)
                .FirstOrDefault(group => group.Count() > 1);
            if (duplicate is not null)
            {
                throw new BusinessValidationException(new[]
                {
                    new ValidationError(
                        nameof(ServicoDTO.Funcionario_Servicos),
                        $"Funcionario {duplicate.Key} foi informado mais de uma vez.")
                });
            }

            if (_funcionarioRepository is null)
                return;

            var errors = new List<ValidationError>();
            foreach (var employee in employees)
            {
                if (!await _funcionarioRepository.ExistsInOficina(employee.IdFuncionario, oficinaId))
                {
                    errors.Add(new ValidationError(
                        nameof(ServicoDTO.Funcionario_Servicos),
                        $"Funcionario {employee.IdFuncionario} nao pertence a oficina do servico."));
                }
            }

            if (errors.Count > 0)
                throw new BusinessValidationException(errors);
        }

        private static int EnsureOficinaOwner(int? oficinaId)
        {
            if (oficinaId.HasValue && oficinaId.Value > 0)
                return oficinaId.Value;

            throw new BusinessValidationException(new[]
            {
                new ValidationError(nameof(ServicoDTO.IdOficina), "Servico deve estar vinculado a uma oficina.")
            });
        }

        private static void NormalizeEmployees(ServicoDTO servicoDTO)
        {
            servicoDTO.Funcionario_Servicos ??= new List<Funcionario_ServicoDTO>();
        }
    }
}
