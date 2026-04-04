using System;
using SmartVillageAPI.Model;
public class Camera
{
    public int Id { get; set; }
    public string Name { get; set; }
    public string Location { get; set; }
    public string StreamUrl { get; set; }

    // الربط مع Zone
    public int ZoneId { get; set; }
    public Zone Zone { get; set; }

    public DateTime CreatedAt { get; set; }

    public List<Recording> Recordings { get; set; }
}

public class Recording
{
    public int Id { get; set; }
    public int CameraId { get; set; }
    public string FileUrl { get; set; } 
    public DateTime RecordedAt { get; set; }

    public Camera Camera { get; set; }
}
