namespace SkyOrderBook
{
    // Knowing how many elements will have the same price tells us that we may need
    // a quick-retrieval structure inside a quick-retrieval structure.
    // Given that live-counting N and Q can be a bit faster, that may be reason enough to create a dedicated class.
    public class PriceInnerCounter
    {
        public PriceInnerCounter() : base() { }

        public int Q { get; private set; }
        public int N { get; private set; }
        public void Add(int quantity)
        {
            N++;
            Q += quantity;
        }
        public void Remove(int quantity)
        {
            N--;
            Q -= quantity;
        }
    }

    public class MultiSet
    {
        private SortedSet<int> _set;
        private Dictionary<int, int> _counter;
        public int Min { get { return _set.Min; } }
        public int Max { get { return _set.Max; } }
        public int Count { get { return _set.Count; } }

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

        public bool Contains(int key)
        {
            return _counter.ContainsKey(key);
        }
    }
}