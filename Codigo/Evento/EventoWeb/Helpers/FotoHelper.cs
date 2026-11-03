#nullable enable
using System;
using Util;

namespace EventoWeb.Helpers
{
    public static class FotoHelper
    {
        public static bool TryObterFotoDataUrl(byte[]? fotoBytes, out string? dataUrl)
        {
            dataUrl = null;

            if (fotoBytes == null || !fotoBytes.IsImagem())
            {
                return false;
            }

            var mime = DetectarMimeType(fotoBytes);
            dataUrl = $"data:{mime};base64,{Convert.ToBase64String(fotoBytes)}";
            return true;
        }

        public static string DetectarMimeType(byte[] fotoBytes)
        {
            if (fotoBytes.Length >= 3 && fotoBytes[0] == 0xFF && fotoBytes[1] == 0xD8 && fotoBytes[2] == 0xFF)
                return "image/jpeg";

            if (fotoBytes.Length >= 12 &&
                fotoBytes[0] == 0x52 && fotoBytes[1] == 0x49 && fotoBytes[2] == 0x46 && fotoBytes[3] == 0x46 &&
                fotoBytes[8] == 0x57 && fotoBytes[9] == 0x45 && fotoBytes[10] == 0x42 && fotoBytes[11] == 0x50)
                return "image/webp";

            if (fotoBytes.Length >= 4 &&
                fotoBytes[0] == 0x89 && fotoBytes[1] == 0x50 && fotoBytes[2] == 0x4E && fotoBytes[3] == 0x47)
                return "image/png";

            if (fotoBytes.Length >= 4 &&
                ((fotoBytes[0] == 0x49 && fotoBytes[1] == 0x49 && fotoBytes[2] == 0x2A && fotoBytes[3] == 0x00) ||
                 (fotoBytes[0] == 0x4D && fotoBytes[1] == 0x4D && fotoBytes[2] == 0x00 && fotoBytes[3] == 0x2A)))
                return "image/tiff";

            return "image/jpeg";
        }
    }
}
