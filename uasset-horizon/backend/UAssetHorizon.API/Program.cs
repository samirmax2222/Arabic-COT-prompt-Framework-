using UAssetHorizon.API.Security;
using UAssetHorizon.Core.AES;
using UAssetHorizon.Core.Decryption;
using UAssetHorizon.Core.Parsers;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

// Register core services
builder.Services.AddSingleton<UniversalAssetParser>();
builder.Services.AddSingleton<AesKeyScanner>();
builder.Services.AddSingleton<AesKeyDatabase>();
builder.Services.AddSingleton<PakDecryptor>();
builder.Services.AddSingleton<PathSanitizer>();

// CORS scoped to the Electron app and the local Vite dev server.
// Electron file:// pages send "Origin: null", so we also allow that.
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.WithOrigins(
                  "http://localhost:5173",  // Vite dev server
                  "http://127.0.0.1:5173",
                  "null"                    // Electron file:// origin
              )
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

var app = builder.Build();

app.UseCors();
app.MapControllers();

// Initialize key database
var keyDb = app.Services.GetRequiredService<AesKeyDatabase>();
await keyDb.LoadAsync();

var port = args.Length > 0 ? args[0] : "5175";
app.Urls.Add($"http://localhost:{port}");

Console.WriteLine($"UAsset Horizon API running on http://localhost:{port}");
app.Run();
