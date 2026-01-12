namespace CSD;

public static class Program
{
    public static int Main(string[] args)
    {
        if (args.Length == 0)
        {
            Console.WriteLine("CSD <Input> <Output>");
            return 0;
        }
        else if (args.Length == 2)
        {
            if (File.Exists(args[0]))
            {
                var data = File.ReadAllBytes(args[0]);
                var input = new ReversibleStream(data);
                int mode = 16;
                using var writer = new StreamWriter(args[1]);
                while (true)
                {
                    var offset = input.index;
                    try
                    {
                        var instruction = Dissassembler.Decode(input, mode);
                        if (instruction.opcode == ("invalid"))
                        {
                            break;
                        }
                        writer.WriteLine($"{offset:X8}\t{instruction.ToString()}");

                        if (instruction?.template?.opcode == "jmp")
                        {
                            var target = instruction.operand[0].lval + instruction.Length;
                            if (target > input.index)
                            {
                                for(; input.index<target; input.index++)
                                {
                                    writer.WriteLine($"{input.index:X8}\tdb {data[input.index]:X02}");
                                }
                            }
                        }

                        if (input.index == data.Length)
                            break;
                    }
                    catch (Exception ex)
                    {
                        break;
                    }
                }
            }
        }
        return 0;
    }
    public static int Advance(int index, byte[] data)
    {
        while ((index > 0) && !((data[index] & 0xFF) < 255))
            index--;
        if ((index == 0) && (data[0] == 0xff))
            return -1;
        data[index]++;
        for (int i = index + 1; i < data.Length; i++)
            data[i] = 0;
        return index;
    }
    
}
