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
        private static async Task Main()
        {
            Log.Logger = new LoggerConfiguration()
                .Enrich.WithThreadId()
                .WriteTo.Console(outputTemplate: "{Timestamp:HH:mm:ss} {ThreadId} {Message}{NewLine}")
                .CreateLogger();

            Log.Information("Begin");

            var head = new BufferBlock<int>();
            var actionBlock = new ActionBlock<object>(i =>
            {
                Log.Information("ActionBlock({i}) starts", i);
                Thread.Sleep(TimeSpan.FromSeconds(1));
                Log.Information("ActionBlock({i}) finished", i);
            }, new ExecutionDataflowBlockOptions
            {
                MaxDegreeOfParallelism = 1
            });

            var transformBlock = new TransformBlock<int, string>(Transform, new ExecutionDataflowBlockOptions
            {
                MaxDegreeOfParallelism = 2
            });

            //head.LinkTo(workActionBlock);
            head.LinkTo(transformBlock);
            transformBlock.LinkTo(actionBlock);
            Log.Information("head => transformBlock => actionBlock");

            head.Completion.ContinueWith(t => transformBlock.Complete());
            transformBlock.Completion.ContinueWith(t => actionBlock.Complete());

            var sw = new Stopwatch();
            sw.Start();
            foreach (var i in Enumerable.Range(0, 10))
            {
                head.Post(i);
                //await head.SendAsync(i);
            }

            head.Complete();
            await head.Completion;
            await actionBlock.Completion;

            sw.Stop();

            Log.Information("Finished in {elapsed}", sw.Elapsed);
            Log.CloseAndFlush();
        }

        private static string Transform(int i)
        {
            Log.Information("Transform({i}) starts work on {i}", i, i);
            var result = i.ToString(); 
            Thread.Sleep(TimeSpan.FromSeconds(1));
            
            Log.Information("Transform({i}) finished work on {i} with result {result}", i, i, result);
            return result;
        }
    }
}
