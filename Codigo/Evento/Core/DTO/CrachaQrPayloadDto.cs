using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Core.DTO
{
    public class CrachaQrPayloadDto
    {
        [JsonPropertyName("v")]
        public int V { get; set; } = 1;

        [JsonPropertyName("pid")]
        public uint Pid { get; set; }

        [JsonPropertyName("cpf")]
        public string Cpf { get; set; } = string.Empty;

        [JsonPropertyName("nome")]
        public string Nome { get; set; } = string.Empty;

        [JsonPropertyName("eid")]
        public uint Eid { get; set; }

        [JsonPropertyName("sid")]
        public List<uint> Sid { get; set; } = new();

        [JsonPropertyName("hash")]
        public string Hash { get; set; } = string.Empty;
    }
}

