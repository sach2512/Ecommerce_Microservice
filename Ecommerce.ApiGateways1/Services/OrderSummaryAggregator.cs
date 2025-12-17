using Ecommerce.ApiGateways1.common;
using Ecommerce.ApiGateways1.OrderSummary;
using Microsoft.AspNetCore.Http;
using OrderService.Application.DTOs.Order;
using OrderService.Domain.Entities;
using ProductService.Application.DTOs;
using System.Collections.Generic;
using System.Net;
using System.Text.Json;
using System.Text.Json.Serialization;
using UserService.Application.DTOs;
using UserService.Domain.Entities;

namespace Ecommerce.ApiGateways1.Services
{
    public class OrderSummaryAggregator : IOrderSummaryAggregator
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger _logger;
       private readonly IHttpContextAccessor _httpContextAccessor;

        private readonly string  baseurl = $"/api";


        public OrderSummaryAggregator(IHttpClientFactory httpClientFactory, ILogger logger, IHttpContextAccessor httpContextAccessor)
        {
            _httpClientFactory = httpClientFactory;
            _logger = logger;
            _httpContextAccessor = httpContextAccessor;
        }

        public async Task<OrderSummaryResponseDTO?> GetOrderSummaryAsync(Guid orderId)
        {
             var order =await fecthorder(orderId);
            
            if(order == null)
            {
                return null;
            }
            var result = new OrderSummaryResponseDTO()
            {
                OrderId = order.OrderId,
                Order = new OrderInfoDTO()
                {
                    OrderNumber = order.OrderNumber,
                    OrderDate = order.OrderDate,
                    Status=order.OrderStatus.ToString(),
                    SubTotalAmount=order.TotalAmount,
                    DiscountAmount=order.DiscountAmount,
                    ShippingCharges=order.ShippingCharges,
                    TaxAmount=order.TaxAmount,
                    TotalAmount=order.TotalAmount,
                    PaymentMethod=order.PaymentMethod.ToString(),
                },

            };
            var userId = order.UserId;
            var items =  new List<OrderItemResponseDTO>();

            var customerTask = FetchCustomerAsync(userId);
            var productsTask = FetchProductsAsync(items);
            var paymentTask = FetchPaymentAsync(orderId);

            // Run all API calls in parallel (non-blocking)
            await Task.WhenAll(customerTask, productsTask, paymentTask);

            // 3️. Aggregate responses and track partial failures.

            // Customer
            if (customerTask.Result != null)
            {
                result.Customer = customerTask.Result;
            }
            else
            {
                result.IsPartial = true;
                result.Warnings.Add("Customer details could not be loaded.");
            }

            // Products
            if (productsTask.Result.Any())
            {
                result.Products = productsTask.Result;
            }
            else
            {
                result.IsPartial = true;
                result.Warnings.Add("Product details could not be fully loaded.");
            }

            // Payment
            if (paymentTask.Result != null)
            {
                result.Payment = paymentTask.Result;
            }
            else
            {
                result.IsPartial = true;
                result.Warnings.Add("Payment details not available.");
            }

            // Return a unified object even if some data sources failed.
            return result;
        }

        

        private async Task<OrderResponseDTO> fecthorder(Guid orderId)
        {
            try
            {
                var orderclient = _httpClientFactory.CreateClient("OrderService");
               
                var requesturl = baseurl + $"/order/GetOrder{orderId}";
                var response = orderclient.GetAsync(requesturl).Result;
                if (response.StatusCode == HttpStatusCode.NotFound)
                {
                    return null;
                }
                response.EnsureSuccessStatusCode();
                var apiresponse = await response.Content.ReadFromJsonAsync<ApiResponse<OrderResponseDTO>>(
                    new System.Text.Json.JsonSerializerOptions()
                    {
                        PropertyNameCaseInsensitive = true,
                        PropertyNamingPolicy = null,
                        Converters = { new JsonStringEnumConverter() }

                    });
                if (apiresponse?.Success != true || apiresponse.Data is null)
                {
                    _logger.LogWarning("OrderService returned invalid response for {OrderId}", orderId);
                    return null;
                }

                return apiresponse.Data;
                


            }
            catch (Exception ex)
            {
                _logger.LogInformation("error occured while fecthng order");
                return null;
            }
            
            


        }


