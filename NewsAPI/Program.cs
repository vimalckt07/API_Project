using Microsoft.OpenApi.Models;
using NewsAPI.ExceptionHandling;
using NewsAPI.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();

// Adding in-memory caching service
builder.Services.AddMemoryCache();
builder.Services.AddHttpClient();

// Registering HttpClient for IHackerNewsService with a handler lifetime
builder.Services.AddHttpClient<IHackerNewsService, HackerNewsService>();
// Registering the HackerNewsService as a singleton service
builder.Services.AddSingleton<IHackerNewsService, HackerNewsService>();

builder.Services.AddEndpointsApiExplorer();
// Configuring Swagger for API documentation
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Version = "v1",
        Title = "Hacker News API",
        Description = "An ASP.NET Core Web API for fetching the newest Hacker News stories",
    });
    
});
// Adding logging services
builder.Logging.ClearProviders();
builder.Logging.AddConsole();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger(); //// Enable Swagger in development mode
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Hacker News API V1");
    });
}


app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

// Using custom middleware for exception handling
app.UseMiddleware<ExceptionMiddleware>();

var summaries = new[]
{
    "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
};

app.MapGet("/weatherforecast", () =>
{
    var forecast = Enumerable.Range(1, 5).Select(index =>
        new WeatherForecast
        (
            DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
            Random.Shared.Next(-20, 55),
            summaries[Random.Shared.Next(summaries.Length)]
        ))
        .ToArray();
    return forecast;
});

// Running the application
app.Run();
internal record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}
