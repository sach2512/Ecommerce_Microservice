using Ecommerce.ApiGateways1.Middleware;
using Ecommerce.ApiGateways1.Models;
using Ecommerce.ApiGateways1.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Ocelot.DependencyInjection;
using Ocelot.Middleware;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

builder.Configuration
    .SetBasePath(builder.Environment.ContentRootPath)
    .AddOcelot("ocelot.json", reloadOnChange: true, optional: false);

// Register Ocelot
builder.Services.AddOcelot(builder.Configuration);

// JWT Authentication
builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters()
        {
            ValidateIssuer = true,
            ValidateAudience = false,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["JwtSetting:Issuer"],
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(builder.Configuration["JwtSetting:SecretKey"]!)
            )
        };
    });
builder.Configuration.AddJsonFile("ReverseProxy.Json", optional: false, reloadOnChange: true);
builder.Services.AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));
// Load compression settings
var settings = builder.Configuration.GetSection("CompressionSettings").Get<CompressionSettings>();

// Load service URLs
var urls = builder.Configuration.GetSection("ServiceUrls");

// Register named HttpClients
builder.Services.AddHttpClient("OrderService", c =>
{
    c.BaseAddress = new Uri(urls["OrderService"]!);
});

builder.Services.AddHttpClient("UserService", c =>
{
    c.BaseAddress = new Uri(urls["UserService"]!);
});

builder.Services.AddHttpClient("ProductService", c =>
{
    c.BaseAddress = new Uri(urls["ProductService"]!);
});

builder.Services.AddHttpClient("PaymentService", c =>
{
    c.BaseAddress = new Uri(urls["PaymentService"]!);
});

// Register aggregator
builder.Services.AddSingleton<IOrderSummaryAggregator, OrderSummaryAggregator>();

builder.Services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = builder.Configuration["ReddisCacheSetting:ConnectionString"];
    options.InstanceName = builder.Configuration["ReddisCacheSetting:InstanceName"];

});

var app = builder.Build();

// Gateway route pipeline (/gateway/*)
app.MapWhen(
    x => x.Request.Path.StartsWithSegments("/gateway", StringComparison.OrdinalIgnoreCase),
    gatewayApp =>
    {
        gatewayApp.UseRouting();
        gatewayApp.UseAuthentication();
        gatewayApp.UseAuthorization();

        gatewayApp.UseEndpoints(endpoints =>
        {
            endpoints.MapControllers();
        });
    });

// Custom JWT middleware (optional)
app.UseMiddleware<ResponseCompressionMiddleware>();
app.UseMiddleware<JwtAuthenticationMiddleware>();
app.UseMiddleware<ResponseCachingMiddleware>();
// Authorization (global)
app.UseAuthorization();

// Test root route
app.MapGet("/", () => "Hello World!");

// Ocelot
//await app.UseOcelot();
app.MapReverseProxy();

app.Run();
