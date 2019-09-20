using Serilog;
using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

namespace ConsoleApp.TDF
{
    internal class Program
    {
        private static void Main()
        {
            Log.Logger = new LoggerConfiguration()
                .Enrich.WithThreadId()
                .Enrich.WithThreadName()
                .WriteTo.Console(outputTemplate: "{Timestamp:HH:mm:ss} {ThreadId} {Message}{NewLine}")
                .CreateLogger();

            Log.Information("Main()");

            var head = new BufferBlock<int>();
            var workActionBlock = new ActionBlock<int>(i =>
            {
                Log.Information("ActionBlock({i})", i);
                Thread.Sleep(TimeSpan.FromSeconds(1));
            }, ExOpts);

            head.LinkTo(workActionBlock);
            head.Completion.ContinueWith(t => workActionBlock.Complete());

            var sw = new Stopwatch();
            sw.Start();
            foreach (var i in Enumerable.Range(0, 10))
            {
                head.SendAsync(i);
            }

            head.Complete();

            Task.WaitAll(head.Completion, workActionBlock.Completion);

            sw.Stop();

            Log.Information("Finished in {elapsed}", sw.Elapsed);
            Log.CloseAndFlush();
        }

        #region Tunables

        private static ExecutionDataflowBlockOptions ExOpts =>
            new ExecutionDataflowBlockOptions
            {
                MaxDegreeOfParallelism = 1,
                MaxMessagesPerTask = 1,
                BoundedCapacity = 100
            };

        #endregion
    }
}
