using SIGO.Objects.Dtos.Entities;

namespace SIGO.Services.Interfaces
{
    public interface ICompartilhamentoClienteService
    {
        Task<CompartilhamentoClienteCodigoDTO> CriarCodigo(int clienteId, CriarCompartilhamentoClienteDTO dto);
        Task<CompartilhamentoClienteResultadoDTO> ResgatarCodigo(int oficinaId, ResgatarCompartilhamentoClienteDTO dto, string? ipAddress = null);
    }
}
