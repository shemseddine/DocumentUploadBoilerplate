using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using DocumentUpload.Mvc.Data;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace DocumentUpload.Mvc.Controllers
{
    public class DocumentsController : Controller
    {
        public string[] AllowedExtensions = new[] { ".doc", ".docx", ".xls", ".xlsx" };
        private readonly AppDbContext _db;

        public DocumentsController(AppDbContext db)
        {
            _db = db;
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
            var filePath = Path.GetTempFileName();
            if (file.Length > 0)
            {
                var extension = Path.GetExtension(file.FileName);
                if (AllowedExtensions.Contains(extension))
                {
                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await file.CopyToAsync(stream);
                    }

                    _db.Documents.Add(new DocumentEntity
                    {
                        Id = Guid.NewGuid(),
                        FileName = file.FileName,
                        Location = filePath
                    });

                    await _db.SaveChangesAsync();
                }
            }

            return RedirectToAction("Index");
        }
    }
}