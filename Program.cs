using DotNetEnv;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

// Load environment variables (if using a .env file)
Env.Load();

var builder = WebApplication.CreateBuilder(args);

// Register controllers with the DI container.
builder.Services.AddControllers();

var port = Environment.GetEnvironmentVariable("PORT") ?? "3000";
var url = $"http://0.0.0.0:{port}";
var target = Environment.GetEnvironmentVariable("TARGET") ?? "World";

var app = builder.Build();

// Optional: Use an inline endpoint for the root path.
app.MapGet("/", () => $"Hello {target}!");


// Map attribute-routed controllers.
// The framework will discover controllers in your assembly (e.g. YoutubeTranscriptController and WhisperTranscriptController)
app.MapControllers();

app.Run(url);
