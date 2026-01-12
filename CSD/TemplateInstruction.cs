namespace CSD;

public class ZygoteInstruction(string mnemonic, TemplateOperand op1, TemplateOperand op2, TemplateOperand op3, int prefix)
{
    public readonly string op = mnemonic;
    public readonly TemplateOperand[] operand = [op1, op2, op3];
    public readonly int prefix = prefix;
}