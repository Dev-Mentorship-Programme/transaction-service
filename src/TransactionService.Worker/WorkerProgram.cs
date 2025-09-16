using System;
using TransactionService.Infrastructure.Messaging;
using TransactionService.Infrastructure.DocGen;
using TransactionService.Infrastructure.Signing;

namespace TransactionService.Worker;

public class WorkerProgram
{
    public static async Task RunWorkerProgram()
    {
        var queue = new InMemoryQueue();
        var docGen = new PdfDocumentGenerator();
        var signer = new HmacLinkSigner("dev-key-worker");

        Console.WriteLine("Worker started. Polling queue...");
        while (true)
        {
            if (queue.TryDequeue(out var item) && item != null)
            {
                try
                {
                    var id = Guid.Parse(item.Id);
                    var bytes = await docGen.GenerateReceiptAsync(id);
                    var token = signer.SignToken(id, TimeSpan.FromHours(1));
                    Console.WriteLine($"Processed {id}, token: {token[..20]}...");
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Worker error: " + ex.Message);
                }
            }
            else
            {
                await Task.Delay(1000);
            }
        }
    }
}
