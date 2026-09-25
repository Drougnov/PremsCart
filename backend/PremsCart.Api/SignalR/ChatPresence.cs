using System.Collections.Concurrent;

namespace PremsCart.Api.SignalR;

// Suitable for the single API server used by this student project.
public sealed class ChatPresence
{
    private readonly ConcurrentDictionary<int, ConcurrentDictionary<string, byte>> _connections = new();

    public bool Connect(int userId, string connectionId)
    {
        var connections = _connections.GetOrAdd(userId, _ => new());
        connections[connectionId] = 0;
        return connections.Count == 1;
    }

    public bool Disconnect(int userId, string connectionId)
    {
        if (!_connections.TryGetValue(userId, out var connections)) return false;
        connections.TryRemove(connectionId, out _);
        if (!connections.IsEmpty) return false;
        ((ICollection<KeyValuePair<int, ConcurrentDictionary<string, byte>>>)_connections)
            .Remove(new(userId, connections));
        return true;
    }

    public bool IsOnline(int userId) => _connections.TryGetValue(userId, out var connections) && !connections.IsEmpty;
}
