namespace Benchmarks
{
    using System;
    using BenchmarkDotNet.Running;

    class Program
    {
        static void Main(string[] args)
        {
            var summary = BenchmarkRunner.Run<MessageSenderBenchmarks>();

            Console.WriteLine("Press any key to exit");
            Console.ReadKey();
        }
    }
}
