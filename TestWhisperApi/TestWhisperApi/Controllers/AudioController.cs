using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

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
                Timeout = TimeSpan.FromMinutes(15)
            };
        }

        [HttpPost("transcribe-and-analyze")]
        public async Task<IActionResult> TranscribeAndAnalyze(IFormFile audioFile)
        {
            if (audioFile == null || audioFile.Length == 0)
                return BadRequest("Please upload a valid audio file.");

            // 1. تفريغ الصوت
            var transcription = await SendAudioToWhisperAsync(audioFile);
            if (transcription.StartsWith("Error:"))
                return BadRequest(transcription);

            // 2. استخراج النص من JSON
            var transcriptionText = ExtractTextFromJson(transcription);
            if (string.IsNullOrEmpty(transcriptionText))
                return BadRequest("No text found in Whisper response.");

            // 3. إرسال النص للتحليل الذكي
            var analyzedResult = await SendTextToAIModelAsync(transcriptionText);

            return Content(analyzedResult, "application/json");
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

            var response = await _httpClient.PostAsync("http://localhost:8000/transcribe/", content);
            if (!response.IsSuccessStatusCode)
                return $"Error: {response.StatusCode} - {await response.Content.ReadAsStringAsync()}";

            return await response.Content.ReadAsStringAsync();
        }

        private async Task<string> SendTextToAIModelAsync(string text)
        {
            var requestData = new { transcript = text };
            var json = JsonSerializer.Serialize(requestData);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync("http://localhost:8001/analyze_meeting/", content);
            if (!response.IsSuccessStatusCode)
                return $"Error: {response.StatusCode} - {await response.Content.ReadAsStringAsync()}";

            return await response.Content.ReadAsStringAsync();
        }


        private string ExtractTextFromJson(string json)
        {
            try
            {
                using var doc = JsonDocument.Parse(json);
                if (doc.RootElement.TryGetProperty("text", out var textElement))
                    return textElement.GetString() ?? "";
            }
            catch
            {
                // log error if needed
            }
            return "";
        }
    }
}
