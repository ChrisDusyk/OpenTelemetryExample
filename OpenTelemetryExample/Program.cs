using OpenTelemetry.Trace;
using OpenTelemetry.Resources;
using System.Diagnostics;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Add OpenTelemetry
var otelResourceBuilder = ResourceBuilder
	.CreateDefault()
	.AddService("open-telemetry-example")
	.AddAttributes(new Dictionary<string, object>()
	{
		{ "host.environment", "development" },
		{ "host.machine_name", Environment.MachineName },
		{ "host.process_id", Environment.ProcessId },
		{ "app.version", FileVersionInfo.GetVersionInfo(typeof(Program).Assembly.Location).FileVersion ?? "0.0.0.0" }
	})
	.AddTelemetrySdk();

builder.Services.AddOpenTelemetryTracing(tracerProviderBuilder =>
{
	tracerProviderBuilder
		.SetResourceBuilder(otelResourceBuilder)
		.SetSampler(new AlwaysOnSampler())
		.AddAspNetCoreInstrumentation(options =>
		{
			options.EnrichWithHttpRequest = (activity, httpRequest) =>
			{
				activity.SetTag("http.referer_url", httpRequest.GetTypedHeaders().Referer?.OriginalString);
				activity.SetTag("http.content_length", httpRequest.GetTypedHeaders().ContentLength);
				activity.SetTag("http.client_ip", httpRequest.HttpContext.Connection.RemoteIpAddress?.ToString());
			};

			options.EnrichWithHttpResponse = (activity, httpResponse) =>
			{
				activity.SetTag("http.response_type", httpResponse.ContentType);
				activity.SetTag("http.response_length", httpResponse.ContentLength);
			};

			options.Filter = httpContext =>
			{
				var containsPing = httpContext.Request.Path.Value?.Contains("ping", StringComparison.InvariantCultureIgnoreCase) ?? false;
				var containsSwagger = httpContext.Request.Path.Value?.Contains("swagger", StringComparison.InvariantCultureIgnoreCase) ?? false;
				var containsAspNetCoreFramework = httpContext.Request.Path.Value?.Contains("_framework", StringComparison.InvariantCultureIgnoreCase) ?? false;
				var containsVisualStudio = httpContext.Request.Path.Value?.Contains("_vs", StringComparison.InvariantCultureIgnoreCase) ?? false;
				return !containsPing && !containsSwagger && !containsAspNetCoreFramework && !containsVisualStudio;
			};
		})
		.AddHttpClientInstrumentation()
		.AddSource("open-telemetry-example.instrumentation.custom")
		.AddOtlpExporter(options =>
		{
			var honeycombConfig = builder.Configuration.GetSection("Honeycomb");
			var apiUri = honeycombConfig["ApiUri"];
			var apiKey = honeycombConfig["ApiKey"];
			var dataset = honeycombConfig["Dataset"];
			options.Endpoint = new Uri(apiUri);
			options.Headers = $"x-honeycomb-team={apiKey},x-honeycomb-dataset={dataset}";
		});
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
	app.UseSwagger();
	app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
