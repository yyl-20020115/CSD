using System.Text;

namespace CSD;

public class Operand
{
    public long Length;
    public Instruction? Parent;
    public long EIP;
    public string? Segment;
    public string? Type, Base;
    public int Size;
    public long Lval;
    public Pointer? Pointer; // should be same as lval somehow
    public string? Index;
    public long Offset, Scale;
    public int Cast;
    //long pc;
    //value;
    //ref;
    // required for patterns
    public int MaxSize;
    public int ImmStart, DisStart;

    public override string ToString() => ToString(false) ?? "";
    public string? ToString(bool pattern = false)
    {
        if (Type == null)
            return "UNKNOWN - AVX?";
        if (Type == ("OP_REG"))
            return Base;
        bool first = true;
        var builder = new StringBuilder();
        //if (cast == 1)
        //    b.append(intel_size(size));
        if (Type == ("OP_MEM"))
        {
            builder.Append(Instruction.IntelModeSize(Size));
            if (Segment != null)
                builder.Append(Segment + ":");
            else if ((Base == null) && (Index == null))
                builder.Append("ds:");
            if ((Base != null) || (Index != null))
                builder.Append('[');

            if (Base != null)
            {
                builder.Append(Base);
                first = false;
            }
            if (Index != null)
            {
                if (!first)
                    builder.Append('+');
                builder.Append(Index);
                first = false;
            }
            if (Scale != 0)
                builder.Append("*" + Scale);
            if ((Offset == 8) || (Offset == 16) || (Offset == 32) || (Offset == 64))
            {
                if (!pattern)
                {
                    if ((Lval < 0) && ((Base != null) || (Index != null)))
                        builder.Append($"-0x{-Lval:X}");
                    else
                    {
                        if (!first)
                            builder.Append($"+0x{Lval & ((1L << (int)Offset) - 1):X}");
                        else
                            builder.Append($"0x{Lval & ((1L << (int)Offset) - 1):X}");
                    }
                }
                else
                {
                    builder.Append('$');
                    for (int i = 0; i < Offset / 8; i++)
                        builder.Append("DD");
                }
            }
            if ((Base != null) || (Index != null))
                builder.Append(']');
        }
        else if (Type == ("OP_IMM"))
        {
            if (!pattern)
            {
                if (Lval < 0)
                {
                    if (Instruction.SignExtends.Contains(Parent.OpCode)) // these are sign extended
                        builder.Append($"0x{Lval & ((1L << MaxSize) - 1):X}");
                    else
                        builder.Append($"0x{Lval & ((1L << Size) - 1):X}");
                }
                else
                    builder.Append($"0x{Lval:X}");
            }
            else
            {
                builder.Append('$');
                for (int i = 0; i < Size / 8; i++)
                    builder.Append("II");
            }
        }
        else if (Type == ("OP_JIMM"))
        {
            if (!pattern)
            {
                if (EIP + Length + Lval < 0)
                    builder.Append($"0x{(EIP + Length + Lval) & ((1L << MaxSize) - 1):X}");
                else
                    builder.Append($"0x{EIP + Length + Lval:X}");
            }
            else
            {
                builder.Append('$');
                for (int i = 0; i < Size / 8; i++)
                    builder.Append("II");
            }
        }
        else if (Type == ("OP_PTR"))
        {
            if (!pattern)
                builder.Append($"0x{{0:X4}}:0x{{1:X}}");
            else
                builder.Append("$SSSS:$DDDDDDDD");
        }
        return builder.ToString();
    }
}
