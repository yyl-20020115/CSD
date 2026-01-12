using System.Text;

namespace CSD;

public class Operand
{
    public long x86Length;
    public Instruction? parent;
    public long eip;
    public String? seg;
    public String? type, _base;
    public int size;
    public long lval;
    public Pointer? ptr; // should be same as lval somehow
    public String? index;
    public long offset, scale;
    public int cast;
    //long pc;
    //value;
    //ref;
    // required for patterns
    public int maxSize;
    public int imm_start, dis_start;

    public override string ToString() => ToString(false);
    public  string ToString(bool pattern = false)
    {
        if (type == null)
            return "UNKNOWN - AVX?";
        if (type==("OP_REG"))
            return _base;
        bool first = true;
        var builder = new StringBuilder();
        //if (cast == 1)
        //    b.append(intel_size(size));
        if (type == ("OP_MEM"))
        {
            builder.Append(Instruction.IntelModeSize(size));
            if (seg != null)
                builder.Append(seg + ":");
            else if ((_base == null) && (index == null))
                builder.Append("ds:");
            if ((_base != null) || (index != null))
                builder.Append('[');

            if (_base != null)
            {
                builder.Append(_base);
                first = false;
            }
            if (index != null)
            {
                if (!first)
                    builder.Append('+');
                builder.Append(index);
                first = false;
            }
            if (scale != 0)
                builder.Append("*" + scale);
            if ((offset == 8) || (offset == 16) || (offset == 32) || (offset == 64))
            {
                if (!pattern)
                {
                    if ((lval < 0) && ((_base != null) || (index != null)))
                        builder.Append($"-0x{-lval:X}");
                    else
                    {
                        if (!first)
                            builder.Append($"+0x{lval & ((1L << (int)offset) - 1):X}");
                        else
                            builder.Append($"0x{lval & ((1L << (int)offset) - 1):X}");
                    }
                }
                else
                {
                    builder.Append('$');
                    for (int i = 0; i < offset / 8; i++)
                        builder.Append("DD");
                }
            }
            if ((_base != null) || (index != null))
                builder.Append(']');
        }
        else if (type==("OP_IMM"))
        {
            if (!pattern)
            {
                if (lval < 0)
                {
                    if (Instruction.SignExtends.Contains(parent.opcode)) // these are sign extended
                        builder.Append($"0x{lval & ((1L << maxSize) - 1):X}");
                    else
                        builder.Append($"0x{lval & ((1L << size) - 1):X}");
                }
                else
                    builder.Append($"0x{lval:X}");
            }
            else
            {
                builder.Append("$");
                for (int i = 0; i < size / 8; i++)
                    builder.Append("II");
            }
        }
        else if (type==("OP_JIMM"))
        {
            if (!pattern)
            {
                if (eip + x86Length + lval < 0)
                    builder.Append($"0x{(eip + x86Length + lval) & ((1L << maxSize) - 1):X}");
                else
                    builder.Append($"0x{eip + x86Length + lval:X}");
            }
            else
            {
                builder.Append('$');
                for (int i = 0; i < size / 8; i++)
                    builder.Append("II");
            }
        }
        else if (type==("OP_PTR"))
        {
            if (!pattern)
                builder.Append($"0x{{0:X4}}:0x{{1:X}}");
            else
                builder.Append("$SSSS:$DDDDDDDD");
        }
        return builder.ToString();
        //return String.format("[%s %s %s %d %x %x %x]", type, @base, index, size, lval, offset, scale);
    }
}