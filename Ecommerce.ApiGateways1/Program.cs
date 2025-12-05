using Ecommerce.ApiGateways1.Middleware;
using Ecommerce.ApiGateways1.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Ocelot.DependencyInjection;
using Ocelot.Middleware;
using System.Text;

var builder = WebApplication.CreateBuilder(args);
builder.Configuration
      .SetBasePath(builder.Environment.ContentRootPath)
      .AddOcelot("ocelot.json",reloadOnChange:true,optional:false); // single ocelot.json file in read-only mode
builder.Services
    .AddOcelot(builder.Configuration);
builder.Services.AddAuthentication(options =>
{
    var scheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;


}).AddJwtBearer(options =>
{
    options.TokenValidationParameters = new Microsoft.IdentityModel.Tokens.TokenValidationParameters()
    {
        ValidateIssuer = true,
        ValidateAudience = false,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = builder.Configuration["JwtSetting:Issuer"],
        IssuerSigningKey= new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["JwtSetting:SecretKey"]!)), 
    };
    

});
var urls = builder.Configuration.GetSection("ServiceUrls");

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


builder.Services.AddSingleton<IOrderSummaryAggregator,OrderSummaryAggregator>();
var app = builder.Build();
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

app.UseMiddleware<JwtAuthenticationMiddleware>();


app.UseAuthorization();

app.MapGet("/", () => "Hello World!");

await app.UseOcelot();
app.Run();
