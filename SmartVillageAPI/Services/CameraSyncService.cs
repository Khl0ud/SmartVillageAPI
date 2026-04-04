using Microsoft.EntityFrameworkCore;
using SmartVillageAPI.Model;
using System.IO;

namespace SmartVillageAPI.Services
{
    public class CameraSyncService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        // هنخليها كل دقيقة للتجربة، وبعدين ممكن تخليها 5 دقايق
        private readonly TimeSpan _period = TimeSpan.FromMinutes(1);

        public CameraSyncService(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using (var scope = _serviceProvider.CreateScope())
                    {
                        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                        // تأكدي أن المسار هو نفسه المستخدم في كل المشروع
                        var recordingsPath = "D:\\MediaMTX\\recordings";

                        if (!Directory.Exists(recordingsPath))
                        {
                            Directory.CreateDirectory(recordingsPath);
                        }

                        var cameras = await context.Cameras.ToListAsync();
                        foreach (var cam in cameras)
                        {
                            string camFolder = Path.Combine(recordingsPath, $"cam{cam.Id}");
                            if (Directory.Exists(camFolder))
                            {
                                var videoFiles = Directory.GetFiles(camFolder, "*.mp4");
                                foreach (var filePath in videoFiles)
                                {
                                    var fileName = Path.GetFileName(filePath);

                                    // التحقق إذا كان التسجيل موجود مسبقاً
                                    bool exists = await context.Recordings.AnyAsync(r => r.FileUrl == fileName);

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
                        }
                        await context.SaveChangesAsync();
                    }
                }
                catch (Exception ex)
                {
                    // تسجيل الخطأ في الـ Console عشان تتابعي لو حصل مشكلة في المسارات
                    Console.WriteLine($"[CameraSyncService Error]: {ex.Message}");
                }

                await Task.Delay(_period, stoppingToken);
            }
        }
    }
}