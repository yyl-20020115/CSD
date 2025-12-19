using System.Text;

namespace CSD;

public class Operand
{
    public int x86Length;
    public Instruction parent;
    public long eip;
    public String seg;
    public String type, _base;
    public int size;
    public long lval;
    public Pointer ptr; // should be same as lval somehow
    public String index;
    public long offset, scale;
    public int cast;
    //long pc;
    //value;
    //ref;
    // required for patterns
    public int maxSize;
    public int imm_start, dis_start;

    public override string ToString()
    {
        if (type == null)
            return "UNKNOWN - AVX?";
        if (type.Equals("OP_REG"))
            return _base;
        bool first = true;
        var b = new StringBuilder();
        //if (cast == 1)
        //    b.append(intel_size(size));
        if (type.Equals("OP_MEM"))
        {
            b.Append(Instruction.intel_size(size));

            if (seg != null)
                b.Append(seg + ":");
            else if ((_base == null) && (index == null))
                b.Append("ds:");
            if ((_base != null) || (index != null))
                b.Append('[');

            if (_base != null)
            {
                b.Append(_base);
                first = false;
            }
            if (index != null)
            {
                if (!first)
                    b.Append('+');
                b.Append(index);
                first = false;
            }
            if (scale != 0)
                b.Append("*" + scale);
            if ((offset == 8) || (offset == 16) || (offset == 32) || (offset == 64))
            {
                if (!Instruction.pattern)
                {
                    if ((lval < 0) && ((_base != null) || (index != null)))
                        b.Append("-" + $"0x{-lval:X}");
                    else
                    {
                        if (!first)
                            b.Append("+" + $"0x{lval & ((1L << (int)offset) - 1):X}");
                        else
                            b.Append(value: $"0x{lval & ((1L << (int)offset) - 1):X}");
                    }
                }
                else
                {
                    b.Append('$');
                    for (int i = 0; i < offset / 8; i++)
                        b.Append("DD");
                }
            }
            if ((_base != null) || (index != null))
                b.Append(']');
        }
        else if (type.Equals("OP_IMM"))
        {
            if (!Instruction.pattern)
            {
                if (lval < 0)
                {
                    if (Instruction.sign_extends.Contains(parent.op)) // these are sign extended
                        b.Append($"0x{lval & ((1L << maxSize) - 1):X}");
                    else
                        b.Append($"0x{lval & ((1L << size) - 1):X}");
                }
                else
                    b.Append(value: $"0x{lval:X}");
            }
            else
            {
                b.Append("$");
                for (int i = 0; i < size / 8; i++)
                    b.Append("II");
            }
        }
        else if (type.Equals("OP_JIMM"))
        {
            if (!Instruction.pattern)
            {
                if (eip + x86Length + lval < 0)
                    b.Append($"0x{(eip + x86Length + lval) & ((1L << maxSize) - 1):X}");
                else
                    b.Append($"0x{eip + x86Length + lval:X}");
            }
            else
            {
                b.Append('$');
                for (int i = 0; i < size / 8; i++)
                    b.Append("II");
            }
        }
        else if (type.Equals("OP_PTR"))
        {
            if (!Instruction.pattern)
                b.Append(String.Format($"0x{{0:X4}}:0x{{1:X}}", ptr.seg & 0xFFFF, ptr.off));
            else
                b.Append("$SSSS:$DDDDDDDD");
        }
        return b.ToString();
        //return String.format("[%s %s %s %d %x %x %x]", type, @base, index, size, lval, offset, scale);
    }
}