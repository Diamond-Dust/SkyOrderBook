using System.Diagnostics;

namespace SkyOrderBook
{
    public class OrderBookOrder : IEquatable<OrderBookOrder>
    {
        public long OrderId { get; set; }
        public OrderSide Side { get; set; }
        public int Price { get; set; }
        public int Qty { get; set; }
        public OrderBookOrder(OrderBookEntry entry)
        {
            OrderId = entry.OrderId;
            Side = entry.Side;
            Price = entry.Price;
            Qty = entry.Qty;
        }
        public bool Equals(OrderBookOrder? other)
        {
            return other is not null && other.OrderId == OrderId;
        }
        // Assume correct ID
        public void Update(OrderBookEntry other)
        {
            Side = other.Side;
            Price = other.Price;
            Qty = other.Qty;
        }
        public void Update(OrderBookOrder other)
        {
            Side = other.Side;
            Price = other.Price;
            Qty = other.Qty;
        }
    }

    public class OrderBookEntry
    {
        public string OriginalLine { get; set; }
        public string? SourceTime { get; set; }
        public OrderSide Side { get; set; }
        public OrderAction Action { get; set; }
        // The Clear action makes us not care about the OrderId, Price and Qty, so we can just assign it here.
        public long OrderId { get; set; }
        public int Price { get; set; }
        public int Qty { get; set; }
        public int? B0 { get; set; }
        public int? BQ0 { get; set; }
        public int? BN0 { get; set; }
        public int? A0 { get; set; }
        public int? AQ0 { get; set; }
        public int? AN0 { get; set; }

        private void assignFromLineQuick()
        {
            // Source time always has the same number of digits in this task and we do not care for its value save for re-saving it, so we can quicksplit it.: -0.07
            SourceTime = OriginalLine[0..11];
            // There is only one case without a Side
            Side = OriginalLine[12] == '1' ? OrderSide.BID : OriginalLine[12] == '2' ? OrderSide.ASK : OrderSide.NONE;
            int sideIndex = Side == OrderSide.NONE ? 13 : 14;
            Action = OriginalLine[sideIndex] switch
            {
                'Y' or 'F' => OrderAction.CLEAR,
                'A' => OrderAction.ADD,
                'M' => OrderAction.MODIFY,
                'D' => OrderAction.DELETE,
                _ => OrderAction.NONE,
            };
            // Clear action doesn't need any information
            if (Action == OrderAction.CLEAR)
            {
                return;
            }
            // OrderId has 14 digits except for the first line - but that's a Clear, so we do not need to worry about it
            OrderId = long.Parse(OriginalLine[(sideIndex + 2)..(sideIndex + 16)]);
            // Find the semicolon to split the reamining tick
            for (int i = sideIndex + 17; i < OriginalLine.Length; i++)
            {
                if (OriginalLine[i] == ';')
                {
                    Price = int.Parse(OriginalLine[(sideIndex + 17)..i]);
                    // Delete needs only Id and Price (Price only for the helper structure)
                    if (Action != OrderAction.DELETE)
                    {
                        Qty = int.Parse(OriginalLine[(i + 1)..]);
                    }
                    break;
                }
            }
        }

        public OrderBookEntry(string csvLine)
        {
            OriginalLine = csvLine;
            assignFromLineQuick();
        }

        public override string ToString()
        {
            return string.Format(
                    "{0};{1};{2};{3};{4};{5};{6}",
                    OriginalLine,
                    B0,
                    BQ0,
                    BN0,
                    A0,
                    AQ0,
                    AN0
                );
        }
    }
}