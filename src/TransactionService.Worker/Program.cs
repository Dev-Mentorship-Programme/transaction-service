using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using TransactionService.Worker;
using TransactionService.Infrastructure.Extensions;
using TransactionService.Infrastructure.Messaging;
using TransactionService.Domain.Interfaces;

var executablePath = System.Reflection.Assembly.GetExecutingAssembly().Location;
var executableDirectory = Path.GetDirectoryName(executablePath) ?? Directory.GetCurrentDirectory();

var host = Host.CreateDefaultBuilder(args)
    .UseContentRoot(executableDirectory)
    .ConfigureServices((context, services) =>
    {
        services.AddInfrastructure(context.Configuration);
        services.AddHostedService<TransactionEventConsumerWorker>();
        services.AddScoped<ITransactionEventConsumer, TransactionEventConsumer>();
        services.AddMetrics();
    })
    .Build();

await host.RunAsync();
