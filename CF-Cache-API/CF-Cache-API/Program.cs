using CF_Cache_API.Services;

var builder = WebApplication.CreateBuilder(args);

// Configure URLs based on environment
var port = Environment.GetEnvironmentVariable("PORT") ?? "5100";
var urls = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") == "Production" 
    ? $"http://0.0.0.0:{port}" 
    : $"http://localhost:{port}";
builder.WebHost.UseUrls(urls);

// Configure CORS based on environment
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        if (builder.Environment.IsDevelopment())
        {
            policy.WithOrigins("http://localhost:4101", "http://localhost:3000")
                  .AllowAnyMethod()
                  .AllowAnyHeader();
        }
        else
        {
            // For production, configure specific origins or use AllowAnyOrigin() for testing
            policy.AllowAnyOrigin()
                  .AllowAnyMethod()
                  .AllowAnyHeader();
        }
    });
});

builder.Services.AddSingleton<EmployeeService>();
builder.Services.AddSingleton<UserService>();
builder.Services.AddControllers();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    // Swagger disabled for production
}

app.UseCors();

app.UseAuthorization();

app.MapControllers();

app.Run();
