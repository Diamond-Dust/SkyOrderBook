namespace SkyOrderBook
{
    // Knowing how many elements will have the same price tells us that we may need
    // a quick-retrieval structure inside a quick-retrieval structure.
    // Given that live-counting N and Q can be a bit faster, that may be reason enough to create a dedicated class.
    public class PriceInnerDictionary : Dictionary<long, OrderBookOrder>
    {
        public PriceInnerDictionary() : base() { }
        public PriceInnerDictionary(int capacity) : base(capacity) { }

        public int Q { get; private set; }
        public int N { get; private set; }
        public void Add(OrderBookOrder order)
        {
            Add(order.OrderId, order);
            N++;
            Q += order.Qty;
        }
        public void Add(OrderBookEntry entry)
        {
            OrderBookOrder order = new OrderBookOrder(entry);
            Add(order);
        }
        public bool Remove(OrderBookOrder order)
        {
            if (!ContainsKey(order.OrderId))
                return false;
            Remove(order.OrderId);
            N--;
            Q -= order.Qty;
            return true;
        }
        public bool Remove(OrderBookEntry entry)
        {
            if (!ContainsKey(entry.OrderId))
                return false;
            N--;
            Q -= this[entry.OrderId].Qty;
            Remove(entry.OrderId);
            return true;
        }
    }

    public class MultiSet
    {
        private SortedSet<int> _set;
        private Dictionary<int, int> _counter;
        public int Min { get { return _set.Min; } }
        public int Max { get { return _set.Max; } }

        public MultiSet()
        {
            _set = new SortedSet<int>();
            _counter = new Dictionary<int, int>();
        }

        public void Add(int key)
        {
            if (_set.Contains(key))
            {
                _counter[key]++;
            }
            else
            {
                _set.Add(key);
                _counter[key] = 1;
            }
        }
        public void Remove(int key)
        {
            if (_set.Contains(key))
            {
                _counter[key]--;
                if (_counter[key] == 0)
                {
                    _counter.Remove(key);
                    _set.Remove(key);
                }
            }
        }
        public void Clear()
        {
            _set.Clear();
            _counter.Clear();
        }
    }
}