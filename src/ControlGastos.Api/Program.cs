using ControlGastos.Api.Services;
using ControlGastos.Core.Interfaces;
using ControlGastos.Infrastructure.Data;
using ControlGastos.Infrastructure.Repositories;
using ControlGastos.Infrastructure.Storage;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;
var builder = WebApplication.CreateBuilder(args);

var connectionString =
    builder.Configuration.GetConnectionString("SupabaseDatabase")
    ?? throw new InvalidOperationException(
        "No se encontró la conexión SupabaseDatabase.");

var supabaseUrl = builder.Configuration["Supabase:Url"]
    ?? throw new InvalidOperationException("La configuración Supabase:Url es obligatoria.");
var issuer = $"{supabaseUrl.TrimEnd('/')}/auth/v1";
var metadataAddress = $"{issuer}/.well-known/openid-configuration";
var configurationManager = new ConfigurationManager<OpenIdConnectConfiguration>(
    metadataAddress,
    new OpenIdConnectConfigurationRetriever(),
    new HttpDocumentRetriever { RequireHttps = true });

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.Authority = issuer;
        options.MetadataAddress = metadataAddress;
        options.ConfigurationManager = configurationManager;
        options.Audience = "authenticated";
        options.MapInboundClaims = false;
        options.RefreshOnIssuerKeyNotFound = true;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = issuer,
            ValidateAudience = true,
            ValidAudience = "authenticated",
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ClockSkew = TimeSpan.FromMinutes(1),
            IssuerSigningKeyResolver = (_, _, kid, _) =>
            {
                var configuration = configurationManager
                    .GetConfigurationAsync(CancellationToken.None)
                    .GetAwaiter()
                    .GetResult();
                var keys = configuration.SigningKeys
                    .Where(key => string.Equals(key.KeyId, kid, StringComparison.Ordinal))
                    .ToArray();

                if (keys.Length == 0)
                {
                    configurationManager.RequestRefresh();
                }

                return keys;
            }
        };
        options.Events = new JwtBearerEvents
        {
            OnTokenValidated = context =>
            {
                var subject = context.Principal?.FindFirst("sub")?.Value;
                if (!Guid.TryParse(subject, out _))
                {
                    context.Fail("El token no contiene un claim sub válido.");
                }

                return Task.CompletedTask;
            },
            OnAuthenticationFailed = context =>
            {
                var authorization = context.Request.Headers.Authorization.ToString();
                var token = authorization.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase)
                    ? authorization["Bearer ".Length..]
                    : string.Empty;
                var kid = ObtenerKid(token);
                var logger = context.HttpContext.RequestServices
                    .GetRequiredService<ILoggerFactory>()
                    .CreateLogger("SupabaseJwt");

                logger.LogWarning(
                    "Falló la autenticación JWT. Tipo: {ExceptionType}. Mensaje: {Message}. Kid: {Kid}",
                    context.Exception.GetType().Name,
                    context.Exception.Message,
                    kid ?? "no disponible");

                return Task.CompletedTask;
            }
        };
    });

builder.Services.AddAuthorization();

builder.Services.AddDbContext<ControlGastosDbContext>(options =>
{
    options.UseNpgsql(connectionString);
});

builder.Services.AddScoped<ICategoriaGastoRepository, CategoriaGastoRepository>();
builder.Services.AddScoped<IMedioPagoRepository, MedioPagoRepository>();
builder.Services.AddScoped<IGastoRepository, GastoRepository>();
builder.Services.AddScoped<IPerfilRepository, PerfilRepository>();
builder.Services.AddScoped<IDashboardRepository, DashboardRepository>();
builder.Services.Configure<SupabaseOptions>(builder.Configuration.GetSection(SupabaseOptions.SectionName));
builder.Services.AddHttpClient<IComprobanteStorageService, SupabaseComprobanteStorageService>();
builder.Services.AddScoped<IGastoComprobanteService, GastoComprobanteService>();
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<IUsuarioActualService, UsuarioActualService>();

builder.Services.AddControllers();
builder.Services.AddOpenApi();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();

static string? ObtenerKid(string token)
{
    var tokenHandler = new JsonWebTokenHandler();
    return tokenHandler.CanReadToken(token)
        ? tokenHandler.ReadJsonWebToken(token).Kid
        : null;
}
