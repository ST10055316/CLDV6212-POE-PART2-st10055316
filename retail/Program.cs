using ABCRetailers.Services;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.Configuration; // Ensure this is available
using System.Globalization;

// The previous version included an IAzureStorageService initialization block, 
// which is useful for creating tables/blobs/queues on startup in an MVC app. 
// I will re-add the necessary service registration for that service here 
// since it was present in the first code block you supplied.
// If IAzureStorageService is not needed for the MVC app, you can remove this later.
// Note: IAzureStorageService is not fully defined in this snippet.

var builder = WebApplication.CreateBuilder(args);

// ----------------------------------------------------------------------
// 1. Add Services to the container.
// ----------------------------------------------------------------------

// Add MVC Services
builder.Services.AddControllersWithViews();

// Register Azure Storage Service (Assuming IAzureStorageService and AzureStorageService exist)
// builder.Services.AddSingleton<IAzureStorageService, AzureStorageService>(); 

// ----------------------------------------------------------------------
// 2. Add Typed HttpClient for Azure Functions API communication
// ----------------------------------------------------------------------
builder.Services.AddHttpClient("Functions", (sp, client) =>
{
    var cfg = sp.GetRequiredService<IConfiguration>();

    // Prefer "FunctionsBaseUrl" or "Functions:BaseUrl" from appsettings.json
    var baseUrl = cfg["FunctionsBaseUrl"] ?? cfg["Functions:BaseUrl"];

    if (string.IsNullOrWhiteSpace(baseUrl))
        throw new InvalidOperationException(
            "? Azure Functions base URL is missing. Please add 'FunctionsBaseUrl' in appsettings.json.");

    // Ensure proper trailing slash and consistent /api/ path
    if (!baseUrl.EndsWith("/"))
        baseUrl += "/";

    // Append /api/ if the base URL doesn't already end with it
    if (baseUrl.EndsWith("/") && !baseUrl.EndsWith("/api/"))
        baseUrl += "api/";
    else if (!baseUrl.EndsWith("/api/"))
        baseUrl += "/api/";


    client.BaseAddress = new Uri(baseUrl);
    client.Timeout = TimeSpan.FromSeconds(100);
});

// Register the typed API client for DI (Assuming IFunctionsApi and FunctionsApiClient exist)
builder.Services.AddScoped<IFunctionsApi, FunctionsApiClient>();

// ----------------------------------------------------------------------
// 3. Optional: Increase multipart upload limits (for images/files)
// ----------------------------------------------------------------------
builder.Services.Configure<FormOptions>(options =>
{
    options.MultipartBodyLengthLimit = 50 * 1024 * 1024; // 50 MB
});

// ----------------------------------------------------------------------
// 4. Optional: Add logging
// ----------------------------------------------------------------------
builder.Services.AddLogging(logging =>
{
    logging.AddConsole();
    logging.AddDebug();
});

// ----------------------------------------------------------------------
// 5. Build the app
// ----------------------------------------------------------------------
var app = builder.Build();

// ----------------------------------------------------------------------
// 6. Optional: Initialize Storage (If using IAzureStorageService, uncomment below)
// ----------------------------------------------------------------------
/*
using (var scope = app.Services.CreateScope())
{
    var storageService = scope.ServiceProvider.GetRequiredService<IAzureStorageService>();
    await storageService.InitializeStorageAsync(); 
}
*/

// ----------------------------------------------------------------------
// 7. Culture settings (decimal handling fix)
// ----------------------------------------------------------------------
var culture = new CultureInfo("en-US");
CultureInfo.DefaultThreadCurrentCulture = culture;
CultureInfo.DefaultThreadCurrentUICulture = culture;

// ----------------------------------------------------------------------
// 8. Configure Middleware Pipeline
// ----------------------------------------------------------------------
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage(); // detailed errors during dev
}
else
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthorization();

// ----------------------------------------------------------------------
// 9. Map default route
// ----------------------------------------------------------------------
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

// ----------------------------------------------------------------------
// 10. Run the app
// ----------------------------------------------------------------------
app.Run();
