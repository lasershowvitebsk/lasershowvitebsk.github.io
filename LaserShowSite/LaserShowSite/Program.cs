using LaserShowSite;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.Extensions.Localization;
using Blazored.LocalStorage;
using System.Globalization;
using LaserShowSite.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

// Выносим регистрацию сервисов в статический метод ниже
ConfigureServices(builder.Services, builder.HostEnvironment.BaseAddress);

var host = builder.Build();
var localStorage = host.Services.GetRequiredService<Blazored.LocalStorage.ILocalStorageService>();
var culture = await localStorage.GetItemAsync<string>("culture");
if (!string.IsNullOrEmpty(culture))
{
    var ci = new CultureInfo(culture);
    CultureInfo.DefaultThreadCurrentCulture = ci;
    CultureInfo.DefaultThreadCurrentUICulture = ci;
}
else
{
    var ru = new CultureInfo("ru-RU");
    CultureInfo.DefaultThreadCurrentCulture = ru;
    CultureInfo.DefaultThreadCurrentUICulture = ru;
    await localStorage.SetItemAsync("culture", "ru-RU");
}

await host.RunAsync();

// Этот метод ОБЯЗАТЕЛЬНО должен быть статическим с таким именем,
// чтобы сборщик пререндеринга мог его вызвать:
static void ConfigureServices(IServiceCollection services, string baseAddress)
{
    services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(baseAddress) });
    services.AddScoped<ChatService>();
    services.AddScoped<RequestService>();

    services.AddLocalization(options => options.ResourcesPath = "Resources");
    services.AddScoped<IStringLocalizer<App>, StringLocalizer<App>>();

    services.AddBlazoredLocalStorage();
}