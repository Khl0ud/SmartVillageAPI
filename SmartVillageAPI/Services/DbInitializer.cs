using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using SmartVillageAPI.Model;

namespace SmartVillageAPI.Services
{
    public static class DbInitializer
    {
        public static async Task Seed(IServiceProvider serviceProvider)
        {
            using var scope = serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

            // 1. Seed User
            var testEmail = "test@example.com";
            var user = await userManager.FindByEmailAsync(testEmail);
            if (user == null)
            {
                user = new ApplicationUser
                {
                    UserName = testEmail,
                    Email = testEmail,
                    FullName = "Test User",
                    WalletBalance = 500.0,
                    PhoneNumber = "01234567890"
                };
                var result = await userManager.CreateAsync(user, "Password123!");
                if (!result.Succeeded)
                {
                    throw new Exception("Failed to seed user: " + string.Join(", ", result.Errors.Select(e => e.Description)));
                }
            }

            // 2. Seed Devices (if not exists)
            if (!context.Devices.Any())
            {
                var devices = new List<Device>
                {
                    // Smart Home (Zone 1)
                    new Device { Name = "Main Light", Type = DeviceType.Actuator, IsActive = true, CurrentState = "OFF", ZoneId = 1 },
                    new Device { Name = "Temperature Sensor", Type = DeviceType.Sensor, IsActive = true, CurrentState = "Normal", ZoneId = 1 },
                    
                    // Agriculture (Zone 2)
                    new Device { Name = "Water Pump", Type = DeviceType.Actuator, IsActive = true, CurrentState = "OFF", ZoneId = 2 },
                    new Device { Name = "Soil Moisture Sensor", Type = DeviceType.Sensor, IsActive = true, CurrentState = "Dry", ZoneId = 2 },

                    // Parking (Zone 3)
                    new Device { Name = "Spot 1", Type = DeviceType.Sensor, IsActive = true, CurrentState = "Empty", ZoneId = 3 },
                    new Device { Name = "Spot 2", Type = DeviceType.Sensor, IsActive = true, CurrentState = "Occupied", ZoneId = 3 },

                    // Waste Mgmt (Zone 6)
                    new Device { Name = "Main Trash Bin", Type = DeviceType.Sensor, IsActive = true, CurrentState = "40%", ZoneId = 6 }
                };
                context.Devices.AddRange(devices);
                await context.SaveChangesAsync();
            }

            // 3. Seed Sensor Readings
            if (!context.SensorReadings.Any())
            {
                var tempSensor = context.Devices.FirstOrDefault(d => d.Name == "Temperature Sensor");
                if (tempSensor != null)
                {
                    context.SensorReadings.Add(new SensorReading { DeviceId = tempSensor.Id, Type = ReadingType.Temperature, Value = 25.5, CreatedAt = DateTime.Now });
                    context.SensorReadings.Add(new SensorReading { DeviceId = tempSensor.Id, Type = ReadingType.Humidity, Value = 45.0, CreatedAt = DateTime.Now });
                }
                await context.SaveChangesAsync();
            }

            // 4. Seed Notifications
            if (!context.Notifications.Any())
            {
                context.Notifications.Add(new Notification { UserId = user.Id, Title = "Welcome", Message = "Welcome to Smart Village!", CreatedAt = DateTime.Now, IsRead = false });
                context.Notifications.Add(new Notification { UserId = user.Id, Title = "Alert", Message = "Gas leak detected in Zone 1!", CreatedAt = DateTime.Now.AddHours(-1), IsRead = true });
                await context.SaveChangesAsync();
            }

            // 5. Seed Cameras
            if (!context.Cameras.Any())
            {
                context.Cameras.Add(new Camera { Name = "Main Gate", Location = "Entrance", StreamUrl = "http://example.com/live1", ZoneId = 5, CreatedAt = DateTime.Now });
                context.Cameras.Add(new Camera { Name = "Parking Area", Location = "Level 1", StreamUrl = "http://example.com/live2", ZoneId = 3, CreatedAt = DateTime.Now });
                await context.SaveChangesAsync();
            }

            // 6. Seed Recordings
            if (!context.Recordings.Any())
            {
                var camera = context.Cameras.FirstOrDefault();
                if (camera != null)
                {
                    context.Recordings.Add(new Recording { CameraId = camera.Id, FileUrl = "test_video.mp4", RecordedAt = DateTime.Now.AddDays(-1) });
                }
                await context.SaveChangesAsync();
            }

            // 7. Seed Activity Logs
            if (!context.ActivityLogs.Any())
            {
                var light = context.Devices.FirstOrDefault(d => d.Name == "Main Light");
                if (light != null)
                {
                    context.ActivityLogs.Add(new ActivityLog { DeviceId = light.Id, UserId = user.Id, ActionType = "Toggle", Details = "Turned ON the main light", CreatedAt = DateTime.Now.AddMinutes(-30) });
                }
                await context.SaveChangesAsync();
            }

            // 8. Seed Waste Requests
            if (!context.WasteCollectionRequests.Any())
            {
                var bin = context.Devices.FirstOrDefault(d => d.Name == "Main Trash Bin");
                if (bin != null)
                {
                    context.WasteCollectionRequests.Add(new WasteCollectionRequest { DeviceId = bin.Id, Status = "Pending", ScheduledTime = DateTime.Now.AddDays(1) });
                }
                await context.SaveChangesAsync();
            }

            // 9. Seed Parking Reservations
            if (!context.ParkingReservations.Any())
            {
                var spot = context.Devices.FirstOrDefault(d => d.Name == "Spot 1");
                if (spot != null)
                {
                    context.ParkingReservations.Add(new ParkingReservation { DeviceId = spot.Id, UserId = user.Id, PlateNumber = "ABC-123", StartTime = DateTime.Now, EndTime = DateTime.Now.AddHours(2), Status = "Active" });
                }
                await context.SaveChangesAsync();
            }

            // 10. Seed Chat Messages
            if (!context.ChatMessages.Any())
            {
                var admin = await userManager.FindByEmailAsync("test@example.com");
                // Let's create another user for chat
                var otherUser = await userManager.FindByEmailAsync("user@example.com");
                if (otherUser == null)
                {
                    otherUser = new ApplicationUser
                    {
                        UserName = "user@example.com",
                        Email = "user@example.com",
                        FullName = "Another User",
                        WalletBalance = 100.0,
                        PhoneNumber = "01122334455"
                    };
                    await userManager.CreateAsync(otherUser, "Password123!");
                }

                if (admin != null && otherUser != null)
                {
                    context.ChatMessages.Add(new ChatMessage { SenderId = admin.Id, ReceiverId = otherUser.Id, Message = "Hello there!", SentAt = DateTime.Now.AddMinutes(-10), IsRead = true });
                    context.ChatMessages.Add(new ChatMessage { SenderId = otherUser.Id, ReceiverId = admin.Id, Message = "Hi! How can I help you?", SentAt = DateTime.Now.AddMinutes(-5), IsRead = false });
                }
                await context.SaveChangesAsync();
            }
        }
    }
}