        private async Task<CustomerInfoDTO?> FetchCustomerAsync(Guid customerId)
        {
            try
            {
                var client = _httpClientFactory.CreateClient("UserService");

                var url = $"{baseurl}/User/GetProfile/{customerId}";

                var httpResponse = await client.GetAsync(url);

                if (httpResponse is null)
                    return null;

                httpResponse.EnsureSuccessStatusCode();

                var jsonResponse = await httpResponse.Content
                    .ReadFromJsonAsync<ApiResponse<ProfileDTO>>(
                        new JsonSerializerOptions
                        {
                            PropertyNameCaseInsensitive = true,
                            Converters = { new JsonStringEnumConverter() }
                        });

                if (jsonResponse is null || jsonResponse.Success != true || jsonResponse.Data is null)
                {
                    _logger.LogWarning("UserService returned invalid response for customer {CustomerId}", customerId);
                    return null;
                }


                var p= jsonResponse.Data;
                var customerinfo = new CustomerInfoDTO()
                {
                    FullName = p.FullName,
                    UserId = p.UserId,
                    Email = p.Email,
                    Mobile=p.PhoneNumber,
                    ProfilePhotoUrl = p.ProfilePhotoUrl,
                };
                return customerinfo;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while fetching customer details for {CustomerId}", customerId);
                return null;
            }
        }
        private async Task<PaymentInfoDTO?> FetchPaymentAsync(Guid orderId)
        {
            // NOTE:
            // Currently, there is NO endpoint in PaymentService to get payment details by OrderId.
            // This method returns hardcoded data for demo purposes.

            _logger.LogInformation(
                "Payment details for OrderId {OrderId} are currently stubbed. Integration pending.",
                orderId);

            await Task.CompletedTask;

            // Hardcoded sample payment (used until PaymentService endpoint is ready)
            return new PaymentInfoDTO
            {
                PaymentId = Guid.Parse("11111111-1111-1111-1111-111111111111"),
                Status = "Paid",
                Method = "Online",
                PaidOn = DateTime.UtcNow.AddMinutes(-5),
                TransactionReference = "DEMO-TXN-PLACEHOLDER"
            };
        }

        private async Task<List<OrderProductInfoDTO>> FetchProductsAsync(IEnumerable<OrderItemResponseDTO> items)
        {
            try
            {
                var result = new List<OrderProductInfoDTO>();

                var productIds = items
                    .Select(i => i.ProductId)
                    .Where(id => id != Guid.Empty)
                    .Distinct()
                    .ToList();

                if (!productIds.Any())
                    return result;

                var client = _httpClientFactory.CreateClient("ProductService");

                var authheader = _httpContextAccessor.HttpContext.Request.Headers["X-Authorization"];
                if (!string.IsNullOrEmpty(authheader))
                {
                    client.DefaultRequestHeaders.Authorization =
                        System.Net.Http.Headers.AuthenticationHeaderValue.Parse(authheader);
                }

                // 1️⃣ Send POST request
                var httpResponse = await client.PostAsJsonAsync("/api/products/GetByIds", productIds);

                // handle 404
                if (httpResponse.StatusCode == HttpStatusCode.NotFound)
                    return result;

                // handle all other bad codes
                if (!httpResponse.IsSuccessStatusCode)
                    return result;

                // 2️⃣ Read JSON safely
                var productsResponse = await httpResponse.Content
                    .ReadFromJsonAsync<ApiResponse<List<ProductDTO>>>(new System.Text.Json.JsonSerializerOptions()
                    {
                        PropertyNameCaseInsensitive = true,
                        PropertyNamingPolicy = null,
                        Converters = { new JsonStringEnumConverter() }

                    });

                if (productsResponse == null || productsResponse.Data == null)
                    return result;

                var productData = productsResponse.Data;

                // 3️⃣ Build lookup
                var productLookup = productData
                    .GroupBy(p => p.Id)
                    .ToDictionary(g => g.Key, g => g.First());

                // 4️⃣ Merge order items + product data
                foreach (var orderItem in items)
                {
                    if (productLookup.TryGetValue(orderItem.ProductId, out var productDTO))
                    {
                        result.Add(new OrderProductInfoDTO
                        {
                            ProductId = productDTO.Id,
                            Name = productDTO.Name,
                            SKU = productDTO.SKU,
                            ImageUrl = productDTO.PrimaryImageUrl,

                            Quantity = orderItem.Quantity,
                            UnitPrice = orderItem.DiscountedPrice
                        });
                    }
                }

                return result;   // ✅ RETURN THE FINAL LIST
            }
            catch (Exception)
            {
                return null;
            }
        }



    }
}
