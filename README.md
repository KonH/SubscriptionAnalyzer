# Subscription Analyzer

## Summary

Roslyn analyzers to track custom event manager subscriptions.

## Examples

```csharp
// Custom attribute with this name is required to track sub/unsub methods
public sealed class TrackingSubscriptionAttribute : Attribute {}

public static class EventManager {
    [TrackingSubscription]
    public static void Subscribe<T>(object target, Action<T> handler) {}

    [TrackingSubscription]
    public static void Unsubscribe<T>(Action<T> handler) {}
}

public sealed class InvalidUsage1 {
    public void Init() {
        // Warning:
        // [SA1001] Subscribe call found, but no corresponding Unsubscribe call found
        EventManager.Subscribe<object>(this, Handle);
    }

    public void Deinit() {}

    void Handle(object _) {}
}
```

## Installation

### Unity 2020.2+

Follow instructions - https://docs.unity3d.com/2020.2/Documentation/Manual/roslyn-analyzers.html

### Unity (older)

Add UPM package https://github.com/tertle/com.bovinelabs.analyzers and put analyzer DLL in expected location (RoslynAnalyzers by default)