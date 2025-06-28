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
        public long SourceTime { get; set; }
        public OrderSide Side { get; set; }
        public OrderAction Action { get; set; }
        public long OrderId { get; set; }
        public int Price { get; set; }
        public int Qty { get; set; }
        public int? B0 { get; set; }
        public int? BQ0 { get; set; }
        public int? BN0 { get; set; }
        public int? A0 { get; set; }
        public int? AQ0 { get; set; }
        public int? AN0 { get; set; }

        private void AssignAtIndexN(int n, string segment)
        {
            switch (n)
            {
                case 0:
                    SourceTime = long.Parse(segment);
                    break;
                case 1:
                    Side = string.IsNullOrEmpty(segment) ? OrderSide.NONE : (OrderSide)int.Parse(segment);
                    break;
                case 2:
                    Action = segment switch
                    {
                        "Y" or "F" => OrderAction.CLEAR,
                        "A" => OrderAction.ADD,
                        "M" => OrderAction.MODIFY,
                        "D" => OrderAction.DELETE,
                        _ => OrderAction.NONE,
                    };
                    break;
                case 3:
                    OrderId = long.Parse(segment);
                    break;
                case 4:
                    Price = int.Parse(segment);
                    break;
                case 5:
                    Qty = int.Parse(segment);
                    break;
                // We do not expect anything else, so drop it
                default:
                    break;
            }
        }

        private void assignFromLine(string csvLine)
        {
            // We do not expect quotes, escaped quotes or anything of that sort, so we can simplify
            int lastIndex = 0, lastFieldIndex = 0;
            string segment;
            for (int i = 0; i < csvLine.Length; i++)
            {
                if (csvLine[i] == ';')
                {
                    if (lastIndex >= i)
                    {
                        lastIndex = i + 1;
                        lastFieldIndex++;
                        continue;
                    }
                    segment = csvLine[lastIndex..i];
                    AssignAtIndexN(lastFieldIndex, segment);
                    lastIndex = i + 1;
                    lastFieldIndex++;
                }
            }
            segment = csvLine[lastIndex..csvLine.Length];
            AssignAtIndexN(lastFieldIndex, segment);
        }

        public OrderBookEntry(string csvLine)
        {
            assignFromLine(csvLine);
        }

        public override string ToString()
        {
            return string.Format(
                "{0};{1};{2};{3};{4};{5};{6};{7};{8};{9};{10};{11}",
                SourceTime,
                Side switch
                {
                    OrderSide.BID or OrderSide.ASK => (int)Side,
                    _ => "",
                },
                Action switch
                {
                    OrderAction.CLEAR => "Y",
                    OrderAction.ADD => "A",
                    OrderAction.MODIFY => "M",
                    OrderAction.DELETE => "D",
                    _ => "",
                },
                OrderId,
                Price,
                Qty,
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