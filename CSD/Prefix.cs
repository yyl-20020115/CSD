using System.Text;

namespace CSD;

public class Prefix
{
    public int rex, opr, adr, _lock, rep, repe, repne, insn;
    public string seg = "";
    public override string ToString()
    {
        var builder = new StringBuilder();
        if (_lock != 0)
            builder.Append("lock ");
        if (rep != 0)
            builder.Append("rep ");
        if (repe != 0)
            builder.Append("repe ");
        if (repne != 0)
            builder.Append("repne ");
        //if (seg != null)
        //    b.append(seg+" ");

        return builder.ToString();
    }
}
