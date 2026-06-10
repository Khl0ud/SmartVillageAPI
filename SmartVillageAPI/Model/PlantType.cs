namespace SmartVillageAPI.Model
{
    public enum PlantType
    {
        None,
        // خضروات
        Tomato,
        Lettuce,
        Cucumber,
        Pepper,
        // أعشاب
        Herbs,
        // ورود وزهور
        Roses,
        Sunflower,
        Tulips,
        Hibiscus,
        // نجيلة
        GrassLawn,
        // أشجار فاكهة
        OrangeTree,
        LemonTree,
        AppleTree,
        OliveTree,
        // أشجار زينة
        PalmTree,
        PineTree,
        DeciduousTree,
        // صبار
        Cactus
    }

    /// <summary>
    /// بيانات كل نبتة - بتستخدمها الـ AI عشان تحسب التوصية الصح
    /// </summary>
    public class PlantProfile
    {
        public PlantType Type { get; set; }
        public string Name { get; set; } = string.Empty;
        public double OptimalMoisture { get; set; }
        public double OptimalTemp { get; set; }
        public string BestTimeToIrrigate { get; set; } = string.Empty;
        public int WeeklyFrequency { get; set; }
        public string Season { get; set; } = string.Empty;
    }

    public static class PlantDatabase
    {
        public static readonly List<PlantProfile> All = new()
        {
            // خضروات
            new() { Type = PlantType.Tomato,       Name = "Tomato",        OptimalMoisture = 65, OptimalTemp = 24, BestTimeToIrrigate = "Early Morning", WeeklyFrequency = 4, Season = "Spring/Summer" },
            new() { Type = PlantType.Lettuce,      Name = "Lettuce",       OptimalMoisture = 70, OptimalTemp = 18, BestTimeToIrrigate = "Morning",        WeeklyFrequency = 5, Season = "Spring/Fall"   },
            new() { Type = PlantType.Cucumber,     Name = "Cucumber",      OptimalMoisture = 68, OptimalTemp = 22, BestTimeToIrrigate = "Early Morning", WeeklyFrequency = 4, Season = "Summer"         },
            new() { Type = PlantType.Pepper,       Name = "Pepper",        OptimalMoisture = 60, OptimalTemp = 26, BestTimeToIrrigate = "Morning",        WeeklyFrequency = 3, Season = "Summer"         },
            // أعشاب
            new() { Type = PlantType.Herbs,        Name = "Herbs",         OptimalMoisture = 55, OptimalTemp = 20, BestTimeToIrrigate = "Morning",        WeeklyFrequency = 3, Season = "All Year"       },
            // ورود وزهور
            new() { Type = PlantType.Roses,        Name = "Roses",         OptimalMoisture = 60, OptimalTemp = 22, BestTimeToIrrigate = "Early Morning", WeeklyFrequency = 4, Season = "Spring/Summer"  },
            new() { Type = PlantType.Sunflower,    Name = "Sunflower",     OptimalMoisture = 58, OptimalTemp = 25, BestTimeToIrrigate = "Morning",        WeeklyFrequency = 3, Season = "Summer"         },
            new() { Type = PlantType.Tulips,       Name = "Tulips",        OptimalMoisture = 62, OptimalTemp = 18, BestTimeToIrrigate = "Morning",        WeeklyFrequency = 3, Season = "Spring"         },
            new() { Type = PlantType.Hibiscus,     Name = "Hibiscus",      OptimalMoisture = 65, OptimalTemp = 26, BestTimeToIrrigate = "Morning",        WeeklyFrequency = 4, Season = "Summer"         },
            // نجيلة
            new() { Type = PlantType.GrassLawn,    Name = "Grass Lawn",    OptimalMoisture = 55, OptimalTemp = 20, BestTimeToIrrigate = "Early Morning", WeeklyFrequency = 5, Season = "All Year"       },
            // أشجار فاكهة
            new() { Type = PlantType.OrangeTree,   Name = "Orange Tree",   OptimalMoisture = 50, OptimalTemp = 24, BestTimeToIrrigate = "Morning",        WeeklyFrequency = 2, Season = "All Year"       },
            new() { Type = PlantType.LemonTree,    Name = "Lemon Tree",    OptimalMoisture = 50, OptimalTemp = 23, BestTimeToIrrigate = "Morning",        WeeklyFrequency = 2, Season = "All Year"       },
            new() { Type = PlantType.AppleTree,    Name = "Apple Tree",    OptimalMoisture = 48, OptimalTemp = 20, BestTimeToIrrigate = "Morning",        WeeklyFrequency = 2, Season = "Spring/Fall"    },
            new() { Type = PlantType.OliveTree,    Name = "Olive Tree",    OptimalMoisture = 40, OptimalTemp = 25, BestTimeToIrrigate = "Morning",        WeeklyFrequency = 1, Season = "All Year"       },
            // أشجار زينة
            new() { Type = PlantType.PalmTree,     Name = "Palm Tree",     OptimalMoisture = 45, OptimalTemp = 28, BestTimeToIrrigate = "Morning",        WeeklyFrequency = 2, Season = "All Year"       },
            new() { Type = PlantType.PineTree,     Name = "Pine Tree",     OptimalMoisture = 42, OptimalTemp = 18, BestTimeToIrrigate = "Morning",        WeeklyFrequency = 2, Season = "All Year"       },
            new() { Type = PlantType.DeciduousTree,Name = "Deciduous Tree",OptimalMoisture = 45, OptimalTemp = 20, BestTimeToIrrigate = "Morning",        WeeklyFrequency = 2, Season = "Spring/Fall"    },
            // صبار
            new() { Type = PlantType.Cactus,       Name = "Cactus",        OptimalMoisture = 30, OptimalTemp = 28, BestTimeToIrrigate = "Morning",        WeeklyFrequency = 1, Season = "All Year"       },
        };

        public static PlantProfile? Get(PlantType type) =>
            All.FirstOrDefault(p => p.Type == type);
    }
}
