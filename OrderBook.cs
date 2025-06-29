using System.ComponentModel;
using System.Runtime.InteropServices;

namespace SkyOrderBook
{
    public class OrderBook
    {
        private List<OrderBookEntry> _orderEntryList;
        private Dictionary<long, OrderBookOrder> _orderById;
        // Did you know that even though SortedDictionary is built upon SortedSet,
        // which is built upon a binary search tree, the only way to get
        // First/Last element is to call e.g. LINQ's .Last(), which
        // WILL create an IEnumerable and iterate through it one by one!
        // SortedDictionary does not expose the SortedSet's Max and Min properties.
        // This problem has been reported in 2016 and it still hasn't been fixed.
        // The easiest way to circumvent this is by using negative price values as keys.
        // However, the First() method is still slow, so a redundant SortedSet multi set will do.
        // This practically doubles our SortedDictionary just for quick access to the first and last elements.
        private MultiSetCounter _askPrices;
        private MultiSetCounter _bidPrices;

        private bool _askStale;
        private bool _bidStale;
        private int? _cacheB0;
        private int? _cacheBQ0;
        private int? _cacheBN0;
        private int? _cacheA0;
        private int? _cacheAQ0;
        private int? _cacheAN0;

        public OrderBook()
        {
            _orderEntryList = new List<OrderBookEntry>();
            _orderById = new Dictionary<long, OrderBookOrder>(70000);
            _askPrices = new MultiSetCounter();
            _bidPrices = new MultiSetCounter();
            _askStale = true;
            _bidStale = true;
        }

        private void AddOrder(OrderBookEntry entry)
        {
            // Add to a specialised data structure
            OrderBookOrder order = new OrderBookOrder(entry);
            MultiSetCounter _prices;
            switch (entry.Side)
            {
                case OrderSide.ASK:
                    _prices = _askPrices;
                    // Conservative cache - if you can beat ASK with the best price, we assume that the result changed
                    _askStale |= entry.Price <= _cacheA0;
                    break;
                case OrderSide.BID:
                    _prices = _bidPrices;
                    // Conservative cache - if you can beat BID with the best price, we assume that the result changed
                    _bidStale |= entry.Price >= _cacheB0;
                    break;
                default:
                    return;
            }

            // There never is any Add on an existing Id
            _orderById.Add(entry.OrderId, order);
            _prices.Add(order.Price, order.Qty);
        }

        private void ModifyOrder(OrderBookEntry entry)
        {
            OrderBookOrder? preexistingOrder;
            MultiSetCounter _prices;

            // There never is any Modify on an unexisting Id
            preexistingOrder = _orderById[entry.OrderId];
            switch (preexistingOrder.Side)
            {
                case OrderSide.ASK:
                    _prices = _askPrices;
                    break;
                case OrderSide.BID:
                    _prices = _bidPrices;
                    break;
                default:
                    return;
            }
            // Conservative cache - if you touch Order with the best price, we assume that the result changed
            if (preexistingOrder.Side == OrderSide.ASK)
            {
                _askStale |= (preexistingOrder.Price == _cacheA0) || (entry.Price <= _cacheA0);
            }
            else
            {
                _bidStale |= (preexistingOrder.Price == _cacheB0) || (entry.Price >= _cacheB0);
            }

            // Only Qty is changing
            if (preexistingOrder.Price == entry.Price)
            {
                _prices.Update(preexistingOrder.Price, entry.Qty - preexistingOrder.Qty);
                preexistingOrder.Update(entry);
                return;
            }

            // Remove out-of-date information

            // Remove old information
            _prices.Remove(preexistingOrder.Price, preexistingOrder.Qty);

            // Add new information
            _prices.Add(entry.Price, entry.Qty);

            preexistingOrder.Update(entry);
        }

        private void RemoveOrder(OrderBookEntry entry)
        {
            MultiSetCounter _prices;
            switch (entry.Side)
            {
                case OrderSide.ASK:
                    _prices = _askPrices;
                    break;
                case OrderSide.BID:
                    _prices = _bidPrices;
                    break;
                default:
                    return;
            }

            // Remove from a specialised data structure
            OrderBookOrder? preexistingOrder;
            if (_orderById.TryGetValue(entry.OrderId, out preexistingOrder))
            {
                // Conservative cache - if you touch Order with the best price, we assume that the result changed
                if (preexistingOrder.Side == OrderSide.ASK)
                {
                    _askStale |= preexistingOrder.Price == _cacheA0;
                }
                else
                {
                    _bidStale |= preexistingOrder.Price == _cacheB0;
                }

                // Remove out-of-date information
                _prices.Remove(preexistingOrder.Price, preexistingOrder.Qty);

                _orderById.Remove(entry.OrderId);
            }
        }

        private void calculateMissing(OrderBookEntry entry)
        {
            if (_orderById.Count == 0)
                return;

            // O(1) without LINQ's IEnumerable troubles
            if (_askPrices.Count > 0)
            {
                // Why go through the tree if need not be?
                if (_askStale)
                {
                    int min = _askPrices.Min;
                    Counter cmin = _askPrices.GetCounter(min);
                    _cacheA0 = min;
                    _cacheAQ0 = cmin.Q;
                    _cacheAN0 = cmin.N;
                    _askStale = false;
                }
                // Performance testing seems to corroborate that.
                // SortedSet does not have internal stale caching, so we can do this here
                entry.A0 = _cacheA0;
                entry.AQ0 = _cacheAQ0;
                entry.AN0 = _cacheAN0;
            }

            // O(1) without LINQ's IEnumerable troubles
            if (_bidPrices.Count > 0)
            {
                // Why go through the tree if need not be?
                if (_bidStale)
                {
                    int max = _bidPrices.Max;
                    Counter cmax = _bidPrices.GetCounter(max);
                    _cacheB0 = max;
                    _cacheBQ0 = cmax.Q;
                    _cacheBN0 = cmax.N;
                    _bidStale = false;
                }
                // Performance testing seems to corroborate that.
                // SortedSet does not have internal stale caching, so we can do this here
                entry.B0 = _cacheB0;
                entry.BQ0 = _cacheBQ0;
                entry.BN0 = _cacheBN0;
            }
        }

        public void Add(OrderBookEntry entry)
        {
            // Add to entry list - history
            _orderEntryList.Add(entry);
        }

        private void ClearOrders()
        {
            _orderById.Clear();
            _askPrices.Clear();
            _bidPrices.Clear();
        }

        public void Construct()
        {
            _askStale = true;
            _bidStale = true;
            ClearOrders();

            foreach (OrderBookEntry entry in CollectionsMarshal.AsSpan(_orderEntryList))
            {
                // Perform action
                switch (entry.Action)
                {
                    case OrderAction.CLEAR:
                        ClearOrders();
                        break;
                    case OrderAction.ADD:
                        AddOrder(entry);
                        break;
                    case OrderAction.MODIFY:
                        ModifyOrder(entry);
                        break;
                    case OrderAction.DELETE:
                        RemoveOrder(entry);
                        break;
                }

                // Calculate missing values
                calculateMissing(entry);
            }
        }

        public void Save(StreamWriter sw)
        {
            sw.WriteLine("SourceTime;Side;Action;OrderId;Price;Qty;B0;BQ0;BN0;A0;AQ0;AN0");
            foreach (OrderBookEntry entry in CollectionsMarshal.AsSpan(_orderEntryList))
            {
                sw.WriteLine(entry.ToString());
            }
        }
    }
}