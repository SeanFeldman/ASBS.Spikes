using System;

namespace MessageSenderBenchmarking
{
    using System.Threading.Tasks;
    using BenchmarkDotNet.Attributes;
    using BenchmarkDotNet.Attributes.Jobs;
    using BenchmarkDotNet.Running;
    using Microsoft.Azure.ServiceBus;
    using Microsoft.Azure.ServiceBus.Core;

    class Program
    {
        static void Main(string[] args)
        {
            var summary = BenchmarkRunner.Run<MessageSenderBenchmarks>();
        }
    }

    [CoreJob, ClrJob]
    [MemoryDiagnoser]
    public class MessageSenderBenchmarks
    {
        private ServiceBusConnection connection;

        [GlobalSetup]
        public void GlobalSetup()
        {
            var connectionString = Environment.GetEnvironmentVariable("AzureServiceBus.ConnectionString", EnvironmentVariableTarget.Machine);
            connection = new ServiceBusConnection(connectionString);
        }

        [GlobalCleanup]
        public void GlobalCleanup()
        {
            connection.CloseAsync().GetAwaiter().GetResult();
        }


        [Benchmark]
        public MessageSender CreateMessageSender()
        {
            return new MessageSender(connection, "queue-0");
        }

        [Benchmark]
        public async Task CreateAndDisposeMessageSender()
        {
            var sender = new MessageSender(connection, "queue-0");
            await sender.CloseAsync().ConfigureAwait(false);
        }
    }
}
