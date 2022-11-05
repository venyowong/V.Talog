using Microsoft.AspNetCore.HttpOverrides;
using Serilog;
using Serilog.Events;
using V.Common.Extensions;
using V.Talog;
using V.Talog.Server;
using V.User.Extensions;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((context, config) =>
{
    config.MinimumLevel.Information()
        .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
        .MinimumLevel.Override("System", LogEventLevel.Warning)
        .ReadFrom.Configuration(context.Configuration)
        .Enrich.FromLogContext();
});

builder.Services.AddJwt(builder.Configuration["Jwt:Secret"]);

// Add services to the container.

builder.Services.AddControllers();
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders =
        ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
});
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddTaloger(getMapping: taloger =>
{
    taloger.CreateJsonIndexer("stored_index")
        .Tag("tag_mapping", "pg")
        .Data(new Dictionary<string, string> { { "date", typeof(int).FullName } }.ToJson())
        .Save();
    taloger.CreateJsonIndexer("stored_index")
        .Tag("tag_mapping", "metric")
        .Data(new Dictionary<string, string> { { "date", typeof(int).FullName } }.ToJson())
        .Save();

    return new IndexMapping(taloger);
});

var app = builder.Build();

app.Lifetime.ApplicationStopping.Register(() =>
{
    var taloger = app.Services.GetService<Taloger>();
    taloger?.Dispose();
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

app.UseStaticFiles();

app.UseForwardedHeaders();

app.MapControllers();

app.Run();
