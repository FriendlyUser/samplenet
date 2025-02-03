using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;

namespace myapp.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class WhisperTranscriptController : ControllerBase
    {
        /// <summary>
        /// Retrieves a transcript for the specified YouTube video using yt-dlp, ffmpeg, and openai-whisper.
        /// </summary>
        /// <param name="videoId">The YouTube video ID.</param>
        /// <returns>The transcript text if successful; otherwise an error message.</returns>
        [HttpGet("{videoId}")]
        public async Task<IActionResult> GetTranscript(string videoId)
        {
            // Build the full YouTube URL.
            var youtubeUrl = $"https://www.youtube.com/watch?v={videoId}";
            // Create a temporary directory for intermediate files.
            var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            Directory.CreateDirectory(tempDir);

            try
            {
                // Step 1: Download audio from YouTube using yt-dlp.
                var audioFilePath = await DownloadAudioAsync(youtubeUrl, tempDir);
                if (audioFilePath == null)
                {
                    return Problem("Failed to download audio using yt-dlp.");
                }

                // Step 2: Convert the audio file to WAV using ffmpeg.
                var wavFilePath = await ConvertToWavAsync(audioFilePath, tempDir);
                if (wavFilePath == null)
                {
                    return Problem("Failed to convert audio to WAV using ffmpeg.");
                }

                // Step 3: Transcribe the WAV file using openai-whisper.
                var transcript = await TranscribeAudioAsync(wavFilePath, tempDir);
                if (string.IsNullOrWhiteSpace(transcript))
                {
                    return Problem("Failed to transcribe audio using Whisper.");
                }

                return Content(transcript, "text/plain");
            }
            finally
            {
                // Cleanup: Remove temporary directory and its files.
                try
                {
                    Directory.Delete(tempDir, true);
                }
                catch (Exception ex)
                {
                    Console.Error.WriteLine($"Error cleaning up temporary files: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// Uses yt-dlp to download the best available audio for the provided YouTube URL.
        /// </summary>
        private async Task<string> DownloadAudioAsync(string youtubeUrl, string tempDir)
        {
            // Define the output template (yt-dlp replaces %(ext)s with the proper extension).
            string outputTemplate = Path.Combine(tempDir, "audio.%(ext)s");

            var psi = new ProcessStartInfo
            {
                FileName = "yt-dlp",
                Arguments = $"-f bestaudio -o \"{outputTemplate}\" {youtubeUrl}",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using (var process = Process.Start(psi))
            {
                if (process == null)
                    return "";

                await process.WaitForExitAsync();

                if (process.ExitCode != 0)
                {
                    string errorOutput = await process.StandardError.ReadToEndAsync();
                    Console.Error.WriteLine("yt-dlp error: " + errorOutput);
                    return "";
                }
            }

            // Locate the downloaded audio file.
            var files = Directory.GetFiles(tempDir, "audio.*");
            return files.FirstOrDefault();
        }

        /// <summary>
        /// Uses ffmpeg to convert the downloaded audio file to a WAV file.
        /// </summary>
        private async Task<string> ConvertToWavAsync(string inputFilePath, string tempDir)
        {
            // Define the output WAV file path.
            string wavFilePath = Path.Combine(tempDir, "audio.wav");

            var psi = new ProcessStartInfo
            {
                FileName = "ffmpeg",
                Arguments = $"-i \"{inputFilePath}\" -ac 1 -ar 16000 \"{wavFilePath}\" -y",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using (var process = Process.Start(psi))
            {
                if (process == null)
                    return "";

                await process.WaitForExitAsync();

                if (process.ExitCode != 0)
                {
                    string errorOutput = await process.StandardError.ReadToEndAsync();
                    Console.Error.WriteLine("ffmpeg error: " + errorOutput);
                    return "";
                }
            }

            return wavFilePath;
        }

        /// <summary>
        /// Uses openai-whisper to transcribe the WAV file into text.
        /// </summary>
        private async Task<string> TranscribeAudioAsync(string wavFilePath, string tempDir)
        {
            // Create an output directory for Whisper's transcript.
            var whisperOutputDir = Path.Combine(tempDir, "whisper_output");
            Directory.CreateDirectory(whisperOutputDir);

            var psi = new ProcessStartInfo
            {
                FileName = "whisper",
                Arguments = $"\"{wavFilePath}\" --model tiny --output_format txt --output_dir \"{whisperOutputDir}\"",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using (var process = Process.Start(psi))
            {
                if (process == null)
                    return "";

                await process.WaitForExitAsync();

                if (process.ExitCode != 0)
                {
                    string errorOutput = await process.StandardError.ReadToEndAsync();
                    Console.Error.WriteLine("Whisper error: " + errorOutput);
                    return "";
                }
            }

            // Whisper typically names the transcript file based on the audio file.
            string transcriptFileName = Path.GetFileNameWithoutExtension(wavFilePath) + ".txt";
            string transcriptFilePath = Path.Combine(whisperOutputDir, transcriptFileName);

            if (!System.IO.File.Exists(transcriptFilePath))
            {
                // If the expected file is not found, try picking up any .txt file.
                var txtFiles = Directory.GetFiles(whisperOutputDir, "*.txt");
                transcriptFilePath = txtFiles.FirstOrDefault();
            }

            if (transcriptFilePath == null)
                return null;

            return await System.IO.File.ReadAllTextAsync(transcriptFilePath);
        }
    }
}
