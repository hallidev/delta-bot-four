using DeltaBotFour.DependencyResolver;
using DeltaBotFour.ServiceInterfaces;
using System;
using System.Linq;
using DeltaBotFour.Models;

namespace DeltaBotFour.Console
{
    public class Program
    {
        private const string ArgDelimiter = "=";
        private const string ModeArg = "mode=";
        private const string CommentMonitorMode = "commentmonitor";
        private const string DeltaboardMode = "deltaboard";

        private static DeltaBotFourContainer _container;

        public static void Main(string[] args)
        {
            System.Console.WriteLine("------DB4 Startup-----");

            // Determine what mode we're starting in
            string mode = args
                .FirstOrDefault(arg => arg.ToLower().Trim().Contains(ModeArg))?
                .Split(ArgDelimiter)[1];

            mode = CommentMonitorMode;

            if (string.IsNullOrEmpty(mode))
            {
                System.Console.WriteLine("The 'mode' argument must be specified. Valid modes are 'commentmonitor', 'deltaboard'.");
                return;
            }

            // Perform DI Container registrations. From this point on all dependencies are available
            _container = new DeltaBotFourContainer().Install(new RegistrationCatalogFactory().GetRegistrationCatalog());

            switch (mode)
            {
                case CommentMonitorMode:
                    // Start comment monitor
                    var commentMonitor = _container.Resolve<ICommentMonitor>();
                    commentMonitor.Start();

                    // Start queue dispatcher
                    var queueDispatcher = _container.Resolve<IDB4QueueDispatcher>();
                    queueDispatcher.Start();

                    while (true)
                    {
                        if (System.Console.KeyAvailable && System.Console.ReadKey(true).Key == ConsoleKey.Escape)
                        {
                            commentMonitor.Stop();
                            queueDispatcher.Stop();
                            break;
                        }
                    }
                    break;
                case DeltaboardMode:
                    var deltaboardBuilder = _container.Resolve<IDeltaboardBuilder>();
                    deltaboardBuilder.Build(DeltaboardType.Daily);
                    break;
                default:
                    System.Console.WriteLine($"Unknown mode '{mode}'.");
                    break;
            }

            System.Console.WriteLine("------DB4 Shutdown-----");
        }
    }
}
