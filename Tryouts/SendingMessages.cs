namespace Tryouts
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using Microsoft.Azure.ServiceBus;
    using Microsoft.Azure.ServiceBus.Core;

    public class SendingMessages
    {
        const string queuePath = "queue";
        static readonly Message TestMessage1K = Create1KMessage();

        public static async Task Main()
        {
            var connectionString = Environment.GetEnvironmentVariable("AzureServiceBus.ConnectionString");
            
            var numberOfMessages = 10000;

            Console.WriteLine($"Sending {numberOfMessages} messages\n");
            
            var sendersToUse = new[] {1, 2, 3, 4, 5, 8, 10};

            foreach (var senderQuantity in sendersToUse)
            {
                for (var iterations = 0; iterations < 10; iterations++)
                {
                    Console.WriteLine($"--- Run #{iterations+1}, {senderQuantity} senders ---");

                    await SendUsingMultipleSendersCreatedWithConnectionString(connectionString, numberOfSenders:senderQuantity, totalNumberOfMessages:numberOfMessages);
                }
            }

            Console.WriteLine("All done. Press Enter to exit.");
            Console.ReadLine();
        }
       
        static async Task SendUsingMultipleSendersCreatedWithConnectionString(string connectionString, int numberOfSenders, int totalNumberOfMessages)
        {

            var senders = new MessageSender[numberOfSenders];

            for (var i = 0; i < numberOfSenders; i++)
            {
                senders[i] = new MessageSender(connectionString, $"{queuePath}-{i}");
            }

            var numberOfMessagesToSend = totalNumberOfMessages / numberOfSenders;

            var stopwatch = Stopwatch.StartNew();
            
            await Task.WhenAll(senders.Select(sender => SendMessages(sender, numberOfMessagesToSend))).ConfigureAwait(false);

            stopwatch.Stop();

            Console.WriteLine($"Time:       {Math.Floor(stopwatch.Elapsed.TotalMilliseconds)} msec");
            Console.WriteLine($"Throughput: {Math.Floor(totalNumberOfMessages * 1.0 / stopwatch.Elapsed.TotalSeconds)} msg/s");
            Console.WriteLine();

            await WaitForAnyKey().ConfigureAwait(false);

            await CloseSenders(senders).ConfigureAwait(false);
        }


        static Message Create1KMessage()
        {
            var content = $"message {DateTime.Now:s} ";
            var paddedContent = content.PadRight(1024, 'x');

            return new Message(Encoding.UTF8.GetBytes(paddedContent));
        }

        static Task SendMessages(MessageSender sender, int numberOfMessages = 1000)
        {
            var sends = new List<Task>(numberOfMessages);

            for (var i = 0; i < numberOfMessages; i++)
            {
                sends.Add(sender.SendAsync(TestMessage1K));
            }

            return Task.WhenAll(sends);
        }

        static async Task WaitForAnyKey()
        {
            return;
            Console.WriteLine("Press ESC to close connections and exit");
            do
            {
                while (!Console.KeyAvailable)
                {
                    await Task.Delay(100).ConfigureAwait(false);
                }
            } while (Console.ReadKey(true).Key != ConsoleKey.Escape);
        }

        static async Task CloseSenders(params MessageSender[] senders)
        {
            foreach (var sender in senders)
            {
                await sender.CloseAsync().ConfigureAwait(false);
            }
        }
    }
}
