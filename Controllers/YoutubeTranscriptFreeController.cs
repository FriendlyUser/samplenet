using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using YoutubeTranscriptApi;
using System.Linq;

namespace myapp.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class YoutubeTranscriptApiFreeController : ControllerBase
    {
        /// <summary>
        /// Retrieves the English transcript for a given YouTube video using the YoutubeTranscriptApi library.
        /// </summary>
        /// <param name="videoId">The YouTube video ID.</param>
        /// <returns>An IActionResult containing the transcript text, or an error message.</returns>
        [HttpGet("{videoId}")]
        public async Task<IActionResult> GetTranscript(string videoId)
        {
            YouTubeTranscriptApi? youTubeTranscriptApi = null;
            try
            {
                youTubeTranscriptApi = new YouTubeTranscriptApi();
                var transcriptItems = youTubeTranscriptApi.GetTranscript(videoId, new[] { "en" });

                if (transcriptItems == null || !transcriptItems.Any())
                {
                    return NotFound($"No English transcript found for video ID: {videoId}");
                }

                // Format the transcript into a single string.
                var transcriptText = string.Join("\n", transcriptItems.Select(item => $"{item.Start:F2} - {item.Text}"));

                return Content(transcriptText, "text/plain");
            }
            catch (YoutubeTranscriptApi.NoTranscriptFound ex)
            {
                return NotFound($"Could not retrieve transcript for video ID: {videoId}.  Reason: {ex.Message}");
            }
            catch (YoutubeTranscriptApi.TranscriptsDisabled ex)
            {
                return NotFound($"Transcripts are disabled for video ID: {videoId}. Reason: {ex.Message}");
            }
            catch (Exception ex)
            {
                return Problem($"An error occurred while retrieving the transcript: {ex.Message}");
            }
            finally
            {
                // youTubeTranscriptApi?.Dispose();
                // youTubeTranscriptApi?.Dispose()
            }
        }

        /// <summary>
        /// Lists the available transcripts for a given YouTube video.
        /// </summary>
        /// <param name="videoId">The YouTube video ID.</param>
        /// <returns>An IActionResult containing the list of transcripts, or an error message.</returns>
        [HttpGet("list/{videoId}")]
        public async Task<IActionResult> ListTranscripts(string videoId)
        {
            YouTubeTranscriptApi? youTubeTranscriptApi = null;
            try
            {
                youTubeTranscriptApi = new YouTubeTranscriptApi();
                var transcriptList = youTubeTranscriptApi.ListTranscripts(videoId);

                if (transcriptList == null)
                {
                    return NotFound($"No transcripts found for video ID: {videoId}");
                }

                //Return the list of transcripts
                var transcripts = transcriptList.ToList();
                return Ok(transcripts.Select(t => new
                {
                    t.VideoId,
                    t.Language,
                    t.LanguageCode,
                    t.IsGenerated,
                    t.IsTranslatable
                }));
            }
            catch (YoutubeTranscriptApi.NoTranscriptFound ex)
            {
                return NotFound($"Could not retrieve transcript list for video ID: {videoId}.  Reason: {ex.Message}");
            }
            catch (YoutubeTranscriptApi.TranscriptsDisabled ex)
            {
                return NotFound($"Transcripts are disabled for video ID: {videoId}. Reason: {ex.Message}");
            }
            catch (Exception ex)
            {
                return Problem($"An error occurred while retrieving the transcript list: {ex.Message}");
            }
            finally
            {
                // youTubeTranscriptApi?.Dispose();
                //return null;
            }
        }


          /// <summary>
        /// Retrieves the English transcript for a given YouTube video using the YoutubeTranscriptApi library,
        /// returning the data in structured JSON format.
        /// </summary>
        /// <param name="videoId">The YouTube video ID.</param>
        /// <returns>An IActionResult containing a structured list of transcript items, or an error message.</returns>
        [HttpGet("structured/{videoId}")]
        public async Task<IActionResult> GetTranscriptStructured(string videoId)
        {
            YouTubeTranscriptApi? youTubeTranscriptApi = null;
            try
            {
                youTubeTranscriptApi = new YouTubeTranscriptApi();
                var transcriptItems = youTubeTranscriptApi.GetTranscript(videoId, new[] { "en" });

                if (transcriptItems == null || !transcriptItems.Any())
                {
                    return NotFound($"No English transcript found for video ID: {videoId}");
                }

                // Format the transcript into a structured format (list of objects).
                var transcriptData = transcriptItems.Select(item => new
                {
                    Start = item.Start,
                    Duration = item.Duration,
                    Text = item.Text
                }).ToList();

                return Ok(transcriptData);
            }
            catch (YoutubeTranscriptApi.NoTranscriptFound ex)
            {
                return NotFound($"Could not retrieve transcript for video ID: {videoId}.  Reason: {ex.Message}");
            }
            catch (YoutubeTranscriptApi.TranscriptsDisabled ex)
            {
                return NotFound($"Transcripts are disabled for video ID: {videoId}. Reason: {ex.Message}");
            }
            catch (Exception ex)
            {
                return Problem($"An error occurred while retrieving the transcript: {ex.Message}");
            }
            finally
            {
                // youTubeTranscriptApi?.Dispose();
            }
        }
    }
}