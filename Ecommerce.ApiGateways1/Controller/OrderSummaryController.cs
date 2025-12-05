using Ecommerce.ApiGateways1.common;
using Ecommerce.ApiGateways1.OrderSummary;
using Ecommerce.ApiGateways1.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Ecommerce.ApiGateways1.Controller
{
    [Route("gateway/order-summary")]
    [ApiController]
    public class OrderSummaryController : ControllerBase
    {
        private readonly IOrderSummaryAggregator _aggregator;
        private readonly ILogger<OrderSummaryController> _logger;

        public OrderSummaryController(
            IOrderSummaryAggregator aggregator,
            ILogger<OrderSummaryController> logger)
        {
            _aggregator = aggregator;
            _logger = logger;
        }

        [HttpGet("GetOrderSummary")]
        [Authorize]
        public async Task<ActionResult<ApiResponse<OrderSummaryResponseDTO>>> GetOrderSummary(Guid orderId)
        {
            try
            {
                _logger.LogInformation("Aggregating response for OrderId {OrderId}", orderId);

                var summary = await _aggregator.GetOrderSummaryAsync(orderId);

                if (summary is null)
                {
                    return NotFound(
                        ApiResponse<OrderSummaryResponseDTO>.FailResponse(
                            $"Order with id {orderId} not found."));
                }

                return Ok(
                    ApiResponse<OrderSummaryResponseDTO>.SuccessResponse(summary));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Error while aggregating order summary for OrderId {OrderId}", orderId);

                return StatusCode(
                    StatusCodes.Status500InternalServerError,
                    ApiResponse<OrderSummaryResponseDTO>.FailResponse(
                        "An unexpected error occurred while retrieving the order summary."));
            }
        }
    }
}
