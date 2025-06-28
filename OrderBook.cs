using System.ComponentModel;

namespace SkyOrderBook
{
    public class OrderBook
    {
        private List<OrderBookEntry> _orderEntryList;
        private SortedDictionary<long, OrderBookOrder> _orderById;
        private SortedDictionary<int, PriceInnerCounter> _orderAsksByPrice;
        private SortedDictionary<int, PriceInnerCounter> _orderBidsByPrice;
        // Did you know that even though SortedDictionary is built upon SortedSet,
        // which is built upon a binary search tree, the only way to get
        // First/Last element is to call e.g. LINQ's .Last(), which
        // WILL create an IEnumerable and iterate through it one by one!
        // SortedDictionary does not expose the SortedSet's Max and Min properties.
        // This problem has been reported in 2016 and it still hasn't been fixed.
        // The easiest way to circumvent this is by using negative price values as keys.
        // However, the First() method is still slow, so a redundant SortedSet multi set will do.
        // This practically doubles our SortedDictionary just for quick access to the first and last elements.
        private MultiSet _askPrices;
        private MultiSet _bidPrices;

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
            _orderById = new SortedDictionary<long, OrderBookOrder>();
            _orderAsksByPrice = new SortedDictionary<int, PriceInnerCounter>();
            _orderBidsByPrice = new SortedDictionary<int, PriceInnerCounter>();
            _askPrices = new MultiSet();
            _bidPrices = new MultiSet();
            _askStale = true;
            _bidStale = true;
        }

        private void AddOrder(OrderBookEntry entry)
        {
            // Add to a specialised data structure
            OrderBookOrder order = new OrderBookOrder(entry);
            SortedDictionary<int, PriceInnerCounter> _orderByPrice;
            MultiSet _prices;
            switch (entry.Side)
            {
                case OrderSide.ASK:
                    _orderByPrice = _orderAsksByPrice;
                    _prices = _askPrices;
                    // Conservative cache - if you can beat ASK with the best price, we assume that the result changed
                    _askStale = entry.Price <= _cacheA0;
                    break;
                case OrderSide.BID:
                    _orderByPrice = _orderBidsByPrice;
                    _prices = _bidPrices;
                    // Conservative cache - if you can beat BID with the best price, we assume that the result changed
                    _bidStale = entry.Price >= _cacheB0;
                    break;
                default:
                    return;
            }

            // There never is any Add on an existing Id
            _orderById.Add(entry.OrderId, order);
            PriceInnerCounter? orderCounter;
            if (_orderByPrice.TryGetValue(entry.Price, out orderCounter))
            {
                orderCounter.Add(order.Qty);
            }
            else
            {
                orderCounter = new PriceInnerCounter();
                _orderByPrice.Add(entry.Price, orderCounter);
                orderCounter.Add(order.Qty);
            }
            _prices.Add(entry.Price);
        }

        private void ModifyOrder(OrderBookEntry entry)
        {
            OrderBookOrder? preexistingOrder;
            PriceInnerCounter? orderCounter;
            SortedDictionary<int, PriceInnerCounter> _orderByPrice;
            MultiSet _prices;

            // There never is any Modify on an unexisting Id
            preexistingOrder = _orderById[entry.OrderId];
            switch (preexistingOrder.Side)
            {
                case OrderSide.ASK:
                    _orderByPrice = _orderAsksByPrice;
                    _prices = _askPrices;
                    break;
                case OrderSide.BID:
                    _orderByPrice = _orderBidsByPrice;
                    _prices = _bidPrices;
                    break;
                default:
                    return;
            }

            // Remove out-of-date information
            // Conservative cache - if you touch Order with the best price, we assume that the result changed
            if (preexistingOrder.Side == OrderSide.ASK)
            {
                _askStale = ((preexistingOrder.Price == _cacheA0) || (entry.Price <= _cacheA0)) && (preexistingOrder.Price != entry.Price);
            }
            else
            {
                _bidStale = ((preexistingOrder.Price == _cacheB0) || (_bidStale = entry.Price >= _cacheB0)) && (preexistingOrder.Price != entry.Price);
            }
            // No TryGetValue, we never should have no such record here
            orderCounter = _orderByPrice[preexistingOrder.Price];
            orderCounter.Remove(preexistingOrder.Qty);
            _prices.Remove(preexistingOrder.Price);
            //We have emptied the Dictionary!
            if (orderCounter.N == 0)
            {
                _orderByPrice.Remove(preexistingOrder.Price);
            }

            // Add new information
            if (_orderByPrice.TryGetValue(entry.Price, out orderCounter))
            {
                orderCounter.Add(entry.Qty);
            }
            else
            {
                orderCounter = new PriceInnerCounter();
                _orderByPrice.Add(entry.Price, orderCounter);
                orderCounter.Add(entry.Qty);
            }

            _prices.Add(entry.Price);

            preexistingOrder.Update(entry);
        }

