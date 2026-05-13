using SIGO.Objects.Enums;
using System.ComponentModel.DataAnnotations.Schema;

namespace SIGO.Objects.Dtos.Entities
{
    public class FuncionarioDTO
    {
        public int Id { get; set; }
        public string Nome { get; set; }
        public string Cpf { get; set; }
        public string Cargo { get; set; }
        public string Email { get; set; }
        public Situacao Situacao { get; set; }
        public int? IdOficina { get; set; }
        public string Role { get; set; } = SIGO.Security.SystemRoles.Funcionario;
    }
}
