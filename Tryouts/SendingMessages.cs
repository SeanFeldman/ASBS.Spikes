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
        private const int NumberOfMessages = 5000;
        static readonly Message TestMessage1K = Create1KMessage();

        public static async Task Main()
        {
            var connectionString = Environment.GetEnvironmentVariable("AzureServiceBus.ConnectionString");

            Console.WriteLine($"Sending {NumberOfMessages} mesasges");

            await SendUsingSingleSender(connectionString);

            await SendUsingMultipleSendersCreatedWithConnectionString(connectionString);

            await SendUsingMultipleSendersSharingConnection(connectionString);

            Console.WriteLine("All done. Press Enter to exit.");
            Console.ReadLine();
        }

        static async Task SendUsingSingleSender(string connectionString)
        {
            const string queuePath = "test-non-partitioned";
            
            var sender = new MessageSender(connectionString, queuePath);

            var stopwatch = Stopwatch.StartNew();
            stopwatch.Start();

            await Task.WhenAll(SendMessages(sender, NumberOfMessages)).ConfigureAwait(false);
            
            stopwatch.Stop();

            Console.WriteLine($"Elapsed time {stopwatch.Elapsed.TotalMilliseconds} msec");
            Console.WriteLine($"Throughput: {NumberOfMessages * 1.0 / stopwatch.Elapsed.TotalSeconds} msg/s");

            await WaitForAnyKey().ConfigureAwait(false);

            await CloseSenders(sender).ConfigureAwait(false);
        }

       
        static async Task SendUsingMultipleSendersCreatedWithConnectionString(string connectionString)
        {
            const string queuePath = "test-non-partitioned";

            var senders = new[]
            {
                new MessageSender(connectionString, queuePath), 
                new MessageSender(connectionString, queuePath), 
                new MessageSender(connectionString, queuePath), 
                new MessageSender(connectionString, queuePath), 
                new MessageSender(connectionString, queuePath) 
            };

            var stopwatch = Stopwatch.StartNew();
            stopwatch.Start();

            await Task.WhenAll(SendMessages(senders[0]), SendMessages(senders[1]), SendMessages(senders[2]), SendMessages(senders[3]), SendMessages(senders[4])).ConfigureAwait(false);

            stopwatch.Stop();

            Console.WriteLine($"Elapsed time {stopwatch.Elapsed.TotalMilliseconds} msec");
            Console.WriteLine($"Throughput: {NumberOfMessages * 1.0 / stopwatch.Elapsed.TotalSeconds} msg/s");

            await WaitForAnyKey().ConfigureAwait(false);

            await CloseSenders(senders).ConfigureAwait(false);
        }

        static async Task SendUsingMultipleSendersSharingConnection(string connectionString)
        {
            const string queuePath = "test-non-partitioned";

            var connection = new ServiceBusConnection(connectionString);

            var senders = new []
            {
                new MessageSender(connection, queuePath), 
                new MessageSender(connection, queuePath), 
                new MessageSender(connection, queuePath), 
                new MessageSender(connection, queuePath), 
                new MessageSender(connection, queuePath) 
            };

            var stopwatch = Stopwatch.StartNew();
            stopwatch.Start();

            await Task.WhenAll(SendMessages(senders[0]), SendMessages(senders[1]), SendMessages(senders[2]), SendMessages(senders[3]), SendMessages(senders[4])).ConfigureAwait(false);

            stopwatch.Stop();


            Console.WriteLine($"Elapsed time {stopwatch.Elapsed.TotalMilliseconds} msec");
            Console.WriteLine($"Throughput: {NumberOfMessages * 1.0 / stopwatch.Elapsed.TotalSeconds} msg/s");

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
