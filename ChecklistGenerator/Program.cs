using ChecklistGenerator.Services;
using Microsoft.AspNetCore.Server.IIS;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddControllers();
builder.Services.AddHttpClient<GeminiService>(client =>
{
    client.Timeout = TimeSpan.FromMinutes(5); // 5 minute timeout for AI requests
});
builder.Services.AddScoped<GeminiService>();
builder.Services.AddScoped<DocxToExcelConverter>();
builder.Services.AddScoped<ExcelProcessor>();
builder.Services.AddScoped<SurveyJSConverter>();

// Configure file upload limits
builder.Services.Configure<IISServerOptions>(options =>
{
    options.MaxRequestBodySize = 50 * 1024 * 1024; // 50MB
});

// Add CORS for development and Codespaces
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}

app.UseStaticFiles();
app.UseCors();
app.UseRouting();

app.MapControllers();

// Serve the default page
app.MapGet("/", () => Results.Redirect("/index.html"));

app.Run();

// Make the Program class accessible for testing
public partial class Program { }
