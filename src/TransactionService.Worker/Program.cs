using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using TransactionService.Worker;
using TransactionService.Infrastructure.Extensions;
using TransactionService.Infrastructure.Messaging;
using TransactionService.Domain.Interfaces;
using TransactionService.Infrastructure.Data;

var host = Host.CreateDefaultBuilder(args)
    .ConfigureServices((context, services) =>
    {
        services.AddInfrastructure(context.Configuration);
        services.AddHostedService<CreateTransactionEventConsumerWorker>();
        services.AddScoped<ICreateTransactionEventConsumer, CreateTransactionEventConsumer>();
        services.AddDbContext<AppDbContext>(options => options.UseNpgsql(context.Configuration.GetConnectionString("DefaultConnection")));
    })
    .Build();

await host.RunAsync();
