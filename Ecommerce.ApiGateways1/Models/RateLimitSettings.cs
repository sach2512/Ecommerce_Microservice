namespace Ecommerce.ApiGateways1.Models
{
    public class RateLimitSettings
    {
        public bool IsEnabled {  get; set; }
        public string?DefaultPolicy { get; set; }
        public string? ProductApiPolicy {  get; set; }
        public string? OrderApiPolicy { get; set; }
        public string? PaymentApiPolicy {  get; set; }
        public class policy
        {
            public int PermitLimit { get; set; }
            public string Window { get; set; }
            public int QueueLimit { get; set; }
            public string QueueProcessingOrder { get; set; }
        }
    }
}
