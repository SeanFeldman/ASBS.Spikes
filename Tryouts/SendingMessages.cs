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
        const string queuePath = "test-non-partitioned"; //"test-partitioned"
        static readonly Message TestMessage1K = Create1KMessage();

        public static async Task Main()
        {
            var connectionString = Environment.GetEnvironmentVariable("AzureServiceBus.ConnectionString");

            var numberOfMessages = 10000;

            Console.WriteLine($"Sending {numberOfMessages} messages");
            
            await SendUsingSingleSender(connectionString, numberOfMessages);

            await SendUsingMultipleSendersCreatedWithConnectionString(connectionString, numberOfSenders:10, totalNumberOfMessages:numberOfMessages);

            //~not really needed anymore
            //~await SendUsingMultipleSendersSharingConnection(connectionString, numberOfSenders:2, totalNumberOfMessages:numberOfMessages);

            Console.WriteLine("All done. Press Enter to exit.");
            Console.ReadLine();
        }

        static async Task SendUsingSingleSender(string connectionString, int numberOfMessages)
        {
            var sender = new MessageSender(connectionString, queuePath);

            var stopwatch = Stopwatch.StartNew();

            await Task.WhenAll(SendMessages(sender, numberOfMessages)).ConfigureAwait(false);
            
            stopwatch.Stop();

            Console.WriteLine($"Elapsed time {stopwatch.Elapsed.TotalMilliseconds} msec");
            Console.WriteLine($"Throughput: {numberOfMessages * 1.0 / stopwatch.Elapsed.TotalSeconds} msg/s");

            await WaitForAnyKey().ConfigureAwait(false);

            await CloseSenders(sender).ConfigureAwait(false);
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

            Console.WriteLine($"Elapsed time {stopwatch.Elapsed.TotalMilliseconds} msec");
            Console.WriteLine($"Throughput: {totalNumberOfMessages * 1.0 / stopwatch.Elapsed.TotalSeconds} msg/s");

            await WaitForAnyKey().ConfigureAwait(false);

            await CloseSenders(senders).ConfigureAwait(false);
        }

        static async Task SendUsingMultipleSendersSharingConnection(string connectionString, int numberOfSenders, int totalNumberOfMessages)
        {
            var connection = new ServiceBusConnection(connectionString);

            var senders = new MessageSender[numberOfSenders];

            for (var i = 0; i < numberOfSenders; i++)
            {
                senders[i] = new MessageSender(connection, queuePath);
            }

            var numberOfMessagesToSend = totalNumberOfMessages / numberOfSenders;

            var stopwatch = Stopwatch.StartNew();

            await Task.WhenAll(senders.Select(sender => SendMessages(sender, numberOfMessagesToSend))).ConfigureAwait(false);

            stopwatch.Stop();

            Console.WriteLine($"Elapsed time {stopwatch.Elapsed.TotalMilliseconds} msec");
            Console.WriteLine($"Throughput: {totalNumberOfMessages * 1.0 / stopwatch.Elapsed.TotalSeconds} msg/s");

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
