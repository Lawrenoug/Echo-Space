using System.Collections.Generic;

namespace EchoSpace.Core.Input;

public sealed class InputBuffer
{
    private readonly Dictionary<string, double> _bufferedActions = new();

    public void Buffer(string actionName, double timestampSeconds)
    {
        _bufferedActions[actionName] = timestampSeconds;
    }

    public bool HasBuffered(string actionName, double timestampSeconds, double bufferWindowSeconds)
    {
        return _bufferedActions.TryGetValue(actionName, out var bufferedAt)
            && timestampSeconds - bufferedAt <= bufferWindowSeconds;
    }

    public bool Consume(string actionName, double timestampSeconds, double bufferWindowSeconds)
    {
        if (!HasBuffered(actionName, timestampSeconds, bufferWindowSeconds))
        {
            return false;
        }

        _bufferedActions.Remove(actionName);
        return true;
    }

    public void ExpireOlderThan(double timestampSeconds, double bufferWindowSeconds)
    {
        var expiredKeys = new List<string>();

        foreach (var (actionName, bufferedAt) in _bufferedActions)
        {
            if (timestampSeconds - bufferedAt > bufferWindowSeconds)
            {
                expiredKeys.Add(actionName);
            }
        }

        foreach (var actionName in expiredKeys)
        {
            _bufferedActions.Remove(actionName);
        }
    }

    public void Clear()
    {
        _bufferedActions.Clear();
    }
}
