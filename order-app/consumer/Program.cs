using System.Net;
using System.Text.Json.Serialization;
using Dapr.Client;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

builder.Logging.ClearProviders();

var logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateLogger();

builder.Logging.AddSerilog(logger);

var app = builder.Build();

var dapr = new DaprClientBuilder().Build();

// Dapr will send serialized event object vs. being raw CloudEvent
app.UseCloudEvents();

if (app.Environment.IsDevelopment()) { app.UseDeveloperExceptionPage(); }

var StateStoreName = Environment.GetEnvironmentVariable("STATESTORE_NAME") ?? "statestore";

app.MapPost("/neworders", async (ILoggerFactory loggerFactory, Order order) =>
{
    var logger = loggerFactory.CreateLogger("state");

    try
    {
        await dapr.SaveStateAsync(StateStoreName, order.OrderId.ToString(), order);
        logger.LogInformation("Order {orderId} successfully persisted", order.OrderId);
    }
    catch (Exception e)
    {
        logger.LogError("Error persisting order {orderId}, Exception: {message} ", order.OrderId, e.InnerException?.Message ?? e.Message);
        return Results.Problem(e.Message, null, (int)HttpStatusCode.InternalServerError);
    }
    return Results.Ok(order);
});

app.Run();

public record Order([property: JsonPropertyName("orderId")] int OrderId);