using AutoMapper;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SIGO.Objects.Contracts;
using SIGO.Objects.Dtos.Entities;
using SIGO.Services.Entities;
using SIGO.Services.Interfaces;
using SIGO.Utils;

namespace SIGO.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class FuncionarioController : ControllerBase
    {
        private readonly IFuncionarioService _funcionarioService;
        private readonly Response _response;
        private readonly IMapper _mapper;

        public FuncionarioController(IFuncionarioService funcionarioService, IMapper mapper)
        {
            _funcionarioService = funcionarioService;
            _mapper = mapper;
            _response = new Response();
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var funcionarioDTO = await _funcionarioService.GetAll();

            _response.Code = ResponseEnum.SUCCESS;
            _response.Data = funcionarioDTO;
            _response.Message = "Funcionário listados com sucesso";

            return Ok(_response);
        }

        [HttpGet("id/{id}")]
        public async Task<IActionResult> GetFuncionarioById(int id)
        {
            var funcionarioDTO = await _funcionarioService.GetById(id);

            if (funcionarioDTO is null)
                return NotFound(new { Message = "Funcionário não encontrado" });

            return Ok(funcionarioDTO);
        }

        [HttpGet("name/{nome}")]
        public async Task<IActionResult> GetFuncionarioByNome(string nome)
        {
            var clientesDto = await _funcionarioService.GetFuncionarioByNome(nome);

            if (!clientesDto.Any())
                return NotFound(new { Message = "Nenhum funcionário encontrado com esse nome" });

            return Ok(clientesDto);
        }

        [HttpPost]
        public async Task<IActionResult> Post(FuncionarioDTO funcionarioDTO)
        {
            if (funcionarioDTO is null)
            {
                _response.Code = ResponseEnum.INVALID;
                _response.Data = null;
                _response.Message = "Dados inválidos";

                return BadRequest(_response);
            }

            try
            {
                funcionarioDTO.Id = 0;
                SanitizeFuncionario(funcionarioDTO);
                await _funcionarioService.ValidarCpf(funcionarioDTO.Cpf);

                await _funcionarioService.Create(funcionarioDTO);

                _response.Code = ResponseEnum.SUCCESS;
                _response.Data = funcionarioDTO;
                _response.Message = "Funcionário cadastrado com sucesso";

                return Ok(_response);
            }
            catch (ArgumentException ex)
            {
                _response.Code = ResponseEnum.INVALID;
                _response.Message = ex.Message;
                _response.Data = null;
                return BadRequest(_response);
            }
            catch (Exception)
            {
                _response.Code = ResponseEnum.ERROR;
                _response.Message = "Não foi possível cadastrar o funcionário";
                _response.Data = null;
                return StatusCode(StatusCodes.Status500InternalServerError, _response);
            }
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Put(int id, FuncionarioDTO funcionarioDTO)
        {
            if (funcionarioDTO is null)
            {
                _response.Code = ResponseEnum.INVALID;
                _response.Data = null;
                _response.Message = "Dados inválidos";

                return BadRequest(_response);
            }

            try
            {

                var existingFuncionarioDTO = await _funcionarioService.GetById(id);
                if (existingFuncionarioDTO is null)
                {
                    _response.Code = ResponseEnum.NOT_FOUND;
                    _response.Data = null;
                    _response.Message = "O funcionário informado não existe";
                    return NotFound(_response);
                }

                SanitizeFuncionario(funcionarioDTO);

                await _funcionarioService.ValidarCpf(funcionarioDTO.Cpf, id);

                await _funcionarioService.Update(funcionarioDTO, id);

                _response.Code = ResponseEnum.SUCCESS;
                _response.Data = funcionarioDTO;
                _response.Message = "Funcionário atualizado com sucesso";

                return Ok(_response);
            }
            catch (ArgumentException ex)
            {
                _response.Code = ResponseEnum.INVALID;
                _response.Message = ex.Message;
                _response.Data = null;
                return BadRequest(_response);
            }
            catch (Exception)
            {
                _response.Code = ResponseEnum.ERROR;
                _response.Message = "Ocorreu um erro ao tentar atualizar os dados do funcionário";
                _response.Data = null;
                return StatusCode(StatusCodes.Status500InternalServerError, _response);
            }
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteFuncionario(int id)
        {
            try
            {
                var clienteDTO = await _funcionarioService.GetById(id);

                if (clienteDTO is null)
                {
                    _response.Code = ResponseEnum.NOT_FOUND;
                    _response.Data = null;
                    _response.Message = "Funcionário não encontrado";
                    return NotFound(_response);
                }

                await _funcionarioService.Remove(id);

                _response.Code = ResponseEnum.SUCCESS;
                _response.Data = null;
                _response.Message = "Funcionário deletado com sucesso";
                return Ok(_response);
            }
            catch (Exception)
            {
                _response.Code = ResponseEnum.ERROR;
                _response.Message = "Ocorreu um erro ao deletar o cliente";
                _response.Data = null;
                return StatusCode(StatusCodes.Status500InternalServerError, _response);
            }
        }

        private static void SanitizeFuncionario(FuncionarioDTO funcionarioDTO)
        {
            funcionarioDTO.Cpf = SanitizeHelper.ApenasDigitos(funcionarioDTO.Cpf);
        }
    }
}
