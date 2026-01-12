namespace CSD;

using System.Collections.Generic;

public class Instruction
{
    private static readonly HashSet<string> invalid = [];
    private static readonly HashSet<string> call = [];
    private static readonly HashSet<string> ret = [];
    private static readonly HashSet<string> jmp = [];
    private static readonly HashSet<string> jcc = [];
    private static readonly HashSet<string> hlt = [];

    static Instruction()
    {
        invalid.Add("invalid");
        call.Add("call");
        call.Add("syscall");
        call.Add("vmcall");
        call.Add("vmmcall");
        ret.Add("ret");
        ret.Add("retf");
        ret.Add("sysret");
        ret.Add("iretw");
        ret.Add("iretd");
        ret.Add("iretq");
        jmp.Add("jmp");
        jcc.Add("jo");
        jcc.Add("jno");
        jcc.Add("jb");
        jcc.Add("jbe");
        jcc.Add("ja");
        jcc.Add("jae");
        jcc.Add("je");
        jcc.Add("jne");
        jcc.Add("js");
        jcc.Add("jns");
        jcc.Add("jp");
        jcc.Add("jnp");
        jcc.Add("jl");
        jcc.Add("jle");
        jcc.Add("jg");
        jcc.Add("jge");
        jcc.Add("jcxz");
        jcc.Add("jecxz");
        jcc.Add("jrcxz");
        jcc.Add("loop");
        jcc.Add("loope");
        jcc.Add("loopnz");
        hlt.Add("hlt");
    }

    /*input;
      mode;
      add;*/
    public long x86Length;
    public long eip;
    public ZygoteInstruction? zygote;
    public string opcode = "invalid";
    public Operand[] operand = [];
    public Prefix prefix = new();
    public int operand_mode, address_mode;
    public string? branch_dist;

    public Instruction() { }

    public static readonly List<string> SignExtends = ["cmp", "or", "imul", "adc", "sbb", "xor"];

    public static string IntelModeSize(int size) => size switch
    {
        0 => "",//XMMWORD PTR ";
        8 => "BYTE PTR ",
        16 => "WORD PTR ",
        32 => "DWORD PTR ",
        64 => "QWORD PTR ",
        80 => "TBYTE PTR ",
        _ => throw new InvalidOperationException("Unknown operand size " + size),
    };


    public override string ToString() => ToString(false);
    public string ToString(bool WithSize = false)
    {
        int maxSize = 0;
        foreach (var op in operand)
        {
            op.parent = this;
            if (op.size > maxSize)
                maxSize = op.size;
            op.eip = eip;
            op.x86Length = x86Length;
        }
        foreach (var op in operand)
            op.maxSize = maxSize;

        return WithSize
            ? operand.Length switch
            {
                1 => $"({x86Length} bytes) {prefix}{opcode + (branch_dist == null ? "" : " " + branch_dist)} {operand[0]}",
                2 => $"({x86Length} bytes) {prefix}{opcode + (branch_dist == null ? "" : " " + branch_dist)} {operand[0]}, {operand[1]}",
                3 => $"({x86Length} bytes) {prefix}{opcode + (branch_dist == null ? "" : " " + branch_dist)} {operand[0]}, {operand[1]}, {operand[2]}",
                _ => $"({x86Length} bytes) {prefix}{opcode + (branch_dist == null ? "" : " " + branch_dist)}",
            }
            : operand.Length switch
            {
                1 => $"{prefix}{opcode + (branch_dist == null ? "" : " " + branch_dist)} {operand[0]}",
                2 => $"{prefix}{opcode + (branch_dist == null ? "" : " " + branch_dist)} {operand[0]},{operand[1]}",
                3 => $"{prefix}{opcode + (branch_dist == null ? "" : " " + branch_dist)} {operand[0]},{operand[1]},{operand[2]}",
                _ => $"{prefix}{opcode + (branch_dist == null ? "" : " " + branch_dist)}",
            };
    }
}