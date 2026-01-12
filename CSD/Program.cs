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
            if (File.Exists(args[0]) && File.Exists(args[1]))
            {
                var data = File.ReadAllBytes(args[0]);
                var input = new ReversibleStream(data);
                int mode = 16;
                int index = 0;
                using var writer = new StreamWriter(args[1]);
                while (true)
                {
                    Instruction instruction;
                    try
                    {
                        instruction = Dissassembler.Decode(input, mode);
                    }
                    catch (InvalidOperationException e)
                    {
                        index = Advance(index, data);
                        if (index == -1)
                            break;
                        continue;
                    }
                    catch (IndexOutOfRangeException e)
                    {
                        --index;
                        if ((index == 0) && (data[0] == 0xff))
                            break;
                        ++data[index];
                        for (int i = index + 1; i < data.Length; i++)
                            data[i] = 0;
                        continue;
                    }
                    if (instruction.op == ("invalid"))
                    {
                        index = Advance(index, data);
                        if (index == -1)
                            break;
                        continue;
                    }
                    writer.WriteLine(instruction.ToString());

                }
                return 0;
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
