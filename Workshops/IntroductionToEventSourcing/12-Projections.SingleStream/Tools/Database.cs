using System.Text.Json;

namespace IntroductionToEventSourcing.GettingStateFromEvents.Tools;

public class Database
{
    private readonly Dictionary<string, object> storage = new();

    public void Store<T>(Guid id, T obj) where T: class
    {
        storage[GetId<T>(id)] = obj;
    }

    public void Delete<T>(Guid id)
    {
        storage.Remove(GetId<T>(id));
    }

    public T? Get<T>(Guid id) where T: class
    {
        var idToResolve = GetId<T>(id);
        var value = storage.TryGetValue(idToResolve, out var result) ?
            // Clone to simulate getting new instance on loading
            result
            : null;
        if (value == null)
            return null;
        var deserialized = JsonSerializer.Deserialize<T>(JsonSerializer.Serialize((T)result));
        return deserialized;
    }

    private static string GetId<T>(Guid id) => $"{typeof(T).Name}-{id}";
}
