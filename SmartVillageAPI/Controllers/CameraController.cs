using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartVillageAPI.Model;

namespace SmartVillageAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class CameraController : ControllerBase
    {
        private readonly AppDbContext _context;
        // تأكدي أن هذا المسار مطابق للموجود في Program.cs وفي ملف mediamtx.yml
        private readonly string _recordingsPath = "D:\\MediaMTX\\recordings";

        public CameraController(AppDbContext context)
        {
            _context = context;
        }

        // 1. جلب الكاميرات مع بناء روابط الفيديوهات كاملة للموبايل
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Camera>>> GetCameras()
        {
            var cameras = await _context.Cameras
                .Include(c => c.Recordings)
                .Include(c => c.Zone)
                .ToListAsync();

            // بناء الـ Base URL الخاص بالسيرفر (مثلاً http://192.168.1.10:5000/recordings)
            var baseUrl = $"{Request.Scheme}://{Request.Host}/recordings";

            foreach (var cam in cameras)
            {
                if (cam.Recordings != null)
                {
                    foreach (var rec in cam.Recordings)
                    {
                        // تركيب الرابط النهائي: baseUrl / اسم_فولدر_الكاميرا / اسم_الملف
                        // النتيجة المتوقعة: http://ip:port/recordings/cam1/video.mp4
                        rec.FullVideoUrl = $"{baseUrl}/cam{cam.Id}/{rec.FileUrl}";
                    }
                }
            }

            return Ok(cameras);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<Camera>> GetCamera(int id)
        {
            var camera = await _context.Cameras
                .Include(c => c.Recordings)
                .Include(c => c.Zone)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (camera == null) return NotFound();

            // بناء الـ URL للكاميرا الواحدة أيضاً
            var baseUrl = $"{Request.Scheme}://{Request.Host}/recordings";
            if (camera.Recordings != null)
            {
                foreach (var rec in camera.Recordings)
                {
                    rec.FullVideoUrl = $"{baseUrl}/cam{camera.Id}/{rec.FileUrl}";
                }
            }

            return Ok(camera);
        }

        // 2. تحديث الداتابيز بملفات الفيديو الموجودة على الهارد ديسك
        [HttpPost("sync-recordings")]
        public async Task<IActionResult> SyncFileSystemWithDb()
        {
            var cameras = await _context.Cameras.ToListAsync();

            foreach (var cam in cameras)
            {
                // مسار الفولدر الخاص بكل كاميرا (مثلاً D:\MediaMTX\recordings\cam1)
                string camFolder = Path.Combine(_recordingsPath, $"cam{cam.Id}");

                if (Directory.Exists(camFolder))
                {
                    var videoFiles = Directory.GetFiles(camFolder, "*.mp4");
                    foreach (var filePath in videoFiles)
                    {
                        var fileName = Path.GetFileName(filePath);

                        // التحقق إذا كان الملف مسجل مسبقاً في الداتابيز لتجنب التكرار
                        if (!await _context.Recordings.AnyAsync(r => r.FileUrl == fileName))
                        {
                            _context.Recordings.Add(new Recording
                            {
                                CameraId = cam.Id,
                                FileUrl = fileName,
                                RecordedAt = System.IO.File.GetCreationTime(filePath)
                            });
                        }
                    }
                }
            }

            await _context.SaveChangesAsync();
            return Ok(new { Message = "Database synced successfully with recordings folder." });
        }

        [HttpPost]
        public async Task<ActionResult<Camera>> CreateCamera(Camera camera)
        {
            _context.Cameras.Add(camera);
            await _context.SaveChangesAsync();
            return CreatedAtAction(nameof(GetCamera), new { id = camera.Id }, camera);
        }
    }
}