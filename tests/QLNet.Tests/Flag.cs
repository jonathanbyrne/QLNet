using QLNet.Patterns;

namespace QLNet.Tests;

[JetBrains.Annotations.PublicAPI] public class Flag : IObserver
{
    private bool up_;

    public Flag()
    {
        up_ = false;
    }

    public void raise() { up_ = true; }
    public void lower() { up_ = false; }
    public bool isUp() => up_;

    public void update() { raise(); }
}