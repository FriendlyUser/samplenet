using GenerativeAI;
using Google.Apis;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using GenerativeAI.Types; // Import the GenerativeAI.Types namespace

namespace myapp.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class GeminiController : ControllerBase
    {
        private readonly GenerativeAI.GoogleAi _googleAI;
        private readonly GenerativeModel _textModel;
        private readonly GenerativeModel _imageModel;
        private readonly IConfiguration _configuration;
        private readonly ILogger<GeminiController> _logger;

        public GeminiController(IConfiguration configuration, ILogger<GeminiController> logger)
        {
            _configuration = configuration;
            _logger = logger;

            var apiKey = _configuration["GeminiApiKey"];

            if (string.IsNullOrEmpty(apiKey))
            {
                _logger.LogError("GeminiApiKey is missing from configuration.");
                throw new InvalidOperationException("GeminiApiKey is missing from configuration.");
            }

            _googleAI = new GenerativeAI.GoogleAi(apiKey);

            _textModel = _googleAI.CreateGenerativeModel(_configuration["GeminiTextModelName"] ?? "models/gemini-1.5-flash");
            _imageModel = _googleAI.CreateGenerativeModel(_configuration["GeminiImageModelName"] ?? "models/gemini-1.5-flash");

            _logger.LogInformation("GeminiController initialized.");
        }

        [HttpGet("text/{prompt}")]
        public async Task<IActionResult> GenerateText(string prompt)
        {
            if (string.IsNullOrEmpty(prompt))
            {
                return BadRequest("Prompt is required.");
            }

            try
            {
                var response = await _textModel.GenerateContentAsync(prompt);
                return Ok(new { Response = response.Text() });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while processing the text request.");
                return Problem($"An error occurred while processing the request: {ex.Message}");
            }
        }


        [HttpPost("multimodal")]
        public async Task<IActionResult> GenerateMultimodal(IFormFile file, [FromForm] string prompt)
        {
            if (string.IsNullOrEmpty(prompt))
            {
                return BadRequest("Prompt is required.");
            }

            if (file == null || file.Length == 0)
            {
                return BadRequest("File is required.");
            }

            var allowedExtensions = new string[] { ".png", ".jpg", ".jpeg", ".pdf" };
            var fileExtension = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (!allowedExtensions.Contains(fileExtension))
            {
                return BadRequest($"Invalid file type. Allowed types: {string.Join(", ", allowedExtensions)}");
            }

            try
            {
                using (var stream = file.OpenReadStream())
                {
                    byte[] fileBytes;
                    using (var memoryStream = new MemoryStream())
                    {
                        await stream.CopyToAsync(memoryStream);
                        fileBytes = memoryStream.ToArray();
                    }

                    // Create a Part object for the image
                    var imagePart = new Part {
                        InlineData = new Blob {  // Changed InlineData to Blob
                            Data = Convert.ToBase64String(fileBytes),
                            MimeType = file.ContentType
                        }
                    };

                    // Create a Part object for the text
                    var textPart = new Part {
                        Text = prompt
                    };

                    // Create a GenerateContentRequest
                    var request = new GenerateContentRequest {
                        Contents = new List<Content> {
                            new Content {
                                Parts = new List<Part> { textPart, imagePart },
                                Role = "user" // Or any appropriate role
                            }
                        }
                    };

                    var response = await _imageModel.GenerateContentAsync(request);

                    return Ok(new { Response = response.Text() });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while processing the multimodal request.");
                return Problem($"An error occurred while processing the request: {ex.Message}");
            }
        }
    }
}