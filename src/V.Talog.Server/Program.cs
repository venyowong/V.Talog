using Microsoft.AspNetCore.HttpOverrides;
using Serilog;
using Serilog.Events;
using V.Common.Extensions;
using V.Quartz;
using V.Talog;
using V.Talog.Server;
using V.Talog.Server.Jobs;
using V.Talog.Server.Middlewares;
using V.User.Extensions;

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
    .MinimumLevel.Override("System", LogEventLevel.Warning)
    .WriteTo.Console()
    .WriteTo.File("log/log.txt", rollingInterval: RollingInterval.Day)
    .Enrich.FromLogContext()
    .CreateLogger();

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddJwt(builder.Configuration["Jwt:Secret"]);

builder.Services.AddHostedService<LogShipper>()
    .AddHostedService<QuartzHostedService>()
    .AddTransientBothTypes<IScheduledJob, AutoCleanJob>();

// Add services to the container.

builder.Services.AddControllers().AddNewtonsoftJson();
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders =
        ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
});
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddTalogger(getMapping: talogger =>
{
    talogger.CreateJsonIndexer("stored_index")
        .Tag("tag_mapping", "pg")
        .Data(new Dictionary<string, string> { { "date", typeof(int).FullName } }.ToJson())
        .Save();
    talogger.CreateJsonIndexer("stored_index")
        .Tag("tag_mapping", "metric")
        .Data(new Dictionary<string, string> { { "date", typeof(int).FullName } }.ToJson())
        .Save();

    return new IndexMapping(talogger);
});

var origins = builder.Configuration["Cors:Origins"];
if (!string.IsNullOrWhiteSpace(origins))
{
    builder.Services.AddCors(o => o.AddPolicy("Default", b =>
    {
        b.WithOrigins(origins.Split(';'))
            .AllowAnyMethod()
            .AllowAnyHeader()
            .AllowCredentials();
    }));
}

var app = builder.Build();

app.Lifetime.ApplicationStopping.Register(() =>
{
    var talogger = app.Services.GetService<Talogger>();
    talogger?.Dispose();
});

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.Use(async (context, next) =>
{
    context.Request.EnableBuffering();
    await next(context);
});

app.UseMiddleware<LogMiddleware>();

if (!string.IsNullOrWhiteSpace(origins))
{ 
    app.UseCors("Default");
}

app.UseDefaultFiles();
app.UseStaticFiles();

app.UseForwardedHeaders();

app.MapControllers();

app.Run();
