using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SIGO.Errors;
using SIGO.Objects.Contracts;
using SIGO.Objects.Dtos.Entities;
using SIGO.Security;
using SIGO.Services.Interfaces;

namespace SIGO.Controllers
{
    [Route("api/v1/veiculos")]
    [ApiController]
    [Authorize(Policy = AuthorizationPolicies.SelfServiceAccess)]
    public class VeiculoController : ControllerBase
    {
        private readonly IVeiculoService _veiculoService;
        private readonly ICurrentUserService _currentUserService;

        public VeiculoController(
            IVeiculoService veiculoService,
            ICurrentUserService currentUserService)
        {
            _veiculoService = veiculoService;
            _currentUserService = currentUserService;
        }

        [HttpGet]
        [Authorize(Roles = $"{SystemRoles.Admin},{SystemRoles.Oficina},{SystemRoles.Funcionario},{SystemRoles.Cliente}")]
        public async Task<IActionResult> Get([FromQuery] PaginationRequest? pagination = null)
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

            return Ok(PagedResponse<VeiculoDTO>.Create(veiculos, pagination ?? new PaginationRequest()));
        }

        [HttpGet("{id:int}")]
        [Authorize(Roles = $"{SystemRoles.Admin},{SystemRoles.Oficina},{SystemRoles.Funcionario},{SystemRoles.Cliente}")]
        public async Task<IActionResult> GetById(int id)
        {
            VeiculoDTO? veiculo;

            if (_currentUserService.IsInRole(SystemRoles.Cliente))
            {
                var clienteId = _currentUserService.UserId;
                if (!clienteId.HasValue)
                    return Forbid();

                veiculo = await _veiculoService.GetByIdForCliente(id, clienteId.Value);
            }
            else if (_currentUserService.IsInRole(SystemRoles.Oficina) ||
                     _currentUserService.IsInRole(SystemRoles.Funcionario))
            {
                var oficinaId = _currentUserService.OficinaId;
                if (!oficinaId.HasValue)
                    return Forbid();

                veiculo = await _veiculoService.GetByIdForOficina(id, oficinaId.Value);
            }
            else
            {
                veiculo = await _veiculoService.GetById(id);
            }

            return veiculo is null
                ? this.ApiProblem(StatusCodes.Status404NotFound, "Veículo não encontrado.")
                : Ok(veiculo);
        }

        [HttpGet("placa/{placa}")]
        [Authorize(Roles = $"{SystemRoles.Admin},{SystemRoles.Oficina},{SystemRoles.Funcionario},{SystemRoles.Cliente}")]
        public async Task<IActionResult> GetByPlaca(
            string placa,
            [FromQuery] PaginationRequest? pagination = null)
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

