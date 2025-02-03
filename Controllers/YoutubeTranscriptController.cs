using System;
using System.IO;
using System.Threading.Tasks;
using Google.Apis.Services;
using Google.Apis.YouTube.v3;
using Microsoft.AspNetCore.Mvc;

namespace myapp.Controllers
{
    [ApiController]
    [Route("youtube/[controller]")]
    public class YoutubeManualTranscriptController : ControllerBase
    {
        /// <summary>
        /// Retrieves a transcript for the specified YouTube video using the YouTube Data API v3.
        /// </summary>
        /// <param name="videoId">The YouTube video ID.</param>
        /// <returns>The transcript text if found; otherwise an error message.</returns>
        [HttpGet("{videoId}")]
        public async Task<IActionResult> GetTranscript(string videoId)
        {
            // Get the API key from the environment.
            var apiKey = Environment.GetEnvironmentVariable("YOUTUBE_API_KEY");
            if (string.IsNullOrWhiteSpace(apiKey))
            {
                return Problem("YOUTUBE_API_KEY is not configured in the environment.");
            }

            // Initialize the YouTube service.
            var youtubeService = new YouTubeService(new BaseClientService.Initializer
            {
                ApiKey = apiKey,
                ApplicationName = "YouTube Transcript API"
            });

            try
            {
                // List caption tracks for the specified video.
                var captionListRequest = youtubeService.Captions.List("id,snippet", videoId);
                var captionListResponse = await captionListRequest.ExecuteAsync();

                if (captionListResponse.Items == null || captionListResponse.Items.Count == 0)
                {
                    return NotFound("No caption tracks found for this video.");
                }

                // For simplicity, choose the first caption track.
                var captionTrack = captionListResponse.Items[0];
                Console.WriteLine($"Found caption track ID: {captionTrack.Id} ({captionTrack.Snippet.Language})");

                // Create a download request for the caption track.
                var downloadRequest = youtubeService.Captions.Download(captionTrack.Id);
                // Optionally, set the format: e.g. downloadRequest.Tfmt = "srt";
                using (var stream = new MemoryStream())
                {
                    // Download the transcript into a memory stream.
                    await downloadRequest.DownloadAsync(stream);
                    stream.Position = 0;
                    using (var reader = new StreamReader(stream))
                    {
                        var transcript = await reader.ReadToEndAsync();
                        // Return the transcript as plain text.
                        return Content(transcript, "text/plain");
                    }
                }
            }
            catch (Exception ex)
            {
                // Return any error encountered.
                return Problem($"An error occurred while retrieving the transcript: {ex.Message}");
            }
        }
    }
}
