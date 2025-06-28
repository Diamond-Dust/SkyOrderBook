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

        public OrderBook()
        {
            _orderEntryList = new List<OrderBookEntry>();
            _orderById = new SortedDictionary<long, OrderBookOrder>();
            _orderAsksByPrice = new SortedDictionary<int, PriceInnerCounter>();
            _orderBidsByPrice = new SortedDictionary<int, PriceInnerCounter>();
            _askPrices = new MultiSet();
            _bidPrices = new MultiSet();
        }

        private void AddOrder(OrderBookEntry entry)
        {
            // Add to a specialised data structure
            int indexPrice = entry.Price;
            OrderBookOrder order = new OrderBookOrder(entry);
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

            if (_orderById.ContainsKey(entry.OrderId))
            {
                ModifyOrder(entry);
                return;
            }
            _orderById.Add(entry.OrderId, order);
            PriceInnerCounter? orderCounter;
            if (_orderByPrice.TryGetValue(indexPrice, out orderCounter))
            {
                orderCounter.Add(order.Qty);
            }
            else
            {
                orderCounter = new PriceInnerCounter();
                _orderByPrice.Add(indexPrice, orderCounter);
                orderCounter.Add(order.Qty);
            }
            _prices.Add(entry.Price);
        }

        private void ModifyOrder(OrderBookEntry entry)
        {
            int indexPrice = entry.Price;
            OrderBookOrder? preexistingOrder;
            PriceInnerCounter? orderCounter;
            SortedDictionary<int, PriceInnerCounter> _orderByPrice;
            MultiSet _prices;

            if (_orderById.TryGetValue(entry.OrderId, out preexistingOrder))
            {
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
                if (_orderByPrice.TryGetValue(preexistingOrder.Price, out orderCounter))
                {
                    orderCounter.Remove(preexistingOrder.Qty);
                    _prices.Remove(preexistingOrder.Price);
                    //We have emptied the Dictionary!
                    if (orderCounter.N == 0)
                    {
                        _orderByPrice.Remove(preexistingOrder.Price);
                    }
                }
                else
                {
                    { }
                    ;
                }

                // Add new information
                if (_orderByPrice.TryGetValue(entry.Price, out orderCounter))
                {
                    orderCounter.Add(entry.Qty);
                }
                else
                {
                    orderCounter = new PriceInnerCounter();
                    _orderByPrice.Add(indexPrice, orderCounter);
                    orderCounter.Add(entry.Qty);
                }

                _prices.Add(entry.Price);

                preexistingOrder.Update(entry);
            }
            else
            {
                AddOrder(entry);
                return;
            }
            return;
        }

        private void RemoveOrder(OrderBookEntry entry)
        {
            int indexPrice = entry.Price;
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
                // Remove out-of-date information
                if (_orderByPrice.TryGetValue(preexistingOrder.Price, out orderCounter))
                {
                    orderCounter.Remove(preexistingOrder.Qty);
                    _prices.Remove(preexistingOrder.Price);
                    //We have emptied the Dictionary!
                    if (orderCounter.N == 0)
                    {
                        _orderByPrice.Remove(preexistingOrder.Price);
                    }
                }
                else
                {
                    { }
                    ;
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
                // This Dict should be small enough that the LINQ's .First() is not a huge time sink.
                // Performance testing seems to corroborate that.
                entry.A0 = _askPrices.Min;
                entry.AQ0 = _orderAsksByPrice[_askPrices.Min].Q;
                entry.AN0 = _orderAsksByPrice[_askPrices.Min].N;
            }

            if (_orderBidsByPrice.Any())
            {
                // This Dict should be small enough that the LINQ's .First() is not a huge time sink.
                // Performance testing seems to corroborate that.
                entry.B0 = _bidPrices.Max;
                entry.BQ0 = _orderBidsByPrice[_bidPrices.Max].Q;
                entry.BN0 = _orderBidsByPrice[_bidPrices.Max].N;
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