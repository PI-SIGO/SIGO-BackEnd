using AutoMapper;
using SIGO.Data.Interfaces;
using SIGO.Objects.Dtos.Entities;
using SIGO.Objects.Models;
using SIGO.Security;
using SIGO.Services.Interfaces;

namespace SIGO.Services.Entities
{
    public class AuditoriaFuncionarioService
        : IAuditoriaFuncionarioService
    {
        private readonly IAuditoriaFuncionarioRepository _repository;
        private readonly ICurrentUserService _currentUserService;
        private readonly IMapper _mapper;

        public AuditoriaFuncionarioService(
            IAuditoriaFuncionarioRepository repository,
            ICurrentUserService currentUserService,
            IMapper mapper)
        {
            _repository = repository;
            _currentUserService = currentUserService;
            _mapper = mapper;
        }

        public async Task Registrar(
            string acao,
            string entidade,
            int? entidadeId,
            string? descricao = null)
        {
            // SOMENTE funcionário normal entra na auditoria.
            if (!_currentUserService.IsInRole(SystemRoles.Funcionario))
                return;

            if (!_currentUserService.UserId.HasValue)
                return;

            var auditoria = new AuditoriaFuncionario
            {
                FuncionarioId = _currentUserService.UserId.Value,

                FuncionarioNome =
                    _currentUserService.UserName
                    ?? "Funcionário não identificado",

                Acao = acao.Trim().ToUpperInvariant(),

                Entidade = entidade.Trim(),

                EntidadeId = entidadeId,

                Descricao = descricao,

                DataHora = DateTime.UtcNow
            };

            await _repository.Add(auditoria);
        }

        public async Task<IEnumerable<AuditoriaFuncionarioDTO>> Get(
            int? funcionarioId = null,
            string? acao = null,
            string? entidade = null,
            DateTime? inicio = null,
            DateTime? fim = null)
        {
            int? oficinaId = null;
            if (!_currentUserService.IsInRole(SystemRoles.Admin))
            {
                if (!_currentUserService.IsInRole(SystemRoles.Oficina) ||
                    !_currentUserService.OficinaId.HasValue)
                {
                    throw new UnauthorizedAccessException();
                }

                oficinaId = _currentUserService.OficinaId.Value;
            }

            var auditorias = await _repository.Get(
                funcionarioId,
                acao,
                entidade,
                inicio,
                fim,
                oficinaId);

            return _mapper.Map<IEnumerable<AuditoriaFuncionarioDTO>>(
                auditorias);
        }
    }
}
