using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.DTO
{
    public class ModelocrachaDTO
    {
        public int Id { get; set; }

        public byte[] Logotipo { get; set; } = null!;

        public string Texto { get; set; } = null!;

        public sbyte Qrcode { get; set; }

        public uint IdEvento { get; set; }
    }
}
