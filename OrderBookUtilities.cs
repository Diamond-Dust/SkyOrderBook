namespace SkyOrderBook
{
    public class Counter(int n, int q)
    {
        public int Q { get; set; } = q;
        public int N { get; set; } = n;
    }

    // Leaner version of SortedDictionary - the Sorted property is granted via the SortedSet
    // (like in the SortedDictionary), while O(1) access is provided via a Dictionary.
    // Min and Max are exposed.
    public class MultiSetCounter
    {
        private SortedSet<int> _set;
        private Dictionary<int, Counter> _counter;
        public int Min { get { return _set.Min; } }
        public int Max { get { return _set.Max; } }
        public int Count { get { return _set.Count; } }

        public MultiSetCounter()
        {
            _set = new SortedSet<int>();
            _counter = new Dictionary<int, Counter>();
        }

        public void Add(int key, int quantity)
        {
            Counter? counter;
            if (_counter.TryGetValue(key, out counter))
            {
                counter.N++;
                counter.Q += quantity;
            }
            else
            {
                _set.Add(key);
                _counter[key] = new Counter(1, quantity);
            }
        }
        public void Remove(int key, int quantity)
        {
            Counter? counter;
            if (_counter.TryGetValue(key, out counter))
            {
                counter.N--;
                if (counter.N == 0)
                {
                    _counter.Remove(key);
                    _set.Remove(key);
                }
                else
                {
                    counter.Q -= quantity;
                }
            }
        }
        //It's your fault if you update a nonexistant entry, we're aiming for speed here!
        public void Update(int key, int diff)
        {
            _counter[key].Q += diff;
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

        public Counter GetCounter(int key)
        {
            return _counter[key];
        }
    }
}