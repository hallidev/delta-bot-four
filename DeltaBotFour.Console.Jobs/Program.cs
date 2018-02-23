using System;
using DeltaBotFour.DependencyResolver;

namespace DeltaBotFour.Console.Jobs
{
    public class Program
    {
        private static DeltaBotFourContainer _container;

        public static void Main(string[] args)
        {
            // Perform DI Container registrations. From this point on all dependencies are available
            _container = new DeltaBotFourContainer().Install(new RegistrationCatalogFactory().GetRegistrationCatalog());
        }
    }
}
