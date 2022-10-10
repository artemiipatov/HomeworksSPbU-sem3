namespace LazyTest;

using System.Threading;
using Lazy;
using NUnit.Framework;

public class Tests
{
    [Test]
    public void SerialLazyExecutesFunctionOnlyOnce()
    {
        var counter = 0;
        ILazy<int> lazy = new LazySerial<int>(() => ++counter);
        for (var i = 0; i < 10; i++)
        {
            Assert.AreEqual(1, lazy.Get());
        }

        Assert.AreEqual(1, counter);
    }

    [Test]
    public void ConcurrentLazyExecutesFunctionOnlyOnce()
    {
        var counter = 0;
        var threads = new Thread[1000];
        ILazy<int> lazy = new LazyConcurrent<int>(() => Interlocked.Increment(ref counter));

        for (var i = 0; i < 1000; i++)
        {
            threads[i] = new Thread(() =>
            {
                for (var _ = 0; _ < 1000; _++)
                {
                    Assert.AreEqual(1, lazy.Get());
                }
            });
        }

        foreach (var thread in threads)
        {
            thread.Start();
        }

        foreach (var thread in threads)
        {
            thread.Join();
        }

        Assert.AreEqual(1, counter);
    }

    [Test]
    public void LaziesCanReturnNull()
    {
        ILazy<object> lazySerial = new LazySerial<object>(() => null);
        ILazy<object> lazyConcurrent = new LazyConcurrent<object>(() => null);

        for (var i = 0; i < 5; i++)
        {
            Assert.IsNull(lazySerial.Get());
            Assert.IsNull(lazyConcurrent.Get());
        }
    }

    [Test]
    public void MultithreadingDoesNotCauseRaceCondition()
    {
        var counter = 0;
        var threads = new Thread[1000];
        ILazy<int> lazy = new LazyConcurrent<int>(() =>
        {
            for (var j = 0; j < 1000; j++)
            {
                Interlocked.Increment(ref counter);
            }

            return counter;
        });

        for (var i = 0; i < 1000; i++)
        {
            threads[i] = new Thread(() =>
            {
                for (var _ = 0; _ < 1000; _++)
                {
                    Assert.AreEqual(1000, lazy.Get());
                }
            });
        }

        foreach (var thread in threads)
        {
            thread.Start();
        }

        foreach (var thread in threads)
        {
            thread.Join();
        }
    }
}