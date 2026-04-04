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

// CORS for Electron frontend
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin()
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
