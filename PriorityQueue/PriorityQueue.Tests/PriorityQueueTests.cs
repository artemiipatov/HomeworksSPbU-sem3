namespace PriorityQueue.Tests;

using PriorityQueue;

public class Tests
{
    private PriorityQueue<int> _priorityQueue = new ();
    
    [SetUp]
    public void Setup()
    {
        _priorityQueue = new PriorityQueue<int>();
        var tasks = new Task[10];

        for (var i = 0; i < 10; i++)
        {
            var priority = i;
            tasks[i] = Task.Run(() => EnqueueValues(priority));
        }

        Task.WaitAll(tasks);
    }

    [Test]
    public void DequeueShouldReturnFirstAddedValue()
    {
        var actualValue = _priorityQueue.Dequeue();
        Assert.That(actualValue, Is.EqualTo(0));
    }

    [Test]
    public void TestNumberOfElements()
    {
        Assert.That(_priorityQueue.Size, Is.EqualTo(100));
    }

    [Test]
    public void DequeueShouldReturnAllElementsInCorrectOrder()
    {
        for (var i = 0; i < 10; i++)
        {
            for (var j = 0; j < 10; j++)
            {
                var actualValue = _priorityQueue.Dequeue();
                Assert.That(actualValue, Is.EqualTo(j));
            }
        }
    }

    [Test]
    public void ThreadsShouldWaitIfThereIsNoElementsInTheQueue()
    {
        for (var i = 0; i < 10; i++)
        {
            for (var j = 0; j < 10; j++)
            {
                var actualValue = _priorityQueue.Dequeue();
            }
        }
        
        var tasks = new Task[5];
        for (var taskNumber = 0; taskNumber < 5; taskNumber++)
        {
            tasks[taskNumber] = Task.Run(() => DequeueValueAndCompare(5));
        }

        for (var i = 0; i < 5; i++)
        {
            _priorityQueue.Enqueue(i, i);
        }

        Task.WaitAll(tasks);
        
        Assert.That(_priorityQueue.Size, Is.EqualTo(0));
    }

    private void DequeueValueAndCompare(int expectedValue)
    {
        var actualValue = _priorityQueue.Dequeue();
        Assert.That(actualValue, Is.LessThan(expectedValue));
    }
    
    private void EnqueueValues(int priority)
    {
        for (var value = 0; value < 10; value++)
        {
            _priorityQueue.Enqueue(value, priority);
        }
    }
}