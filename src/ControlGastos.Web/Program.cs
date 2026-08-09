using ControlGastos.Web.Interfaces;
using ControlGastos.Web.Models;
using ControlGastos.Web.Services;

var builder = WebApplication.CreateBuilder(args);

var supabaseAuthUrl = builder.Configuration["SupabaseAuth:Url"]
    ?? throw new InvalidOperationException("La configuración SupabaseAuth:Url es obligatoria.");
var apiBaseUrl = builder.Configuration["Api:BaseUrl"]
    ?? throw new InvalidOperationException("La configuración Api:BaseUrl es obligatoria.");

builder.Services.Configure<SupabaseAuthOptions>(builder.Configuration.GetSection(SupabaseAuthOptions.SectionName));
builder.Services.Configure<ApiOptions>(builder.Configuration.GetSection(ApiOptions.SectionName));
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.Cookie.SameSite = SameSiteMode.Lax;
    options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
});
builder.Services.AddHttpClient<ISupabaseAuthService, SupabaseAuthService>(client =>
{
    client.BaseAddress = new Uri($"{supabaseAuthUrl.TrimEnd('/')}/");
});
builder.Services.AddHttpClient<IControlGastosApiClient, ControlGastosApiClient>(client =>
{
    client.BaseAddress = new Uri($"{apiBaseUrl.TrimEnd('/')}/");
});
builder.Services.AddControllersWithViews();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
}
app.UseRouting();

app.UseSession();
app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();


app.Run();
