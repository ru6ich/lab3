using Backend.Data;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Добавляем контроллеры
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Добавляем логирование
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();

// Настройка базы данных
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<ApplicationDbContext>(options =>
{
    options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString));
    options.LogTo(Console.WriteLine, LogLevel.Information);
});

// Разрешаем все источники для CORS (чтобы работал фронт)
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll",
        policy =>
        {
            policy.AllowAnyOrigin()
                  .AllowAnyMethod()
                  .AllowAnyHeader();
        });
});

var app = builder.Build();

// Настройка порта для Railway
var port = Environment.GetEnvironmentVariable("PORT") ?? "8080";
app.Urls.Add($"http://*:{port}");

// Включаем CORS (ДО UseRouting())
app.UseCors("AllowAll");

// Включаем маршрутизацию
app.UseRouting();
app.UseAuthorization();

// Добавляем обработку маршрутов контроллеров
app.UseEndpoints(endpoints =>
{
    _ = endpoints.MapControllers();
});

// Проверочный маршрут, чтобы убедиться, что сервер запустился
app.MapGet("/", () => "API работает!");

// Swagger включаем только в режиме разработки
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Логируем запросы
app.Use(async (context, next) =>
{
    var logger = context.RequestServices.GetRequiredService<ILogger<Program>>();
    logger.LogInformation($"Request: {context.Request.Method} {context.Request.Path}");
    logger.LogInformation($"Origin: {context.Request.Headers["Origin"]}");
    
    await next();
    
    logger.LogInformation($"Response: {context.Response.StatusCode}");
});

// Логируем запуск приложения
var logger = app.Services.GetRequiredService<ILogger<Program>>();
logger.LogInformation("Starting application...");
logger.LogInformation($"Database connection string: {connectionString}");

Console.WriteLine($"Application is listening on port: {port}");

// Запускаем приложение
app.Run();
