using System;
using System.Threading;
using NUnit.Framework;

namespace MyThreadPool.Tests;

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
        var resultArray = new IMyTask<long>[10000];
        
        for (var i = 0; i < 10000; i++)
        {
            var j = i;
            resultArray[i] = threadPool.Submit(() =>
            {
                long counter = 0;
                for (long k = 0; k < 100000 + j; k++)
                {
                    counter += k;
                }

                return counter;
            });
        }

        // threadPool.Shutdown();
        Assert.Pass();
        Func<long, string> func = l => l.ToString();
        
        var result = resultArray[9999].ContinueWith<string>(func).Result;
        
        for (long i = 0; i < 10000; i++)
        {
            Assert.AreEqual(4999950000 + (100000 + i - 1 + 100000)*i/2, resultArray[i].Result);
        }
    }
}