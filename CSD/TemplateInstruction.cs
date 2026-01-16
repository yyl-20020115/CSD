namespace CSD;

public class TemplateInstruction(string Mnemonic, TemplateOperand Operand1, TemplateOperand Operand2, TemplateOperand Operand3, int Prefix)
{
    public readonly string OpCode = Mnemonic;
    public readonly TemplateOperand[] Operand = [Operand1, Operand2, Operand3];
    public readonly int Prefix = Prefix;
}