using GenerativeAI.Microsoft.Extensions;

var builder = WebApplication.CreateBuilder(args);

// Thêm services
builder.Services.AddControllers();

// Đăng ký Gemini AI service với DI
builder.Services.AddGenerativeAI(options =>
{
    options.ApiKey = builder.Configuration["Gemini:ApiKey"];
    options.DefaultModel = builder.Configuration["Gemini:Model"] ?? "gemini-1.5-pro";
});

// Hoặc đăng ký đầy đủ hơn:
/*
builder.Services.AddGenerativeAI(options =>
{
    options.ApiKey = builder.Configuration["Gemini:ApiKey"];
    options.DefaultModel = builder.Configuration["Gemini:Model"];
    options.DefaultTemperature = double.Parse(builder.Configuration["Gemini:Temperature"] ?? "0.7");
    options.DefaultMaxTokens = int.Parse(builder.Configuration["Gemini:MaxTokens"] ?? "2048");
});
*/

var app = builder.Build();

app.MapControllers();

app.Run();