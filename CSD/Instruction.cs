namespace CSD;

using System.Collections.Generic;

public class Instruction
{
    private static readonly HashSet<string> Invalid = [];
    private static readonly HashSet<string> Call = [];
    private static readonly HashSet<string> Ret = [];
    private static readonly HashSet<string> Jmp = [];
    private static readonly HashSet<string> Jcc = [];
    private static readonly HashSet<string> Hlt = [];

    static Instruction()
    {
        Invalid.Add("invalid");
        Call.Add("call");
        Call.Add("syscall");
        Call.Add("vmcall");
        Call.Add("vmmcall");
        Ret.Add("ret");
        Ret.Add("retf");
        Ret.Add("sysret");
        Ret.Add("iretw");
        Ret.Add("iretd");
        Ret.Add("iretq");
        Jmp.Add("jmp");
        Jcc.Add("jo");
        Jcc.Add("jno");
        Jcc.Add("jb");
        Jcc.Add("jbe");
        Jcc.Add("ja");
        Jcc.Add("jae");
        Jcc.Add("je");
        Jcc.Add("jne");
        Jcc.Add("js");
        Jcc.Add("jns");
        Jcc.Add("jp");
        Jcc.Add("jnp");
        Jcc.Add("jl");
        Jcc.Add("jle");
        Jcc.Add("jg");
        Jcc.Add("jge");
        Jcc.Add("jcxz");
        Jcc.Add("jecxz");
        Jcc.Add("jrcxz");
        Jcc.Add("loop");
        Jcc.Add("loope");
        Jcc.Add("loopnz");
        Hlt.Add("hlt");
    }

    /*input;
      mode;
      add;*/
    public long Length;
    public long EIP;
    public TemplateInstruction? Template;
    public string OpCode = "invalid";
    public Operand[] Operand = [];
    public Prefix Prefix = new();
    public int OperandMode, AddressMode;
    public string? BranchDist;

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
        foreach (var op in Operand)
        {
            op.Parent = this;
            if (op.Size > maxSize)
                maxSize = op.Size;
            op.EIP = EIP;
            op.Length = Length;
        }
        foreach (var op in Operand)
            op.MaxSize = maxSize;

        return WithSize
            ? Operand.Length switch
            {
                1 => $"({Length} bytes) {Prefix}{OpCode + (BranchDist == null ? "" : " " + BranchDist)} {Operand[0]}",
                2 => $"({Length} bytes) {Prefix}{OpCode + (BranchDist == null ? "" : " " + BranchDist)} {Operand[0]}, {Operand[1]}",
                3 => $"({Length} bytes) {Prefix}{OpCode + (BranchDist == null ? "" : " " + BranchDist)} {Operand[0]}, {Operand[1]}, {Operand[2]}",
                _ => $"({Length} bytes) {Prefix}{OpCode + (BranchDist == null ? "" : " " + BranchDist)}",
            }
            : Operand.Length switch
            {
                1 => $"{Prefix}{OpCode + (BranchDist == null ? "" : " " + BranchDist)} {Operand[0]}",
                2 => $"{Prefix}{OpCode + (BranchDist == null ? "" : " " + BranchDist)} {Operand[0]},{Operand[1]}",
                3 => $"{Prefix}{OpCode + (BranchDist == null ? "" : " " + BranchDist)} {Operand[0]},{Operand[1]},{Operand[2]}",
                _ => $"{Prefix}{OpCode + (BranchDist == null ? "" : " " + BranchDist)}",
            };
    }
}