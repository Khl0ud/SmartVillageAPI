using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace SmartVillageAPI.Model
{
    public class Camera
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Location { get; set; } = string.Empty;

        // RTSP stream from ESP32-CAM e.g. rtsp://192.168.1.x:554/stream
        // Store HLS version here if using MediaMTX:
        // e.g. http://yourserver:8888/cam1/index.m3u8
        public string StreamUrl { get; set; } = string.Empty;

        public int ZoneId { get; set; }
        public Zone? Zone { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public List<Recording> Recordings { get; set; } = new();
    }

    public class Recording
    {
        public int Id { get; set; }
        public int CameraId { get; set; }

        // just the filename e.g. "2024-01-15_10-30.mp4"
        public string FileUrl { get; set; } = string.Empty;

        // full URL built by API e.g. http://server/MediaRecords/cam1/2024-01-15.mp4
        // not stored in DB — built at runtime
        [NotMapped]
        public string FullVideoUrl { get; set; } = string.Empty;

        public DateTime RecordedAt { get; set; }

        public Camera? Camera { get; set; }
    }

    public class CameraSettings
    {
        // default resolves at runtime in Program.cs
        public string RecordingsPath { get; set; } =
            Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "MediaRecords");
    }
}