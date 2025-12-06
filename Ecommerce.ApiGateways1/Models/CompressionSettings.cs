namespace Ecommerce.ApiGateways1.Models
{
    public class CompressionSettings
    {
        public bool Enabled {  get; set; }
        public long CompressionThresholdBytes {  get; set; }
        public string[] SupportedEncoding = new[] { "br", "gzip" };
        public string DefaultEncoding = "gzip";
    }
}
