using System.Diagnostics;
using System.Text;

namespace SkyOrderBook;

public class EntryPoint
{
    static void Main(string[] args)
    {
        // Very bare-bones parameter handling
        if (args.Length < 2)
        {
            Console.WriteLine("Please pass the input CSV path and the resultant csv path as parameters.");
            return;
        }
        string inputPath = Path.GetFullPath(args[0]), outputPath = Path.GetFullPath(args[1]);

        // ######### Step 1: Data input #########
        Stopwatch sw1 = new Stopwatch(), sw2 = new Stopwatch(), sw3 = new Stopwatch();
        sw1.Start();

        int ticksCount = 0;
        OrderBook orderBook = new OrderBook();
        using (StreamReader streamReader = new StreamReader(inputPath, Encoding.UTF8, true, 4096))
        {
            // Skip the header line
            streamReader.ReadLine();
            string? line;
            while ((line = streamReader.ReadLine()) != null)
            {
                OrderBookEntry entry = new OrderBookEntry(line);
                orderBook.Add(entry);
                ticksCount += 1;
            }
        }

        sw1.Stop();
        long ms1 = sw1.ElapsedMilliseconds, ts1 = sw1.ElapsedTicks;
        Console.WriteLine($"Data input time [us]: {ms1 * 1000.0:F3}");
        Console.WriteLine($"Data input time per tick [us]: {ms1 * 1000.0 / ticksCount:F3}");
        // ######### Step 1: Data input #########

        // ######### Step 2: OrderBook construction #########
        long ms2 = long.MaxValue, ts2 = long.MaxValue;
        for (int i = 0; i < 100; i++)
        {
            sw2.Start();

            orderBook.Construct();

            sw2.Stop();
            Console.WriteLine($" OrderBook creation time [us]: {sw2.ElapsedMilliseconds * 1000.0:F3}");
            Console.WriteLine($" OrderBook creation time per tick [us]: {sw2.ElapsedMilliseconds * 1000.0 / ticksCount:F3}");
            if (ms2 > sw2.ElapsedMilliseconds)
            {
                ms2 = sw2.ElapsedMilliseconds;
                ts2 = sw2.ElapsedTicks;
            }
            sw2.Reset();
        }

        Console.WriteLine($"Best OrderBook creation time [us]: {ms2 * 1000.0:F3}");
        Console.WriteLine($"Best OrderBook creation time per tick [us]: {ms2 * 1000.0 / ticksCount:F3}");
        // ######### Step 2: OrderBook construction #########

        // ######### Step 3: Result saving #########
        sw3.Start();

        using (StreamWriter streamWriter = new StreamWriter(outputPath))
        {
            orderBook.Save(streamWriter);
        }

        sw3.Stop();
        long ms3 = sw3.ElapsedMilliseconds, ts3 = sw3.ElapsedTicks;
        Console.WriteLine($"Result saving time [us]: {ms3 * 1000.0:F3}");
        Console.WriteLine($"Result saving time per tick [us]: {ms3 * 1000.0 / ticksCount:F3}");
        // ######### Step 3: Result saving #########

        long total_ms = ms1 + ms2 + ms3, total_ts = ts1 + ts2 + ts3;
        Console.WriteLine($"Best total time [us]: {total_ms * 1000.0:F3}");
        Console.WriteLine($"Best total time per tick [us]: {total_ms * 1000.0 / ticksCount:F3}");
    }
}