using DeltaBotFour.DependencyResolver;
using DeltaBotFour.ServiceInterfaces;
using System;

namespace DeltaBotFour.Console
{
    public class Program
    {
        private static DeltaBotFourContainer _container;

        public static void Main(string[] args)
        {
            // Perform DI Container registrations. From this point on all dependencies are available
            _container = new DeltaBotFourContainer().Install(new RegistrationCatalogFactory().GetRegistrationCatalog());

            // Start comment monitor
            var commentMonitor = _container.Resolve<ICommentMonitor>();
            commentMonitor.Start();

            // Start queue dispatcher
            var queueDispatcher = _container.Resolve<IDB4QueueDispatcher>();
            queueDispatcher.Start();

            while(true)
            {
                if (System.Console.KeyAvailable && System.Console.ReadKey(true).Key == ConsoleKey.Escape)
                {
                    commentMonitor.Stop();
                    queueDispatcher.Stop();
                    return;
                }
            }
        }
    }
}
