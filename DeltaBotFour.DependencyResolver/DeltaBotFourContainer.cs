using System;
using Core.Foundation.IoC;
using SimpleInjector;
using SimpleInjector.Lifestyles;

namespace DeltaBotFour.DependencyResolver
{
    public class DeltaBotFourContainer : IDisposable, IModularContainer
    {
        private readonly Container _container;

        public DeltaBotFourContainer()
        {
            _container = new Container();
            _container.Options.DefaultScopedLifestyle = new AsyncScopedLifestyle();
        }

        public DeltaBotFourContainer Install(IRegistrationCatalog catalog)
        {
            catalog.Register(this);
            _container.Verify();
            return this;
        }

        public IDisposable BeginLifestyleScope()
        {
            return AsyncScopedLifestyle.BeginScope(_container);
        }

        public T Resolve<T>(Type controllerType)
        {
            return (T)_container.GetInstance(controllerType);
        }

        public T Resolve<T>()
        {
            return (T)_container.GetInstance(typeof(T));
        }

        public void Register(Type from, Type to)
        {
            _container.Register(from, to, Lifestyle.Singleton);
        }

        public void Register<TFrom, TTo>() where TTo : class, TFrom where TFrom : class
        {
            _container.Register<TFrom, TTo>(Lifestyle.Singleton);
        }

        public void Register<TFrom>(Func<TFrom> instanceCreator) where TFrom : class
        {
            _container.Register(instanceCreator, Lifestyle.Singleton);
        }

        public void RegisterSingleton<TConcrete>() where TConcrete : class
        {
            _container.RegisterSingleton<TConcrete>();
        }

        public void RegisterSingleton<TConcrete>(TConcrete instance) where TConcrete : class
        {
            _container.RegisterSingleton(instance);
        }

        public void RegisterSingleton<TConcrete>(Func<TConcrete> instanceCreator) where TConcrete : class
        {
            _container.RegisterSingleton<TConcrete>();
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private void Release()
        {
            _container?.Dispose();
        }

        private void Dispose(bool disposing)
        {
            if (!disposing) return;
            Release();
        }

        ~DeltaBotFourContainer()
        {
            Dispose(false);
        }
    }
}
