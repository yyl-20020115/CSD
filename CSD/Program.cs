namespace CSD;

public static class Program
{
    public static int Main(string[] args)
    {
        if (args.Length == 0)
        {
            var instruction = Dissassembler.Decode(new([5]), 32);
            Console.WriteLine(instruction);
            return 0;
        }
        else
        {
            int size = 32;
            if (args[0] == ("-rm"))
            {
                size = 16;
                args = args[1..];
            }
            var data = new byte[args.Length];
            for (int i = 0; i < args.Length; i++)
                data[i] = (byte)(byte.TryParse(args[i], System.Globalization.NumberStyles.HexNumber, null, out var v) ? v : 0);
            Console.Write("Raw bytes: ");
            for (int i = 0; i < args.Length; i++)
                Console.Write("%2x ", data[i]);
            Console.WriteLine();
            var instruction = Dissassembler.Decode(new(data), size);
            Console.WriteLine(instruction);
            return 0;
        }
    }

}
