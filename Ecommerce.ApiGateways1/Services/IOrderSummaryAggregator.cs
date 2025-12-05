using Ecommerce.ApiGateways1.OrderSummary;

namespace Ecommerce.ApiGateways1.Services
{
    public interface IOrderSummaryAggregator
    {
        
            Task<OrderSummaryResponseDTO?> GetOrderSummaryAsync(Guid orderId);
        

    }
}
