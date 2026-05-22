using AttendanceAPI.Data;
using AttendanceAPI.Models;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace AttendanceAPI.Controllers
{
    [Route("api/faces")]
    [ApiController]
    public class FaceController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IHttpClientFactory _httpClientFactory;

        public FaceController(AppDbContext context, IHttpClientFactory httpClientFactory)
        {
            _context = context;
            _httpClientFactory = httpClientFactory;
        }

        [HttpPost("upload")]
        public async Task<IActionResult> Upload([FromForm] int userId, [FromForm] List<IFormFile> images)
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null) return NotFound("User not found");

            if (images == null || images.Count == 0) return BadRequest("No images uploaded");

            using var content = new MultipartFormDataContent();
            // Since stream lifetime is managed by standard ASP.NET form binding, 
            // we copy them to MemoryStreams to ensure they stay open.
            foreach (var img in images)
            {
                var ms = new MemoryStream();
                await img.CopyToAsync(ms);
                ms.Position = 0;
                var streamContent = new StreamContent(ms);
                streamContent.Headers.Add("Content-Type", img.ContentType);
                content.Add(streamContent, "images", img.FileName);
            }

            var client = _httpClientFactory.CreateClient("AIService");
            var response = await client.PostAsync("/api/ai/register", content);

            if (!response.IsSuccessStatusCode)
            {
                return StatusCode((int)response.StatusCode, await response.Content.ReadAsStringAsync());
            }

            var responseBody = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<EmbeddingResponse>(responseBody, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            if (result != null && result.Embeddings != null)
            {
                if (result.Embeddings.Count == 0)
                {
                    return BadRequest("No faces were detected in the provided images.");
                }

                foreach (var emb in result.Embeddings)
                {
                    _context.FaceEmbeddings.Add(new FaceEmbedding
                    {
                        UserId = userId,
                        Embedding = JsonSerializer.Serialize(emb)
                    });
                }
                await _context.SaveChangesAsync();
                
                // Notify AI service to reload embeddings
                await client.PostAsync("/api/ai/reload", null);
                
                return Ok(new { message = "Faces registered successfully" });
            }

            return BadRequest("Failed to parse embeddings");
        }
    }

    public class EmbeddingResponse
    {
        public List<List<float>>? Embeddings { get; set; }
    }
}
