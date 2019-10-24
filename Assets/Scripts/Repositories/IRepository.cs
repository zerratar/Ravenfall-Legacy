using System.Collections.Generic;
using System.Threading.Tasks;

public interface IRepository<T>
{
    void Save();

    Task SaveAsync();

    void Load();

    void Add(string key, T item);

    bool Remove(T item);

    T Get(string key);

    IReadOnlyList<T> All();
}