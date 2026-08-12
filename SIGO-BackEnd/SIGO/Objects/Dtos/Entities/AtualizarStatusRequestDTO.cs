using System.Text.Json.Serialization;
using SIGO.Objects.Enums;

namespace SIGO.Objects.Dtos.Entities
{
    public sealed record AtualizarStatusRequestDTO
    {
        [JsonConverter(typeof(JsonStringEnumConverter))]
        public Status? Status { get; init; }
    }
}
