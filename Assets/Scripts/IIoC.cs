using System;

public interface IIoC
{
    void RegisterShared<TInterface, TImplementation>();

    void Register<TInterface, TImplementation>();

    void Register<TImplementation>();

    void RegisterCustomShared<T>(Func<object> func);

    void RegisterCustom<T>(Func<object> func);

    TInterface Resolve<TInterface>(params object[] args);

    object Resolve(Type t, params object[] args);
}