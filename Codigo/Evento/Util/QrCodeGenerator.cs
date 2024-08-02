using QRCoder;

namespace Util
{
    public static class QrCodeGenerator
    {
        public static byte[] GenerateQr(string url)
        {
            using (QRCodeGenerator qrGenerator = new QRCodeGenerator())
            using (QRCodeData qrCodeData = qrGenerator.CreateQrCode(url, QRCodeGenerator.ECCLevel.Q))
            using (PngByteQRCode qrCode = new PngByteQRCode(qrCodeData))
            {
                byte[] qrCodeImage = qrCode.GetGraphic(20);
                return qrCodeImage;
            }
        }
    }
}