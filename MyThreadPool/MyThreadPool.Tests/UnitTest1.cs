using System.Threading;

namespace MyThreadPool.Tests;

using System;
using NUnit.Framework;

public class Tests
{
    [SetUp]
    public void Setup()
    {
    }

    [Test]
    public void Test1()
    {
        var threadPool = new MyThreadPool(4);
        int numberOfTasks = 100;
        var resultArray = new IMyTask<long>[numberOfTasks];
        long number = 200000;
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
            var result = resultArray[numberOfTasks - 1].ContinueWith<string>(func).Result;
            Assert.AreEqual(resultArray[numberOfTasks - 1].Result.ToString(), result);
        }

        for (long i = 0; i < numberOfTasks; i++)
        {
            Assert.AreEqual(((number - 1) * number / 2) + ((number + i - 1 + number) * i / 2), resultArray[i].Result);
        }

        threadPool.Shutdown();
    }
}