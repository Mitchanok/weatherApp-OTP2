using OpenTelemetry.Resources;
using System.Reflection.PortableExecutable;
using OpenTelemetry.Instrumentation.AspNetCore;
using OpenTelemetry.Trace;
using OpenTelemetry.Metrics;
using System.Diagnostics.Metrics;
using OpenTelemetry.Exporter;
using Weather_opentelemetry2;

var builder = WebApplication.CreateBuilder(args);
const string serviceName = "weather-app";

builder.Services.AddOpenTelemetry()

    .WithMetrics(x =>
    {
      
        x.AddPrometheusExporter()
           .AddAspNetCoreInstrumentation()
           .AddRuntimeInstrumentation()
           .AddProcessInstrumentation();
        x.AddMeter("Microsoft.AspNetCore.Hosting",
             "Microsoft.AspNetCore.Server.Kestrel",
             "Microsoft.AspNetCore.Routing");
        x.AddView(
            instrumentName:"http.server.request.duration",
            new ExplicitBucketHistogramConfiguration { 
                Boundaries = [0.005, 0.01, 0.025, 0.05, 0.075, 0.1, 0.25, 0.5, 0.75, 1, 2.5, 5, 7.5, 10 ] }
            )
        .AddOtlpExporter(s =>
        {
            s.Endpoint = new Uri(builder.Configuration["Otlp:Endpoint"]
                                                       ?? throw new InvalidOperationException());
        });

    })
     .WithTracing(b =>
      {
          if (builder.Environment.IsDevelopment())
          {
              b.SetSampler<AlwaysOnSampler>();
          }
          b.AddConsoleExporter()
            .AddSource(serviceName)
            .SetResourceBuilder(
              ResourceBuilder.CreateDefault().AddService(serviceName))
          .AddAspNetCoreInstrumentation()
          .AddAspNetCoreInstrumentation()
          .AddHttpClientInstrumentation();

      });






builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();


var app = builder.Build();

// Configure the HTTP request pipeline.

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapPrometheusScrapingEndpoint();

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();
app.Run();