        private void RemoveOrder(OrderBookEntry entry)
        {
            SortedDictionary<int, PriceInnerCounter> _orderByPrice;
            MultiSet _prices;
            switch (entry.Side)
            {
                case OrderSide.ASK:
                    _orderByPrice = _orderAsksByPrice;
                    _prices = _askPrices;
                    break;
                case OrderSide.BID:
                    _orderByPrice = _orderBidsByPrice;
                    _prices = _bidPrices;
                    break;
                default:
                    return;
            }

            // Remove from a specialised data structure
            OrderBookOrder? preexistingOrder;
            PriceInnerCounter? orderCounter;
            if (_orderById.TryGetValue(entry.OrderId, out preexistingOrder))
            {
                // Conservative cache - if you touch Order with the best price, we assume that the result changed
                if (preexistingOrder.Side == OrderSide.ASK)
                {
                    _askStale = preexistingOrder.Price == _cacheA0;
                }
                else
                {
                    _bidStale = preexistingOrder.Price == _cacheB0;
                }

                // Remove out-of-date information
                // No TryGetValue, we never should have no such record here
                orderCounter = _orderByPrice[preexistingOrder.Price];
                orderCounter.Remove(preexistingOrder.Qty);
                _prices.Remove(preexistingOrder.Price);
                //We have emptied the Dictionary!
                if (orderCounter.N == 0)
                {
                    _orderByPrice.Remove(preexistingOrder.Price);
                }
                _orderById.Remove(entry.OrderId);
            }
        }

        private void calculateMissing(OrderBookEntry entry)
        {
            if (_orderById.Count == 0)
                return;

            if (_orderAsksByPrice.Any())
            {
                // Why go through the tree if need not be?
                if (_askStale)
                {
                    int min = _askPrices.Min;
                    PriceInnerCounter pmin = _orderAsksByPrice[min];
                    _cacheA0 = min;
                    _cacheAQ0 = pmin.Q;
                    _cacheAN0 = pmin.N;
                    _askStale = false;
                }
                // Performance testing seems to corroborate that.
                // SortedSet does not have internal stale caching, so we can do this here
                entry.A0 = _cacheA0;
                entry.AQ0 = _cacheAQ0;
                entry.AN0 = _cacheAN0;
            }

            if (_orderBidsByPrice.Any())
            {
                // Why go through the tree if need not be?
                if (_bidStale)
                {
                    int max = _bidPrices.Max;
                    PriceInnerCounter pmax = _orderBidsByPrice[max];
                    _cacheB0 = max;
                    _cacheBQ0 = pmax.Q;
                    _cacheBN0 = pmax.N;
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
            _orderAsksByPrice.Clear();
            _orderBidsByPrice.Clear();
            _askPrices.Clear();
            _bidPrices.Clear();
        }

        public void Construct()
        {
            foreach (OrderBookEntry entry in _orderEntryList)
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
            foreach (OrderBookEntry entry in _orderEntryList)
            {
                sw.WriteLine(entry.ToString());
            }
        }
    }
}