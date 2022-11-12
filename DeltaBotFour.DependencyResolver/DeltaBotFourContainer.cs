﻿using System;
using Core.Foundation.IoC;
using DeltaBotFour.Shared.Implementation;
using NLog;
using NLog.Config;
using NLog.Targets;
using SimpleInjector;
using SimpleInjector.Lifestyles;
using ILogger = DeltaBotFour.Shared.Logging.ILogger;

namespace DeltaBotFour.DependencyResolver;

public class DeltaBotFourContainer : IDisposable, IModularContainer
{
    private readonly Container _container;

    public DeltaBotFourContainer()
    {
        _container = new Container();
        _container.Options.DefaultScopedLifestyle = new AsyncScopedLifestyle();
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    public void RegisterLogger(string logFilename)
    {
        var config = new LoggingConfiguration();

        var logfile = new FileTarget
        {
            FileName = logFilename,
            Name = "logfile",
            Layout =
                "${longdate}|${level:uppercase=true}|${logger}|${message}|${exception:format=ToString,StackTrace}${newline}"
        };

        var logconsole = new ConsoleTarget
        {
            Name = "logconsole",
            Layout = "${longdate}|${level:uppercase=true}|${logger:shortName=true}|${message}"
        };

        config.LoggingRules.Add(new LoggingRule("*", LogLevel.Info, logconsole));
        config.LoggingRules.Add(new LoggingRule("*", LogLevel.Warn, logfile));
        config.LoggingRules.Add(new LoggingRule("*", LogLevel.Error, logfile));

        LogManager.Configuration = config;

        _container.RegisterConditional(typeof(ILogger),
            context => typeof(NLogProxy<>).MakeGenericType(context.Consumer.ImplementationType),
            Lifestyle.Singleton, context => true);
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
        _container.RegisterInstance(instance);
    }

    public void RegisterSingleton<TConcrete>(Func<TConcrete> instanceCreator) where TConcrete : class
    {
        _container.RegisterSingleton<TConcrete>();
    }

    public T Resolve<T>()
    {
        return (T) _container.GetInstance(typeof(T));
    }

    public DeltaBotFourContainer Install(IRegistrationCatalog catalog)
    {
        catalog.Register(this);
        _container.Verify();
        return this;
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