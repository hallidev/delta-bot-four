using Core.Foundation.IoC;

namespace DeltaBotFour.DependencyResolver
{
    public class RegistrationCatalogFactory
    {
        public IRegistrationCatalog GetRegistrationCatalog()
        {
            return new DeltaBotFourRegistrationCatalog();
        }
    }
}