            return Ok(PagedResponse<VeiculoDTO>.Create(veiculos, pagination ?? new PaginationRequest()));
        }

        [HttpGet("tipo/{tipo}")]
        [Authorize(Roles = $"{SystemRoles.Admin},{SystemRoles.Oficina},{SystemRoles.Funcionario},{SystemRoles.Cliente}")]
        public async Task<IActionResult> GetByTipo(
            string tipo,
            [FromQuery] PaginationRequest? pagination = null)
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

            return Ok(PagedResponse<VeiculoDTO>.Create(veiculos, pagination ?? new PaginationRequest()));
        }

        [HttpPost]
        [Authorize(Roles = SystemRoles.Cliente)]
        public async Task<IActionResult> Create(VeiculoRequestDTO request)
        {
            var clienteId = _currentUserService.UserId;
            if (!clienteId.HasValue)
                return Forbid();

            var veiculoCriado = await _veiculoService.CreateForCliente(request, clienteId.Value);
            return Created($"/api/v1/veiculos/{veiculoCriado.Id}", veiculoCriado);
        }

        [HttpPost("~/api/v1/clientes/{clienteId:int}/veiculos")]
        [Authorize(Roles = $"{SystemRoles.Admin},{SystemRoles.Oficina},{SystemRoles.Funcionario}")]
        public async Task<IActionResult> CreateForCliente(int clienteId, VeiculoRequestDTO request)
        {
            VeiculoDTO veiculoCriado;
            if (_currentUserService.IsInRole(SystemRoles.Admin))
            {
                veiculoCriado = await _veiculoService.CreateVeiculo(request, clienteId);
            }
            else
            {
                var oficinaId = _currentUserService.OficinaId;
                if (!oficinaId.HasValue)
                    return Forbid();

                veiculoCriado = await _veiculoService.CreateForOficina(
                    request,
                    clienteId,
                    oficinaId.Value);
            }

            return Created($"/api/v1/veiculos/{veiculoCriado.Id}", veiculoCriado);
        }

        [HttpPost("{id:int}/imagens")]
        [Consumes("multipart/form-data")]
        [Authorize(Roles = $"{SystemRoles.Admin},{SystemRoles.Cliente},{SystemRoles.Oficina},{SystemRoles.Funcionario}")]
        public async Task<IActionResult> AddImagens(
            int id,
            [FromForm] List<IFormFile> imagens,
            CancellationToken cancellationToken)
        {
            {
                IReadOnlyCollection<VeiculoImagemDTO> imagensSalvas;

                if (_currentUserService.IsInRole(SystemRoles.Admin))
                {
                    imagensSalvas = await _veiculoService.AddImagens(id, imagens, cancellationToken);
                }
                else if (_currentUserService.IsInRole(SystemRoles.Cliente))
                {
                    var clienteId = _currentUserService.UserId;
                    if (!clienteId.HasValue)
                        return Forbid();

                    imagensSalvas = await _veiculoService.AddImagensForCliente(
                        id,
                        clienteId.Value,
                        imagens,
                        cancellationToken);
                }
                else if (_currentUserService.IsInRole(SystemRoles.Oficina) ||
                         _currentUserService.IsInRole(SystemRoles.Funcionario))
                {
                    var oficinaId = _currentUserService.OficinaId;
                    if (!oficinaId.HasValue)
                        return Forbid();

                    imagensSalvas = await _veiculoService.AddImagensForOficina(
                        id,
                        oficinaId.Value,
                        imagens,
                        cancellationToken);
                }
                else
                {
                    return Forbid();
                }

                return Created($"/api/v1/veiculos/{id}/imagens", imagensSalvas);
            }
        }

        [HttpGet("{veiculoId:int}/imagens/{nomeArquivo}")]
        [Authorize(Roles = $"{SystemRoles.Admin},{SystemRoles.Oficina},{SystemRoles.Funcionario},{SystemRoles.Cliente}")]
        public async Task<IActionResult> GetImagemArquivo(int veiculoId, string nomeArquivo)
        {
            {
                VeiculoImagemArquivoDTO arquivo;

                if (_currentUserService.IsInRole(SystemRoles.Cliente))
                {
                    var clienteId = _currentUserService.UserId;
                    if (!clienteId.HasValue)
                        return Forbid();

                    arquivo = await _veiculoService.GetImagemArquivoForCliente(veiculoId, clienteId.Value, nomeArquivo);
                }
                else if (_currentUserService.IsInRole(SystemRoles.Oficina) || _currentUserService.IsInRole(SystemRoles.Funcionario))
                {
                    var oficinaId = _currentUserService.OficinaId;
                    if (!oficinaId.HasValue)
                        return Forbid();

                    arquivo = await _veiculoService.GetImagemArquivoForOficina(veiculoId, oficinaId.Value, nomeArquivo);
                }
                else
                {
                    arquivo = await _veiculoService.GetImagemArquivo(veiculoId, nomeArquivo);
                }

                var result = File(arquivo.Conteudo, arquivo.ContentType);
                result.EnableRangeProcessing = true;
                return result;
            }
        }

        [HttpDelete("{veiculoId:int}/imagens/{imagemId:int}")]
        [Authorize(Roles = $"{SystemRoles.Admin},{SystemRoles.Cliente}")]
        public async Task<IActionResult> DeleteImagem(int veiculoId, int imagemId)
        {
            if (!_currentUserService.IsInRole(SystemRoles.Admin) && !_currentUserService.IsInRole(SystemRoles.Cliente))
                return Forbid();

            {
                if (_currentUserService.IsInRole(SystemRoles.Admin))
                {
                    await _veiculoService.RemoveImagem(veiculoId, imagemId);
                }
                else if (_currentUserService.IsInRole(SystemRoles.Cliente))
                {
                    var clienteId = _currentUserService.UserId;
                    if (!clienteId.HasValue)
                        return Forbid();

                    await _veiculoService.RemoveImagemForCliente(veiculoId, clienteId.Value, imagemId);
                }

                return NoContent();
            }
        }

        [HttpPut("{id:int}")]
        [Authorize(Roles = $"{SystemRoles.Admin},{SystemRoles.Cliente},{SystemRoles.Oficina},{SystemRoles.Funcionario}")]
        public async Task<IActionResult> Update(int id, VeiculoRequestDTO request)
        {
            {
                VeiculoDTO veiculoAtualizado;
                if (_currentUserService.IsInRole(SystemRoles.Admin))
                {
                    veiculoAtualizado = await _veiculoService.UpdateVeiculo(request, id);
                }
                else if (_currentUserService.IsInRole(SystemRoles.Cliente))
                {
                    var clienteId = _currentUserService.UserId;
                    if (!clienteId.HasValue)
                        return Forbid();

                    veiculoAtualizado = await _veiculoService.UpdateVeiculoForCliente(request, id, clienteId.Value);
                }
                else if (_currentUserService.IsInRole(SystemRoles.Oficina) ||
                         _currentUserService.IsInRole(SystemRoles.Funcionario))
                {
                    var oficinaId = _currentUserService.OficinaId;
                    if (!oficinaId.HasValue)
                        return Forbid();

                    veiculoAtualizado = await _veiculoService.UpdateVeiculoForOficina(request, id, oficinaId.Value);
                }
                else
                {
                    return Forbid();
                }

                return Ok(veiculoAtualizado);
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
                    return this.ApiProblem(
                        StatusCodes.Status404NotFound,
                        "Veículo não encontrado.");
            }

            await _veiculoService.Remove(id);
            return NoContent();
        }

    }
}
