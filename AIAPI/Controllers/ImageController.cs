using Microsoft.AspNetCore.Mvc;

namespace AIAPI.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class ImageController : ControllerBase
    {
        private readonly IConfiguration _configuration;
        private readonly CloudBlobContainer _blobContainer;

        public ImageController(IConfiguration configuration)
        {
            _configuration = configuration;

            // Initialize Azure Blob Storage
            var storageAccount = CloudStorageAccount.Parse(_configuration["AzureBlobStorage:ConnectionString"]);
            var blobClient = storageAccount.CreateCloudBlobClient();
            _blobContainer = blobClient.GetContainerReference(_configuration["AzureBlobStorage:ContainerName"]);

            // Create the container if it doesn't exist
            _blobContainer.CreateIfNotExists();
        }

        [HttpGet("{imageName}")]
        public async Task<IActionResult> GetImage(string imageName)
        {
            var blobReference = _blobContainer.GetBlockBlobReference(imageName);

            if (!await blobReference.ExistsAsync())
                return NotFound();

            var stream = new MemoryStream();
            await blobReference.DownloadToStreamAsync(stream);
            stream.Seek(0, SeekOrigin.Begin);

            return File(stream, "image/jpeg"); // Adjust content type based on your image type
        }

        [HttpPost]
        public async Task<IActionResult> UploadImage([FromForm] IFormFile file)
        {
            if (file == null || file.Length == 0)
                return BadRequest("Invalid file");

            var imageName = Guid.NewGuid().ToString();
            var blobReference = _blobContainer.GetBlockBlobReference(imageName);

            using (var stream = file.OpenReadStream())
            {
                await blobReference.UploadFromStreamAsync(stream);
            }

            return CreatedAtAction(nameof(GetImage), new { imageName }, null);
        }

        [HttpDelete("{imageName}")]
        public async Task<IActionResult> DeleteImage(string imageName)
        {
            var blobReference = _blobContainer.GetBlockBlobReference(imageName);

            if (!await blobReference.ExistsAsync())
                return NotFound();

            await blobReference.DeleteAsync();
            return NoContent();
        }
    }