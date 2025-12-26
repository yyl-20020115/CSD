namespace CSD;

public class ZygoteInstruction(string mnemonic, ZygoteOperand op1, ZygoteOperand op2, ZygoteOperand op3, int prefix)
{
    public readonly string op = mnemonic;
    public readonly ZygoteOperand[] operand = [op1, op2, op3];
    public readonly int prefix = prefix;
}