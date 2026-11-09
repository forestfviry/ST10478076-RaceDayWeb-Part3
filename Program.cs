using System.Globalization;
using Microsoft.AspNetCore.Mvc;
using RaceDayWeb.Services;

// Number inputs always post "5.5", but South African regional settings expect "5,5".
// Using the invariant culture keeps decimals like distances binding correctly everywhere.
CultureInfo.DefaultThreadCurrentCulture = CultureInfo.InvariantCulture;
CultureInfo.DefaultThreadCurrentUICulture = CultureInfo.InvariantCulture;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews(options =>
{
    // Every POST needs a valid anti-forgery token.
    options.Filters.Add(new AutoValidateAntiforgeryTokenAttribute());
});

// Session holds the JWT, the user's role and their display name.
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.Name = ".RaceDay.Session";
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});
builder.Services.AddHttpContextAccessor();

// The MVC app talks to the API through this one typed client.
var apiBase = builder.Configuration["Api:BaseUrl"]
    ?? throw new InvalidOperationException("Api:BaseUrl is missing from appsettings.json.");
builder.Services.AddHttpClient<IApiClient, ApiClient>(client =>
{
    client.BaseAddress = new Uri(apiBase);
    client.Timeout = TimeSpan.FromSeconds(30);
});

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseStatusCodePagesWithReExecute("/Home/Status", "?code={0}");

// Switched off inside Docker, where the container only listens on plain http.
if (builder.Configuration.GetValue("UseHttpsRedirection", true))
    app.UseHttpsRedirection();

app.UseStaticFiles();
app.UseRouting();
app.UseSession();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
