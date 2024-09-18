using AspNetCoreRateLimit;
using VideoDownloaderAPI.Extractor;
using VideoDownloaderAPI.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();

// Register ProcessRunner
builder.Services.AddSingleton<ProcessRunner>();

// Register platform-specific extractors
builder.Services.AddTransient<YouTubeExtractor>();
builder.Services.AddTransient<InstagramExtractor>();
builder.Services.AddTransient<TikTokExtractor>();

// Register IVideoExtractor implementations
builder.Services.AddTransient<IVideoExtractor, YouTubeExtractor>();
builder.Services.AddTransient<IVideoExtractor, InstagramExtractor>();
builder.Services.AddTransient<IVideoExtractor, TikTokExtractor>();

// Register IVideoService
builder.Services.AddScoped<IVideoService, VideoService>();

// Add logging
builder.Services.AddLogging();

// Add Rate Limiting services
builder.Services.AddMemoryCache();
builder.Services.Configure<IpRateLimitOptions>(builder.Configuration.GetSection("IpRateLimiting"));
builder.Services.AddInMemoryRateLimiting();
builder.Services.AddSingleton<IRateLimitConfiguration, RateLimitConfiguration>();

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Add the rate limiting middleware
app.UseIpRateLimiting();

app.UseAuthorization();

app.MapControllers();

app.Run();
