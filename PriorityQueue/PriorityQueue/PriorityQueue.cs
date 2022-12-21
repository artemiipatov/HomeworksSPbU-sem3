namespace PriorityQueue;

using System.Linq;

/// <summary>
/// Thread safe priority queue.
/// </summary>
/// <typeparam name="TValue">Type of queue elements.</typeparam>
public class PriorityQueue<TValue>
{
    private List<QueueElement<TValue>> _queueElementList = new ();

    /// <summary>
    /// Gets count of elements in the queue.
    /// </summary>
    public int Size => _queueElementList.Count;

    /// <summary>
    /// Enqueues value with given priority.
    /// </summary>
    /// <param name="value">Element value.</param>
    /// <param name="priority">Priority of the element.</param>
    public void Enqueue(TValue value, int priority)
    {
        lock (_queueElementList)
        {
            var newElement = new QueueElement<TValue>(value, priority);
            _queueElementList.Add(newElement);

            Monitor.PulseAll(_queueElementList);
        }
    }

    /// <summary>
    /// Gets element with the highest priority and removes it from the queue.
    /// </summary>
    /// <returns></returns>
    public TValue Dequeue()
    {
        lock (_queueElementList)
        {
            while (Size == 0)
            {
                Monitor.Wait(_queueElementList);
            }

            return GetAndRemove().Value;
        }
    }

    private QueueElement<TValue> GetAndRemove()
    {
        var maxElement = _queueElementList.Max() ?? throw new ArgumentNullException();
        _queueElementList.Remove(maxElement);
        return maxElement;
    }
    
    /// <summary>
    /// Class for elements of the queue.
    /// </summary>
    /// <typeparam name="TValue">Elements value type.</typeparam>
    private class QueueElement<TValue> : IComparable<QueueElement<TValue>>
    {
        /// <summary>
        /// Element priority.
        /// </summary>
        public int Priority { get; }

        /// <summary>
        /// Element value.
        /// </summary>
        public TValue Value { get; }
        
        /// <summary>
        /// Initializes a new instance of the <see cref="QueueElement"/> class.
        /// </summary>
        /// <param name="value"></param>
        /// <param name="priority"></param>
        public QueueElement(TValue value, int priority)
        {
            Priority = priority;
            Value = value;
        }

        public int CompareTo(QueueElement<TValue>? other)
        {
            return this.Priority.CompareTo((other ?? throw new ArgumentNullException()).Priority);
        }
    }
}