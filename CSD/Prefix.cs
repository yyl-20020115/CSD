using System.Text;

namespace CSD;

public class Prefix
{
    public int Rex, Opr, Adr, Lock, Rep, Repe, Repne, Insn;
    public string seg = "";
    public override string ToString()
    {
        var builder = new StringBuilder();
        if (Lock != 0)
            builder.Append("lock ");
        if (Rep != 0)
            builder.Append("rep ");
        if (Repe != 0)
            builder.Append("repe ");
        if (Repne != 0)
            builder.Append("repne ");
        //if (seg != null)
        //    b.append(seg+" ");

        return builder.ToString();
    }
}
