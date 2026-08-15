using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SIGO.Objects.Contracts;
using SIGO.Security;
using SIGO.Services.Interfaces;

namespace SIGO.Controllers
{
    [Route("api/v1/auditoria-funcionarios")]
    [ApiController]
    [Authorize(Policy = AuthorizationPolicies.FullAccess)]
    public class AuditoriaFuncionarioController : ControllerBase
    {
        private readonly IAuditoriaFuncionarioService _auditoriaService;

        public AuditoriaFuncionarioController(
            IAuditoriaFuncionarioService auditoriaService)
        {
            _auditoriaService = auditoriaService;
        }

        [HttpGet]
        public async Task<IActionResult> Get(
            [FromQuery] int? funcionarioId,
            [FromQuery] string? acao,
            [FromQuery] string? entidade,
            [FromQuery] DateTime? inicio,
            [FromQuery] DateTime? fim)
        {
            var auditorias = await _auditoriaService.Get(
                funcionarioId,
                acao,
                entidade,
                inicio,
                fim);

            var response = new Response
            {
                Code = ResponseEnum.SUCCESS,
                Message = "Auditoria dos funcionários listada com sucesso",
                Data = auditorias
            };

            return Ok(response);
        }

        [HttpGet("funcionario/{funcionarioId:int}")]
        public async Task<IActionResult> GetByFuncionario(
            int funcionarioId)
        {
            var auditorias = await _auditoriaService.Get(
                funcionarioId: funcionarioId);

            var response = new Response
            {
                Code = ResponseEnum.SUCCESS,
                Message = "Auditoria do funcionário listada com sucesso",
                Data = auditorias
            };

            return Ok(response);
        }
    }
}
