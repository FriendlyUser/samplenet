using System;
using System.IO;
using System.Threading.Tasks;
using DotNetEnv;
using Google.Apis.Services;
using Google.Apis.YouTube.v3;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;

// Load environment variables (if using a .env file)
Env.Load();
var builder = WebApplication.CreateBuilder(args);

var port = Environment.GetEnvironmentVariable("PORT") ?? "3000";
var url = $"http://0.0.0.0:{port}";
var target = Environment.GetEnvironmentVariable("TARGET") ?? "World";

var app = builder.Build();

app.MapGet("/", () => $"Hello {target}!");

app.MapGet("/transcript/{videoId}", async (string videoId) =>
{
    // Get your API key from the environment
    var apiKey = Environment.GetEnvironmentVariable("YOUTUBE_API_KEY");
    if (string.IsNullOrWhiteSpace(apiKey))
    {
        return Results.Problem("YOUTUBE_API_KEY is not configured in the environment.");
    }

    // Initialize the YouTube API service
    var youtubeService = new YouTubeService(new BaseClientService.Initializer
    {
        ApiKey = apiKey,
        ApplicationName = "YouTube Transcript API"
    });

    try
    {
        // List caption tracks for the specified video.
        // The parts "id,snippet" provide caption metadata.
        var captionListRequest = youtubeService.Captions.List("id,snippet", videoId);
        var captionListResponse = await captionListRequest.ExecuteAsync();

        if (captionListResponse.Items == null || captionListResponse.Items.Count == 0)
        {
            return Results.NotFound("No caption tracks found for this video.");
        }

        // For simplicity, choose the first caption track.
        var captionTrack = captionListResponse.Items[0];
        Console.WriteLine($"Found caption track ID: {captionTrack.Id} ({captionTrack.Snippet.Language})");

        // Create a download request for the caption track.
        // Optionally, you can specify the format (e.g., "srt" or "vtt") via the 'tfmt' parameter.
        var downloadRequest = youtubeService.Captions.Download(captionTrack.Id);
        // Example: downloadRequest.Tfmt = "srt";

        // Download the transcript into a MemoryStream.
        using (var stream = new MemoryStream())
        {
            await downloadRequest.DownloadAsync(stream);
            stream.Position = 0;
            using (var reader = new StreamReader(stream))
            {
                var transcript = await reader.ReadToEndAsync();
                // Return the transcript as plain text.
                return Results.Text(transcript, "text/plain");
            }
        }
    }
    catch (Exception ex)
    {
        // Return any error encountered.
        return Results.Problem($"An error occurred while retrieving the transcript: {ex.Message}");
    }
});

app.Run(url);