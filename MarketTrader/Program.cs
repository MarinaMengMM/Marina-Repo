using MarketTrader;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SharedModels;


var builder = Host.CreateApplicationBuilder(args);
// Add logging
builder.Services.AddLogging();

// Register TradeConsumer as singleton
builder.Services.AddSingleton<TradeConsumer>(sp =>
{
    var logger = sp.GetRequiredService<ILogger<TradeConsumer>>();
    var instructionProvider = sp.GetRequiredService<IInstructionProvider>();
    return new TradeConsumer(instructionProvider, "secore_inbound_queue", logger);
});

// Register  IInstructionProvider
builder.Services.AddScoped<IInstructionProvider, InstructionProvider>();

// Register the worker
builder.Services.AddHostedService<TradeConsumerWorker>();

var host = builder.Build();
await host.RunAsync();
