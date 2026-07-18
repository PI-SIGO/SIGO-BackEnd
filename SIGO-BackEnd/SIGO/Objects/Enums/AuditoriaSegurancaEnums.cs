namespace SIGO.Objects.Enums
{
    public enum TipoAtorAuditoria
    {
        Anonimo = 0,
        Cliente = 1,
        Oficina = 2,
        Funcionario = 3,
        Admin = 4,
        Sistema = 5
    }

    public enum TipoEventoAuditoria
    {
        CadastroDiretoConcluido = 2,
        VinculoCriado = 4,
        VinculoAtivado = 5,
        VinculoRevogado = 6,
        SenhaAlterada = 7,
        CompartilhamentoLegadoTentativa = 8,
        CompartilhamentoLegadoDesativado = 9
    }

    public enum ResultadoAuditoria
    {
        Sucesso = 1,
        Falha = 2
    }
}
