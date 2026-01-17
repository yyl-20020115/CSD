using CSD;
using System.Text;

namespace PatternGenerator;

public static class PatternGenerator
{
    public static void Main(string[] args)
    {
        switch (args.Length)
        {
            case 0:
                Console.WriteLine("PG <Input> <Output>");
                break;
            case 1:
                if (File.Exists(args[0]))
                {
                    Analyse(File.ReadAllBytes(args[0]), Console.Out);
                }
                break;
            case >= 2:
                {
                    if (File.Exists(args[0]) && File.Exists(args[1]))
                    {
                        using var writer = new StreamWriter(args[1]);
                        Analyse(File.ReadAllBytes(args[0]), writer);
                    }
                    break;
                }
        }
    }

    public static void Analyse(byte[] data,TextWriter writer)
    {
        Analyse(data, new ReversibleStream(data),writer);
    }

    public static void Analyse(byte[] data, ReversibleStream input,TextWriter writer)
    {
        var mode = 32;
        var index = 0;
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
            if (instruction.OpCode == ("invalid"))
            {
                index = Advance(index, data);
                if (index == -1)
                    break;
                continue;
            }
            var pattern = new StringBuilder();
            for (int c = 0; c < instruction.Length; c++)
                pattern.Append($"{data[c] & 0xFF:X2}");
            // mask out with DD and II
            var disam = instruction.ToString();
            foreach (var op in instruction.Operand)
            {
                if (op.Type == null)
                {
                    disam = "Unknown";
                }
                if (op.Type == ("OP_IMM") || op.Type == ("OP_JIMM"))
                {
                    for (int c = op.ImmStart; c < op.ImmStart + op.Size / 8; c++)
                    {
                        pattern[2 * c + 0] = 'I';
                        pattern[2 * c + 1] = 'I';
                    }
                }
                else if (op.Type == ("OP_MEM"))
                {
                    if (op.Offset > 0)
                    {
                        for (int c = op.DisStart; c < op.DisStart + op.Offset / 8; c++)
                        {
                            pattern[2 * c + 0] = 'D';
                            pattern[2 * c + 1] = 'D';
                        }
                    }
                }
                else if (op.Type == ("OP_PTR"))
                {
                    if (op.Size == 32)
                    {
                        for (int c = op.DisStart; c < op.DisStart + 2; c++)
                        {
                            pattern[2 * c + 0] = 'D';
                            pattern[2 * c + 1] = 'D';
                        }
                        for (int c = op.DisStart + 2; c < op.DisStart + 4; c++)
                        {
                            pattern[2 * c + 0] = 'S';
                            pattern[2 * c + 1] = 'S';
                        }
                    }
                    else if (op.Size == 48)
                    {
                        for (int c = op.DisStart; c < op.DisStart + 4; c++)
                        {
                            pattern[2 * c + 0] = 'D';
                            pattern[2 * c + 1] = 'D';
                        }
                        for (int c = op.DisStart + 4; c < op.DisStart + 6; c++)
                        {
                            pattern[2 * c + 0] = 'S';
                            pattern[2 * c + 1] = 'S';
                        }
                    }
                }
            }
            writer.WriteLine($"{pattern} {disam}");
            // find last byte that is not II SS or DD and increment it, zeroing above it
            index = LastOpcodeByteBefore(pattern.ToString(), pattern.Length - 1);
            index = Advance(index, data);
            if (index == -1)
                break;
        }
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

    public static int LastOpcodeByteBefore(string pat, int index)
    {
        while ((index > 0) && ((pat[(index)] == 'I')
            || (pat[(index)] == 'D') || (pat[(index)] == 'S')))
            index--;
        return index >> 1;
    }
}
