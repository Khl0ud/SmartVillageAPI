using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using SmartVillageAPI.Model;

namespace SmartVillageAPI.Services
{
    public class CameraSyncService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly string _recordingsPath;
        private readonly TimeSpan _period = TimeSpan.FromMinutes(1);

        public CameraSyncService(IServiceProvider serviceProvider, IOptions<CameraSettings> settings)
        {
            _serviceProvider = serviceProvider;
            _recordingsPath = settings.Value.RecordingsPath;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using var scope = _serviceProvider.CreateScope();
                    var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

                    if (!Directory.Exists(_recordingsPath))
                        Directory.CreateDirectory(_recordingsPath);

                    var cameras = await context.Cameras.ToListAsync(stoppingToken);

                    foreach (var cam in cameras)
                    {
                        string camFolder = Path.Combine(_recordingsPath, $"cam{cam.Id}");

                        if (!Directory.Exists(camFolder)) continue;

                        foreach (var filePath in Directory.GetFiles(camFolder, "*.mp4"))
                        {
                            var fileName = Path.GetFileName(filePath);

                            // ✅ check both fileName AND cameraId to avoid cross-camera duplicates
                            bool exists = await context.Recordings
                                .AnyAsync(r => r.FileUrl == fileName && r.CameraId == cam.Id,
                                    stoppingToken);

                            if (!exists)
                            {
                                context.Recordings.Add(new Recording
                                {
                                    CameraId = cam.Id,
                                    FileUrl = fileName,
                                    RecordedAt = File.GetCreationTime(filePath)
                                });
                            }
                        }
                    }

                    await context.SaveChangesAsync(stoppingToken);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[CameraSyncService Error]: {ex.Message}");
                }

                await Task.Delay(_period, stoppingToken);
            }
        }
    }
}