using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using DocumentUpload.Mvc.Data;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.WindowsAzure.Storage;

namespace DocumentUpload.Mvc.Controllers
{
    public class DocumentsController : Controller
    {
        public string[] AllowedExtensions = new[] { ".doc", ".docx", ".xls", ".xlsx" };
        private readonly AppDbContext _db;
        private readonly CloudStorageAccount _storageAccount;

        public DocumentsController(AppDbContext db, IConfiguration configuration)
        {
            _db = db;
            _storageAccount = CloudStorageAccount.Parse(configuration.GetConnectionString("BlobStorage"));
        }
        public IActionResult Index()
        {
            var documents = _db.Documents.ToList();

            return View(documents);
        }

        [HttpGet]
        public IActionResult Upload()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Upload(IFormFile file)
        {
            var blobClient = _storageAccount.CreateCloudBlobClient();

            var container = blobClient.GetContainerReference("files");

            await container.CreateIfNotExistsAsync();

            var id = Guid.NewGuid();
            
            if (file.Length > 0)
            {
                var extension = Path.GetExtension(file.FileName);
                if (AllowedExtensions.Contains(extension))
                {
                    var documentName = $"{id}{extension}";
                    var blockBlock = container.GetBlockBlobReference(documentName);
                    using (var stream = file.OpenReadStream())
                    {
                        await blockBlock.UploadFromStreamAsync(stream);
                    }

                    _db.Documents.Add(new DocumentEntity
                    {
                        Id = id,
                        DocumentName = documentName,
                        FileName = file.FileName,
                        Location = blockBlock.Uri.AbsoluteUri
                    });

                    await _db.SaveChangesAsync();
                }
            }

            return RedirectToAction("Index");
        }
    }
}