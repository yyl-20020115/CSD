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
                    var offset = input.Index;
                    try
                    {
                        var instruction = Dissassembler.Decode(input, mode);
                        if (instruction.OpCode == ("invalid"))
                        {
                            break;
                        }
                        writer.WriteLine($"{offset:X8}\t{instruction.ToString()}");

                        if (instruction?.Template?.OpCode == "jmp")
                        {
                            var target = instruction.Operand[0].Lval + instruction.Length;
                            if (target > input.Index)
                            {
                                for(; input.Index < target; input.Index++)
                                {
                                    writer.WriteLine($"{input.Index:X8}\tdb {data[input.Index]:X02}");
                                }
                            }
                        }

                        if (input.Index == data.Length)
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
}
