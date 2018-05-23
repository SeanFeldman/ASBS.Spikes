namespace Benchmarks
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Text;
    using System.Threading.Tasks;
    using BenchmarkDotNet.Attributes;
    using BenchmarkDotNet.Attributes.Jobs;
    using BenchmarkDotNet.Engines;
    using Microsoft.Azure.ServiceBus;
    using Microsoft.Azure.ServiceBus.Core;

//    [SimpleJob(RunStrategy.ColdStart)]
    [CoreJob, ClrJob]
    public class MessageSenderBenchmarks
    {
        MessageSender[] senders;
        string connectionString = Environment.GetEnvironmentVariable("AzureServiceBus.ConnectionString");
        const string queuePath = "test-non-partitioned";
        const int NumberOfMessages = 5000;
        static readonly Message TestMessage1K = Create1KMessage();

        [GlobalSetup(Target = nameof(SingleMessageSender))]
        public void GlobalSetup_SingleMessageSender()
        {
            senders = new[] {new MessageSender(connectionString, queuePath) };
        }

        [GlobalSetup(Target = nameof(MultipleMessageSendersWithSingleConnection))]
        public void GlobalSetup_MultipleMessageSendersWithSingleConnection()
        {
            var connection = new ServiceBusConnection(connectionString);

            senders = new[]
            {
                new MessageSender(connection, queuePath),
                new MessageSender(connection, queuePath),
                new MessageSender(connection, queuePath),
                new MessageSender(connection, queuePath),
                new MessageSender(connection, queuePath)
            };
        }

        [GlobalSetup(Target = nameof(MultipleMessageSendersWithDifferentConnections))]
        public void GlobalSetup_MultipleMessageSendersWithDifferentConnections()
        {
            senders = new[]
            {
                new MessageSender(connectionString, queuePath),
                new MessageSender(connectionString, queuePath),
                new MessageSender(connectionString, queuePath),
                new MessageSender(connectionString, queuePath),
                new MessageSender(connectionString, queuePath)
            };
        }

        [GlobalCleanup]
        public void GlobalCleanup()
        {
            foreach (var sender in senders)
            {
                sender.CloseAsync().GetAwaiter().GetResult();
            }
        }


        [Benchmark(Baseline = true)]
        public async Task SingleMessageSender()
        {
            var stopwatch = Stopwatch.StartNew();
            stopwatch.Start();

            await Task.WhenAll(SendMessages(senders[0], NumberOfMessages)).ConfigureAwait(false);

            stopwatch.Stop();

            Console.WriteLine("I");
            Console.WriteLine($"Elapsed time {stopwatch.Elapsed.TotalMilliseconds} msec");
            Console.WriteLine($"Throughput: {NumberOfMessages * 1.0 / stopwatch.Elapsed.TotalSeconds} msg/s");

        }

        [Benchmark]
        public async Task MultipleMessageSendersWithSingleConnection()
        {
        }

        [Benchmark]
        public async Task MultipleMessageSendersWithDifferentConnections()
        {
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
    }
}