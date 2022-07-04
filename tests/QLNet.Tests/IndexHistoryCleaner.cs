using System;
using QLNet.Indexes;

namespace QLNet.Tests;

[JetBrains.Annotations.PublicAPI] public class IndexHistoryCleaner : IDisposable
{
    public void Dispose() { IndexManager.instance().clearHistories(); }
}