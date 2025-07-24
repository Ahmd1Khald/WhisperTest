using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Net.Http.Headers;

namespace TestWhisperApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AudioController : ControllerBase
    {
        private readonly HttpClient _httpClient;

        public AudioController()
        {
            _httpClient = new HttpClient
            {
                Timeout = TimeSpan.FromMinutes(15)  // ✅ زود الوقت هنا
            };
        }

        [HttpPost("transcribe")]
        public async Task<IActionResult> Transcribe(IFormFile audioFile)
        {
            if (audioFile == null || audioFile.Length == 0)
                return BadRequest("Please upload a valid audio file.");

            var result = await SendAudioToWhisperAsync(audioFile);
            return Content(result, "application/json");
        }

        private async Task<string> SendAudioToWhisperAsync(IFormFile audioFile)
        {
            using var content = new MultipartFormDataContent();

            using var ms = new MemoryStream();
            await audioFile.CopyToAsync(ms);
            ms.Position = 0;

            var fileContent = new ByteArrayContent(ms.ToArray());
            fileContent.Headers.ContentType = new MediaTypeHeaderValue(audioFile.ContentType);

            content.Add(fileContent, "file", audioFile.FileName);

            // ✅ استخدم _httpClient بدل new HttpClient()
            var response = await _httpClient.PostAsync("http://localhost:8000/transcribe/", content);

            if (!response.IsSuccessStatusCode)
            {
                return $"Error: {response.StatusCode} - {await response.Content.ReadAsStringAsync()}";
            }

            return await response.Content.ReadAsStringAsync();
        }
    }
}
