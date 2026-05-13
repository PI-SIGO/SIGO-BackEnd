namespace SIGO.Security
{
    public class CompartilhamentoClienteOptions
    {
        public const string SectionName = "CompartilhamentoCliente";

        public string CodigoHmacSecret { get; set; } = string.Empty;
    }
}
