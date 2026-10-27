using Microsoft.EntityFrameworkCore;
using Serilog;
using UserManagement.Day19.Data;
using UserManagement.Day19.Services;

// Bootstrap logger: catches errors that happen during startup
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
    Log.Information("Starting UserManagement.Day19");

    var builder = WebApplication.CreateBuilder(args);

    // Full Serilog configuration
    builder.Host.UseSerilog((context, services, configuration) => configuration
        .MinimumLevel.Information()
        .MinimumLevel.Override("Microsoft", Serilog.Events.LogEventLevel.Warning)
        .Enrich.FromLogContext()
        .WriteTo.Console(outputTemplate:
            "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}")
        .WriteTo.File(
            path: "logs/log-.txt",
            rollingInterval: RollingInterval.Day,
            outputTemplate:
            "{Timestamp:yyyy-MM-dd HH:mm:ss} [{Level:u3}] {Message:lj}{NewLine}{Exception}"));

    builder.Services.AddControllers();
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen();

    builder.Services.AddDbContext<AppDbContext>(options =>
        options.UseInMemoryDatabase("UsersDb"));

    builder.Services.AddAutoMapper(cfg => cfg.AddMaps(typeof(Program).Assembly));
    builder.Services.AddScoped<IUserService, UserService>();

    var app = builder.Build();

    // Logs unhandled exceptions and returns a clean 500 response
    app.UseExceptionHandler(errorApp =>
    {
        errorApp.Run(async context =>
        {
            var exception = context.Features
                .Get<Microsoft.AspNetCore.Diagnostics.IExceptionHandlerFeature>()?.Error;

            Log.Error(exception, "Unhandled exception on {Method} {Path}",
                context.Request.Method, context.Request.Path);

            context.Response.StatusCode = StatusCodes.Status500InternalServerError;
            await context.Response.WriteAsJsonAsync(new { title = "An unexpected error occurred." });
        });
    });

    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI();
    }

    // Logs one line per HTTP request (method, path, status code, time taken)
    app.UseSerilogRequestLogging();

    app.UseHttpsRedirection();
    app.UseAuthorization();
    app.MapControllers();

    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}