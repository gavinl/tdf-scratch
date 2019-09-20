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
            #region Console logging
            Log.Logger = new LoggerConfiguration()
                .Enrich.WithThreadId()
                .Enrich.WithThreadName()
                .WriteTo.Console(outputTemplate: "{Timestamp:HH:mm:ss} {ThreadId} {Message}{NewLine}")
                .CreateLogger();
            #endregion

            Log.Information("Main()");

            var head = new BufferBlock<int>();
            var tail = new ActionBlock<int>(i =>
            {
                Log.Information("ActionBlock({i})", i);
                Thread.Sleep(TimeSpan.FromSeconds(1));
            });

            head.LinkTo(tail);

            #region completion closures

            head.Completion.ContinueWith(t =>
            {
                Log.Information("head completed!");
                tail.Complete();
            });
            tail.Completion.ContinueWith(t => Log.Information("workActionBlock completed!"));

            #endregion


            var sw = new Stopwatch();
            sw.Start();
            foreach (var i in Enumerable.Range(0, 10))
            {
                head.SendAsync(i);
            }
            // no more to send
            head.Complete();

            // wait for all threads to complete (head will be completed)
            Task.WaitAll(head.Completion, tail.Completion);

            sw.Stop();

            Log.Information("Finished in {elapsed}", sw.Elapsed);
            Log.CloseAndFlush();
        }
    }
}
