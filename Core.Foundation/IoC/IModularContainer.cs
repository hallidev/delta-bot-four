using System;

namespace Core.Foundation.IoC
{
    public interface IModularContainer
    {
        void RegisterLogger(string logFilename);
        void Register(Type from, Type to);
        void Register<TFrom, TTo>() where TTo : class, TFrom where TFrom : class;
        void Register<TFrom>(Func<TFrom> instanceCreator) where TFrom : class;
        void RegisterSingleton<TConcrete>() where TConcrete : class;
        void RegisterSingleton<TConcrete>(TConcrete instance) where TConcrete : class;
        void RegisterSingleton<TConcrete>(Func<TConcrete> instanceCreator) where TConcrete : class;
        T Resolve<T>();
    }
}
