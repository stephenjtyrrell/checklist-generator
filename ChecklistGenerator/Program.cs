using ChecklistGenerator.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddControllers();

// Register application services
builder.Services.AddScoped<IAzureDocumentIntelligenceService, AzureDocumentIntelligenceService>();
builder.Services.AddScoped<SurveyJSConverter>();

// Configure CORS for cross-origin requests
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

// Health check endpoint
app.MapGet("/health", () => Results.Text("healthy"));

// Default route
app.MapGet("/", () => Results.Redirect("/index.html"));

app.Run();

// Make the Program class accessible for testing
public partial class Program { }
