﻿using DeltaBotFour.DependencyResolver;
using System;
using DeltaBotFour.Infrastructure.Interface;
using DeltaBotFour.Reddit.Interface;

namespace DeltaBotFour.Console
{
    public class Program
    {
        private static DeltaBotFourContainer _container;

        public static void Main(string[] args)
        {
            System.Console.WriteLine("------DB4 Startup-----");

            // Perform DI Container registrations. From this point on all dependencies are available
            _container = new DeltaBotFourContainer().Install(new RegistrationCatalogFactory().GetRegistrationCatalog());

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

            System.Console.WriteLine("------DB4 Shutdown-----");
        }
    }
}
