using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using SmartVillageAPI.Hubs;
using SmartVillageAPI.Model;
using SmartVillageAPI.Services;
using System.Text;
using Microsoft.Extensions.FileProviders; // تم إضافة هذا السطر للتعامل مع ملفات الفيديو

var builder = WebApplication.CreateBuilder(args);

// --- 1. Services Injection ---
builder.Services.AddSingleton<IMqttService, MqttService>();
builder.Services.AddHostedService<MqttBackgroundService>();
builder.Services.AddHostedService<CameraSyncService>();
builder.Services.AddSignalR();

// --- إضافة إعدادات الكاميرا من appsettings.json ---
builder.Services.Configure<CameraSettings>(builder.Configuration.GetSection("CameraSettings"));

// --- 2. Database (EF Core) ---
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(connectionString));

// --- 3. Identity ---
builder.Services.AddIdentity<ApplicationUser, IdentityRole>()
    .AddEntityFrameworkStores<AppDbContext>()
    .AddDefaultTokenProviders();

// --- 4. CORS Policy (عشان الفلاتر يقدر يكلمك) ---
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.SetIsOriginAllowed(_ => true)
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials();
    });
});

// --- 5. JWT Authentication ---
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.SaveToken = true;
    options.RequireHttpsMetadata = false;
    options.TokenValidationParameters = new TokenValidationParameters()
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidAudience = builder.Configuration["JWT:ValidAudience"],
        ValidIssuer = builder.Configuration["JWT:ValidIssuer"],
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["JWT:Secret"]))
    };
});

builder.Services.AddControllers();

// --- 6. Swagger ---
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "Smart Village API",
        Version = "v1",
        Description = "API for Smart Village Management System"
    });

    // إضافة دعم JWT في Swagger
    options.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.Http,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Description = "Enter your JWT token in the text input below.\r\n\r\nExample: \"12345abcdef\""
    });

    options.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
    {
        {
            new Microsoft.OpenApi.Models.OpenApiSecurityScheme
            {
                Reference = new Microsoft.OpenApi.Models.OpenApiReference
                {
                    Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            new string[] {}
        }
    });
});

var app = builder.Build();

// تفعيل Swagger في جميع البيئات (Development & Production)
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Smart Village API v1");
    c.RoutePrefix = "swagger"; // الوصول عبر /swagger
});

// --- إعدادات الملفات الثابتة (Static Files) ---
app.UseStaticFiles(); // لملفات wwwroot العادية

// السماح بالوصول لفيديوهات الكاميرا المسجلة في فولدر MediaMTX
// ملاحظة: تأكدي أن المسار "D:\\MediaMTX\\recordings" موجود فعلياً على جهازك
if (Directory.Exists("D:\\MediaMTX\\recordings"))
{
    app.UseStaticFiles(new StaticFileOptions
    {
        FileProvider = new PhysicalFileProvider("D:\\MediaMTX\\recordings"),
        RequestPath = "/recordings"
    });
}

app.UseHttpsRedirection();

// تفعيل الـ CORS
app.UseCors("AllowAll");

// الترتيب مهم: Authentication ثم Authorization
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapHub<SmartVillageHub>("/villageHub");

app.Run();