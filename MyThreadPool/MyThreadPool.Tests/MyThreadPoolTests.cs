namespace MyThreadPool.Tests;

using System;
using System.Linq;
using System.Threading;
using NUnit.Framework;

public class MyThreadPoolTests
{
    private static readonly int ThreadsCount = Environment.ProcessorCount;

    [Test]
    public void ThereAreNoDeadLocksOnLargeAmountOfTasks()
    {
        for (var _ = 0; _ < 10; _++)
        {
            const int numberOfTasks = 10000;
            const long number = 200000;

            using var threadPool = new MyThreadPool(ThreadsCount);
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
        using var threadPool = new MyThreadPool(ThreadsCount);

        var function = () => 1000;

        threadPool.Submit(function);
        threadPool.Shutdown();
        Assert.Throws<MyThreadPoolTerminatedException>(() => threadPool.Submit(function));
    }

    [Test]
    public void ContinueWithThrowsExceptionAfterShutdown()
    {
        using var threadPool = new MyThreadPool(ThreadsCount);

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

        Thread.Sleep(100);

        threadPool.Shutdown();

        Assert.AreEqual(49995000, task.Result);
        Assert.Throws<MyThreadPoolTerminatedException>(() => task.ContinueWith(value => value * 100));
    }

    [Test]
    public void ThreadsWaitsUntilResultComputesIfItIsNotComputedYet()
    {
        using var threadPool = new MyThreadPool(ThreadsCount);

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
        using var threadPool = new MyThreadPool(ThreadsCount);
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

    [Test]
    public void ExceptionShouldBeThrownOnResultIfThreadPoolIsShutDownBeforeTaskIsCompleted()
    {
        using var threadPool = new MyThreadPool(ThreadsCount);

        var function = () =>
        {
            Thread.Sleep(1000);
            return 10;
        };

        var continuation = threadPool.Submit(function).ContinueWith(value => value.ToString());
        threadPool.Shutdown();
        Assert.Throws<AggregateException>(() =>
        {
            var result = continuation.Result;
        });
    }

    [TestCase(2)]
    [TestCase(4)]
    [TestCase(6)]
    public void MyThreadPoolSuccessfullyCreatesSpecifiedNumberOfThreads(int numberOfThreads)
    {
        var function = () =>
        {
            Thread.Sleep(300);
            return Thread.CurrentThread.ManagedThreadId;
        };

        using var myThreadPool = new MyThreadPool(numberOfThreads);
        var tasks = new IMyTask<int>[numberOfThreads];
        var tasksResults = new int[numberOfThreads];

        for (int i = 0; i < numberOfThreads; i++)
        {
            tasks[i] = myThreadPool.Submit(function);
        }

        Thread.Sleep(2000);

        for (var i = 0; i < numberOfThreads; i++)
        {
            tasksResults[i] = tasks[i].Result;
        }

        Assert.IsTrue(tasksResults.Distinct().Count() == tasksResults.Length);
    }
}