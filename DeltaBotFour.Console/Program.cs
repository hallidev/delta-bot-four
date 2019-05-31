﻿using DeltaBotFour.DependencyResolver;
using System;
using System.Threading;
using DeltaBotFour.Infrastructure;
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

            var appConfiguration = _container.Resolve<AppConfiguration>();

            // Start monitoring comments / edits / private messages
            var activityMonitor = _container.Resolve<IActivityMonitor>();
            activityMonitor.Start(appConfiguration.CommentScanIntervalSeconds,
                appConfiguration.EditScanIntervalSeconds,
                appConfiguration.PMScanIntervalSeconds);

            // Start queue dispatcher
            var queueDispatcher = _container.Resolve<IDB4QueueDispatcher>();
            queueDispatcher.Start();

            // Refresh deltaboards on startup
            var deltaboardEditor = _container.Resolve<IDeltaboardEditor>();
            deltaboardEditor.RefreshDeltaboards();

            while (true)
            {
                if (System.Console.KeyAvailable && System.Console.ReadKey(true).Key == ConsoleKey.Escape)
                {
                    activityMonitor.Stop();
                    queueDispatcher.Stop();
                    break;
                }

                Thread.Sleep(100);
            }

            System.Console.WriteLine("------DB4 Shutdown-----");
        }
    }
}
