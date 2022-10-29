using System.Threading.Tasks;

namespace MyThreadPool.Tests;

using System;
using System.Threading;
using NUnit.Framework;

public class MyThreadPoolTests
{
    [Test]
    public void ThereAreNoDeadLocksOnLargeAmountOfTasks()
    {
        for (var _ = 0; _ < 10; _++)
        {
            const int numberOfTasks = 10000;
            const long number = 200000;

            using var threadPool = new MyThreadPool(6);
            var resultArray = new IMyTask<long>[numberOfTasks];

            for (var i = 0; i < numberOfTasks; i++)
            {
                var j = i;
                resultArray[i] = threadPool.Submit(() =>
                {
                    long counter = 0;
                    for (long k = 0; k < number + j; k++)
                    {
                        counter += k;
                    }

                    return counter;
                });
            }

            Func<long, string> func = l => l.ToString();

            for (var i = 10; i < numberOfTasks; i++)
            {
                var result = resultArray[numberOfTasks - 1].ContinueWith(func).Result;
                Assert.AreEqual(resultArray[numberOfTasks - 1].Result.ToString(), result);
            }

            for (long i = 0; i < numberOfTasks; i++)
            {
                Assert.AreEqual(((number - 1) * number / 2) + ((number + i - 1 + number) * i / 2),
                    resultArray[i].Result);
            }
        }
    }

    [Test]
    public void SubmitThrowsExceptionAfterShutdown()
    {
        using var threadPool = new MyThreadPool(2);

        var function = () => 1000;

        threadPool.Submit(function);
        threadPool.Shutdown();
        Assert.Throws<Exception>(() => threadPool.Submit(function));
    }

    [Test]
    public void ContinueWithThrowsExceptionAfterShutdown()
    {
        using var threadPool = new MyThreadPool(2);

        var function = () =>
        {
            long counter = 0;
            for (long k = 1; k < 10000; k++)
            {
                counter += k;
            }

            return counter;
        };

        var task = threadPool.Submit(function);
        var firstContinuation = task.ContinueWith(value => value.ToString());

        threadPool.Shutdown();

        Assert.AreEqual(49995000, task.Result);
        Assert.AreEqual("49995000", firstContinuation.Result);
        Assert.Throws<Exception>(() => task.ContinueWith(value => value * 100));
    }

    [Test]
    public void ThreadsWaitsUntilResultComputesIfItIsNotComputedYet()
    {
        using var threadPool = new MyThreadPool(2);

        var function = () =>
        {
            Thread.Sleep(1000);
            return 10;
        };

        var mainTask = threadPool.Submit(function);
        Assert.AreEqual("10", mainTask.ContinueWith(number => number.ToString()).Result);
    }

    [Test]
    public void SubmitAndContinueWithAreThreadSafe()
    {
        using var threadPool = new MyThreadPool(4);
        var threads = new Thread[4];
        var continuationsArrays = new IMyTask<string>[4, 1000];

        Func<int> MultiplyBy10(int number) => () => number * 10;

        for (var j = 0; j < 4; j++)
        {
            var numberOfArray = j;

            threads[j] = new Thread(() =>
            {
                for (var i = 0; i < 1000; i++)
                {
                    continuationsArrays[numberOfArray, i] = threadPool.Submit(MultiplyBy10(i)).ContinueWith(value => value.ToString());
                }
            });
            threads[j].Start();
        }

        foreach (var thread in threads)
        {
            thread.Join();
        }

        for (var j = 0; j < 4; j++)
        {
            for (var i = 0; i < 1000; i++)
            {
                Assert.AreEqual((i * 10).ToString(), continuationsArrays[j, i].Result);
            }
        }
    }
}