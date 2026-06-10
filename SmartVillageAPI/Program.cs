using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;
using Microsoft.IdentityModel.Tokens;
using SmartVillageAPI.Hubs;
using SmartVillageAPI.Model;
using SmartVillageAPI.Services;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// ─────────────────────────────────────────────
// 1. Resolve recordings path (relative → absolute)
//    so every service gets the same absolute path
// ─────────────────────────────────────────────
var rawPath = builder.Configuration["CameraSettings:RecordingsPath"] ?? "wwwroot/MediaRecords";
var absoluteRecordingsPath = Path.IsPathRooted(rawPath)
    ? rawPath
    : Path.Combine(builder.Environment.ContentRootPath, rawPath);

// write it back so IOptions<CameraSettings> picks up the resolved path
builder.Configuration["CameraSettings:RecordingsPath"] = absoluteRecordingsPath;

// create folder if it doesn't exist (safe on any machine / Monsta)
Directory.CreateDirectory(absoluteRecordingsPath);

// ─────────────────────────────────────────────
// 2. Services
// ─────────────────────────────────────────────
builder.Services.Configure<CameraSettings>(builder.Configuration.GetSection("CameraSettings"));

builder.Services.AddSingleton<IMqttService, MqttService>();
builder.Services.AddHostedService<MqttBackgroundService>();
builder.Services.AddHostedService<CameraSyncService>();
builder.Services.AddSignalR();

// ─────────────────────────────────────────────
// 3. Database
// ─────────────────────────────────────────────
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(connectionString));

// ─────────────────────────────────────────────
// 4. Identity
// ─────────────────────────────────────────────
builder.Services.AddIdentity<ApplicationUser, IdentityRole>()
    .AddEntityFrameworkStores<AppDbContext>()
    .AddDefaultTokenProviders();

// ─────────────────────────────────────────────
// 5. CORS
// ─────────────────────────────────────────────
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

// ─────────────────────────────────────────────
// 6. JWT Authentication
// ─────────────────────────────────────────────
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
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidAudience = builder.Configuration["JWT:ValidAudience"],
        ValidIssuer = builder.Configuration["JWT:ValidIssuer"],
        IssuerSigningKey = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(builder.Configuration["JWT:Secret"]!))
    };
});

builder.Services.AddControllers();

// ─────────────────────────────────────────────
// 7. Swagger
// ─────────────────────────────────────────────
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "Smart Village API",
        Version = "v1",
        Description = "API for Smart Village Management System"
    });

    options.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.Http,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Description = "Enter your JWT token. Example: \"12345abcdef\""
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
            Array.Empty<string>()
        }
    });
});

// ─────────────────────────────────────────────
// Build
// ─────────────────────────────────────────────
var app = builder.Build();

// ─────────────────────────────────────────────
// Swagger (all environments)
// ─────────────────────────────────────────────
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Smart Village API v1");
    c.RoutePrefix = "swagger";
});

// ─────────────────────────────────────────────
// Static Files
// 1. Default wwwroot (css, js, etc.)
// 2. MediaRecords folder served at /MediaRecords
//    Flutter accesses: http://server/MediaRecords/cam1/video.mp4
// ─────────────────────────────────────────────
app.UseStaticFiles(); // serves wwwroot by default

app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(absoluteRecordingsPath),
    RequestPath = "/MediaRecords"
});

app.UseHttpsRedirection();
app.UseCors("AllowAll");

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapHub<SmartVillageHub>("/villageHub");

app.Run();