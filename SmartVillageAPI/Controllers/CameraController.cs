using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using SmartVillageAPI.Model;

namespace SmartVillageAPI.Controllers
{
    [ApiController]
    [Route("Surveillance")] // ✅ matches Flutter's ApiConstants.baseUrl + /Surveillance
    public class CameraController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly string _recordingsPath;

        public CameraController(AppDbContext context, IOptions<CameraSettings> settings)
        {
            _context = context;
            _recordingsPath = settings.Value.RecordingsPath;
        }

        // ─────────────────────────────────────────────
        // GET /Surveillance/cameras
        // Flutter: CameraApiService.getCameras()
        // ─────────────────────────────────────────────
        [HttpGet("cameras")]
        public async Task<ActionResult<IEnumerable<Camera>>> GetCameras()
        {
            var cameras = await _context.Cameras
                .Include(c => c.Recordings)
                .Include(c => c.Zone)
                .ToListAsync();

            var baseUrl = $"{Request.Scheme}://{Request.Host}/MediaRecords";

            foreach (var cam in cameras)
            {
                if (cam.Recordings != null)
                {
                    foreach (var rec in cam.Recordings)
                    {
                        rec.FullVideoUrl = $"{baseUrl}/cam{cam.Id}/{rec.FileUrl}";
                    }
                }
            }

            return Ok(cameras);
        }

        // ─────────────────────────────────────────────
        // GET /Surveillance/cameras/{id}
        // Flutter: CameraService.getCameraById()
        // ─────────────────────────────────────────────
        [HttpGet("cameras/{id}")]
        public async Task<ActionResult<Camera>> GetCamera(int id)
        {
            var camera = await _context.Cameras
                .Include(c => c.Recordings)
                .Include(c => c.Zone)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (camera == null) return NotFound();

            var baseUrl = $"{Request.Scheme}://{Request.Host}/MediaRecords";
            if (camera.Recordings != null)
            {
                foreach (var rec in camera.Recordings)
                {
                    rec.FullVideoUrl = $"{baseUrl}/cam{camera.Id}/{rec.FileUrl}";
                }
            }

            return Ok(camera);
        }

        // ─────────────────────────────────────────────
        // GET /Surveillance/recordings/{cameraId}
        // Flutter: CameraApiService.getRecordings(cameraId)
        // ─────────────────────────────────────────────
        [HttpGet("recordings/{cameraId}")]
        public async Task<ActionResult<IEnumerable<Recording>>> GetRecordingsByCameraId(int cameraId)
        {
            var recordings = await _context.Recordings
                .Where(r => r.CameraId == cameraId)
                .OrderByDescending(r => r.RecordedAt)
                .ToListAsync();

            var baseUrl = $"{Request.Scheme}://{Request.Host}/MediaRecords";

            foreach (var rec in recordings)
            {
                rec.FullVideoUrl = $"{baseUrl}/cam{cameraId}/{rec.FileUrl}";
            }

            return Ok(recordings);
        }

        // ─────────────────────────────────────────────
        // GET /Surveillance/cameras/{id}/stream
        // Returns HLS stream URL via MediaMTX for Flutter video player
        // ─────────────────────────────────────────────
        [HttpGet("cameras/{id}/stream")]
        public async Task<IActionResult> GetStreamUrl(int id)
        {
            var camera = await _context.Cameras.FindAsync(id);
            if (camera == null) return NotFound();

            // MediaMTX converts RTSP → HLS at this path automatically
            // Make sure mediamtx.yml has HLS enabled on port 8888
            var hlsUrl = $"{Request.Scheme}://{Request.Host}/hls/cam{id}/index.m3u8";

            return Ok(new
            {
                cameraId = id,
                hlsUrl = hlsUrl,       // ✅ Flutter video_player can play this
                rtspUrl = camera.StreamUrl  // kept for reference
            });
        }

        // ─────────────────────────────────────────────
        // POST /Surveillance/sync-recordings
        // Flutter: CameraService.syncRecordings()
        // ─────────────────────────────────────────────
        [HttpPost("sync-recordings")]
        public async Task<IActionResult> SyncFileSystemWithDb()
        {
            var cameras = await _context.Cameras.ToListAsync();

            foreach (var cam in cameras)
            {
                string camFolder = Path.Combine(_recordingsPath, $"cam{cam.Id}");

                if (Directory.Exists(camFolder))
                {
                    var videoFiles = Directory.GetFiles(camFolder, "*.mp4");
                    foreach (var filePath in videoFiles)
                    {
                        var fileName = Path.GetFileName(filePath);

                        // ✅ check both fileName AND cameraId to avoid cross-camera duplicates
                        bool exists = await _context.Recordings
                            .AnyAsync(r => r.FileUrl == fileName && r.CameraId == cam.Id);

                        if (!exists)
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

        // ─────────────────────────────────────────────
        // POST /Surveillance/cameras
        // Create a new camera
        // ─────────────────────────────────────────────
        [HttpPost("cameras")]
        public async Task<ActionResult<Camera>> CreateCamera(Camera camera)
        {
            _context.Cameras.Add(camera);
            await _context.SaveChangesAsync();
            return CreatedAtAction(nameof(GetCamera), new { id = camera.Id }, camera);
        }
    }
}