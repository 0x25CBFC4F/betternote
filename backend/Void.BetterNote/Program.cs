using FluentValidation;
using Serilog;
using Serilog.Events;
using StackExchange.Redis;
using Void.BetterNote.Middlewares;
using Void.BetterNote.Validation;
using Void.BetterNote.Validation.Validators;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

// Serilog setup
var logger = new LoggerConfiguration()
    .WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss}] [{Level:u3}] [{SourceContext}] {Message:lj}{NewLine}{Exception}")
    .MinimumLevel.Override("Microsoft.AspNetCore.Hosting.Diagnostics", LogEventLevel.Warning)
    .MinimumLevel.Override("Microsoft.AspNetCore.Routing.EndpointMiddleware", LogEventLevel.Warning)
    .MinimumLevel.Override("Microsoft.AspNetCore.Mvc.Infrastructure.ControllerActionInvoker", LogEventLevel.Warning)
    .MinimumLevel.Override("Microsoft.AspNetCore.Mvc.Infrastructure.ObjectResultExecutor", LogEventLevel.Warning)
    .MinimumLevel.Override("Microsoft.AspNetCore.StaticFiles.StaticFileMiddleware", LogEventLevel.Warning)
    .CreateLogger();

var appLogger = logger.ForContext("SourceContext", "App");

builder.Logging.ClearProviders();
builder.Logging.AddSerilog(logger, true);

// Redis setup
var redisHost = builder.Configuration["RedisHost"];

if (string.IsNullOrWhiteSpace(redisHost))
{
    appLogger.Fatal("RedisHost configuration entry is missing or empty!");
    return;
}

ConnectionMultiplexer redisConnection;

appLogger.Information("Attempting to connect to Redis at {Host}..", redisHost);

try
{
    redisConnection = await ConnectionMultiplexer.ConnectAsync(redisHost);
}
catch (RedisConnectionException ex)
{
    appLogger.Fatal(ex, "Failed to connect to Redis ({Host})", redisHost);
    return;
}

if (!int.TryParse(builder.Configuration["RedisDatabaseId"] ?? "-1", out var databaseId))
{
    appLogger.Fatal("Failed to parse RedisDatabaseId: expected an integer");
}

builder.Services.AddSingleton(redisConnection.GetDatabase(databaseId));

// FluentValidation
builder.Services.AddSingleton(typeof(ValidationActionFilter<,>));
builder.Services.AddValidatorsFromAssemblyContaining(typeof(CreateRequestValidator));

// Exception handler
builder.Services.AddSingleton<ExceptionMiddleware>();

appLogger.Information("RedisDatabaseId = {DatabaseId}", databaseId);
appLogger.Information("SecretExpiryInMinutes = {SecretExpiryInMinutes}", builder.Configuration.GetValue<int>("SecretExpiryInMinutes"));
appLogger.Information("DemoMode = {DemoMode}", builder.Configuration.GetValue<bool>("DemoMode"));

var app = builder.Build();

app.UseMiddleware<ExceptionMiddleware>();
app.MapControllers();
app.UseDefaultFiles();
app.UseStaticFiles();

await app.RunAsync();