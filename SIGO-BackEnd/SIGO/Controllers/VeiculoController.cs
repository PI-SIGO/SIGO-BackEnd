using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SIGO.Objects.Contracts;
using SIGO.Objects.Dtos.Entities;
using SIGO.Security;
using SIGO.Services.Interfaces;
using SIGO.Validation;

namespace SIGO.Controllers
{
    [Route("api/veiculos")]
    [ApiController]
    [Authorize(Policy = AuthorizationPolicies.SelfServiceAccess)]
    public class VeiculoController : ControllerBase
    {
        private readonly IVeiculoService _veiculoService;
        private readonly ICurrentUserService _currentUserService;
        private readonly Response _response;

        public VeiculoController(
            IVeiculoService veiculoService,
            IMapper mapper,
            ICurrentUserService currentUserService)
        {
            _veiculoService = veiculoService;
            _currentUserService = currentUserService;
            _response = new Response();
        }

        [HttpGet]
        [Authorize(Roles = $"{SystemRoles.Admin},{SystemRoles.Oficina},{SystemRoles.Funcionario},{SystemRoles.Cliente}")]
        public async Task<IActionResult> Get()
        {
            IEnumerable<VeiculoDTO> veiculos;
            if (_currentUserService.IsInRole(SystemRoles.Cliente))
            {
                var clienteId = _currentUserService.UserId;
                if (!clienteId.HasValue)
                    return Forbid();

                veiculos = await _veiculoService.GetByCliente(clienteId.Value);
            }
            else if (_currentUserService.IsInRole(SystemRoles.Oficina) || _currentUserService.IsInRole(SystemRoles.Funcionario))
            {
                var oficinaId = _currentUserService.OficinaId;
                if (!oficinaId.HasValue)
                    return Forbid();

                veiculos = await _veiculoService.GetByOficina(oficinaId.Value);
            }
            else
            {
                veiculos = await _veiculoService.GetAll();
            }

            _response.Code = ResponseEnum.SUCCESS;
            _response.Data = veiculos;
            _response.Message = "Veículos listados com sucesso";

            return Ok(_response);
        }

        [HttpGet("placa/{placa}")]
        [Authorize(Roles = $"{SystemRoles.Admin},{SystemRoles.Oficina},{SystemRoles.Funcionario},{SystemRoles.Cliente}")]
        public async Task<IActionResult> GetByPlaca(string placa)
        {
            IEnumerable<VeiculoDTO> veiculos;
            if (_currentUserService.IsInRole(SystemRoles.Cliente))
            {
                var clienteId = _currentUserService.UserId;
                if (!clienteId.HasValue)
                    return Forbid();

                veiculos = await _veiculoService.GetByPlacaForCliente(placa, clienteId.Value);
            }
            else if (_currentUserService.IsInRole(SystemRoles.Oficina) || _currentUserService.IsInRole(SystemRoles.Funcionario))
            {
                var oficinaId = _currentUserService.OficinaId;
                if (!oficinaId.HasValue)
                    return Forbid();

                veiculos = await _veiculoService.GetByPlacaForOficina(placa, oficinaId.Value);
            }
            else
            {
                veiculos = await _veiculoService.GetByPlaca(placa);
            }

            if (!veiculos.Any())
            {
                _response.Code = ResponseEnum.NOT_FOUND;
                _response.Data = null;
                _response.Message = "Nenhum veículo encontrado com essa placa";
                return NotFound(_response);
            }

            _response.Code = ResponseEnum.SUCCESS;
            _response.Data = veiculos;
            _response.Message = "Veículos encontrados com sucesso";
            return Ok(_response);
        }

