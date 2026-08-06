using ControlGastos.Core.Interfaces;
using ControlGastos.Infrastructure.Data;
using ControlGastos.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
var builder = WebApplication.CreateBuilder(args);

var connectionString =
    builder.Configuration.GetConnectionString("SupabaseDatabase")
    ?? throw new InvalidOperationException(
        "No se encontró la conexión SupabaseDatabase.");

builder.Services.AddDbContext<ControlGastosDbContext>(options =>
{
    options.UseNpgsql(connectionString);
});

builder.Services.AddScoped<ICategoriaGastoRepository, CategoriaGastoRepository>();

builder.Services.AddControllers();
builder.Services.AddOpenApi();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