        [HttpGet("tipo/{tipo}")]
        [Authorize(Roles = $"{SystemRoles.Admin},{SystemRoles.Oficina},{SystemRoles.Funcionario},{SystemRoles.Cliente}")]
        public async Task<IActionResult> GetByTipo(string tipo)
        {
            IEnumerable<VeiculoDTO> veiculos;
            if (_currentUserService.IsInRole(SystemRoles.Cliente))
            {
                var clienteId = _currentUserService.UserId;
                if (!clienteId.HasValue)
                    return Forbid();

                veiculos = await _veiculoService.GetByTipoForCliente(tipo, clienteId.Value);
            }
            else if (_currentUserService.IsInRole(SystemRoles.Oficina) || _currentUserService.IsInRole(SystemRoles.Funcionario))
            {
                var oficinaId = _currentUserService.OficinaId;
                if (!oficinaId.HasValue)
                    return Forbid();

                veiculos = await _veiculoService.GetByTipoForOficina(tipo, oficinaId.Value);
            }
            else
            {
                veiculos = await _veiculoService.GetByTipo(tipo);
            }

            if (!veiculos.Any())
            {
                _response.Code = ResponseEnum.NOT_FOUND;
                _response.Data = null;
                _response.Message = "Nenhum veículo encontrado com esse tipo";
                return NotFound(_response);
            }

            _response.Code = ResponseEnum.SUCCESS;
            _response.Data = veiculos;
            _response.Message = "Veículos encontrados com sucesso";
            return Ok(_response);
        }

        [HttpPost]
        [Authorize(Roles = $"{SystemRoles.Admin},{SystemRoles.Cliente}")]
        public async Task<IActionResult> Create(VeiculoDTO veiculoDto)
        {
            if (!_currentUserService.IsInRole(SystemRoles.Admin) && !_currentUserService.IsInRole(SystemRoles.Cliente))
                return Forbid();

            try
            {
                if (_currentUserService.IsInRole(SystemRoles.Admin))
                {
                    await _veiculoService.Create(veiculoDto);
                }
                else
                {
                    var clienteId = _currentUserService.UserId;
                    if (!clienteId.HasValue)
                        return Forbid();

                    await _veiculoService.CreateForCliente(veiculoDto, clienteId.Value);
                }

                return Ok(new { Message = "Veículo cadastrado com sucesso" });
            }
            catch (BusinessValidationException ex)
            {
                return BadRequest(new { Message = "Dados inválidos", Errors = ex.Errors });
            }
        }

        [HttpPut("{id:int}")]
        [Authorize(Roles = $"{SystemRoles.Admin},{SystemRoles.Cliente}")]
        public async Task<IActionResult> Update(int id, VeiculoDTO veiculoDto)
        {
            if (!_currentUserService.IsInRole(SystemRoles.Admin) && !_currentUserService.IsInRole(SystemRoles.Cliente))
                return Forbid();

            try
            {
                if (_currentUserService.IsInRole(SystemRoles.Admin))
                {
                    await _veiculoService.UpdateVeiculo(veiculoDto, id);
                }
                else
                {
                    var clienteId = _currentUserService.UserId;
                    if (!clienteId.HasValue)
                        return Forbid();

                    await _veiculoService.UpdateVeiculoForCliente(veiculoDto, id, clienteId.Value);
                }

                return Ok(new { Message = "Veículo atualizado com sucesso" });
            }
            catch (BusinessValidationException ex)
            {
                return BadRequest(new { Message = "Dados inválidos", Errors = ex.Errors });
            }
            catch (KeyNotFoundException)
            {
                return NotFound(new { Message = "Veículo não encontrado." });
            }
        }

        [HttpDelete("{id:int}")]
        [Authorize(Roles = $"{SystemRoles.Admin},{SystemRoles.Cliente}")]
        public async Task<IActionResult> Delete(int id)
        {
            if (!_currentUserService.IsInRole(SystemRoles.Admin) && !_currentUserService.IsInRole(SystemRoles.Cliente))
                return Forbid();

            if (!_currentUserService.IsInRole(SystemRoles.Admin))
            {
                var clienteId = _currentUserService.UserId;
                if (!clienteId.HasValue)
                    return Forbid();

                var existing = await _veiculoService.GetByIdForCliente(id, clienteId.Value);
                if (existing is null)
                    return NotFound(new { Message = "Veículo não encontrado." });
            }

            await _veiculoService.Remove(id);
            return Ok(new { Message = "Veículo removido com sucesso" });
        }

    }
}
