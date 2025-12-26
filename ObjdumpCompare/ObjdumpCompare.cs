using CSD;

namespace ObjdumpCompare;

using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

public static class ObjdumpCompare
{
    private static readonly Regex sps = new(@"\s+");
    public static void Main(string[] args)
    {
        int mode = 32;
        Dictionary<string, string> invalid = [];
        foreach (var file in args)
        {
            int count = 0;
            using var reader = new StreamReader(file);
            while (true)
            {
                var line = reader.ReadLine();
                if (line == null)
                    break;
                count++;

                if (line.IndexOf('\t') < 0)
                    continue;
                if (line.Length < 5)
                    continue;

                var parts = line.Split("\t");
                var addrString = parts[0].Trim();
                if (!addrString.EndsWith(':'))
                    continue;
                //throw new IllegalStateException("Invalid address prefix on line "+lineCount + " of " + file);
                var address = long.TryParse(
                    addrString.AsSpan(0, addrString.Length - 1), System.Globalization.NumberStyles.HexNumber, null, out var v) ? v : 0;


                var x86Bytes = parts[1].Trim();
                var byteStrings = parts[1].Trim().Split(" ");
                var rawX86 = new byte[byteStrings.Length];
                for (int i = 0; i < rawX86.Length; i++)
                    rawX86[i] = (byte)(int.TryParse(byteStrings[i], System.Globalization.NumberStyles.HexNumber, null, out var v2) ? v2 : 0);

                var objdump = parts[2].Trim();
                objdump = sps.Replace(objdump, " ");
                objdump = objdump.Replace("\r\n", "\n")
                    .Replace("eiz+", "")
                    .Replace("+eiz*1", "")
                    .Replace("+eiz*2", "")
                    .Replace("+eiz*4", "")
                    .Replace("+eiz*8", "")
                    .Replace("*1", "")
                    .Replace("DWORD PTR ", "")
                    .Replace("XMMWORD PTR ", "")
                    .Replace("st(7)", "st7")
                    .Replace("st(6)", "st6")
                    .Replace("st(5)", "st5")
                    .Replace("st(4)", "st4")
                    .Replace("st(3)", "st3")
                    .Replace("st(2)", "st2")
                    .Replace("st(1)", "st1")
                    .Replace("st(0)", "st0")
                    .Replace("st,", "st0,")
                    .Replace(",1", ",0x1")
                    .Replace("gs ", "")
                    .Replace("es ", "")
                    .Replace("data32 ", "");

                if (objdump.EndsWith(",st"))
                    objdump = objdump.Replace(",st", ",st0");
                int end = objdump.IndexOf('<');
                if (end > 0)
                    objdump = objdump[..end].Trim();
                if (objdump.Contains('?')
                    || objdump.Contains("bad")
                    || objdump.Contains("fs:gs:")
                    || objdump.Contains(".byte")
                    || objdump.Contains("addr16")
                    || objdump.Contains("data16")
                    || objdump.StartsWith("nop")
                    || objdump==("lock")
                    || objdump==("cs")
                    || objdump==("ds")
                    || objdump==("es")
                    || objdump==("fs")
                    || objdump==("gs")
                    || objdump==("ss")
                    || objdump==("fnop")
                    || objdump==("xgetbv"))
                    continue;

                List<string> excludes = ["insb", "insd", "outsb", "outsw", "outsd", "movsb", "movsw", "movsd", "lodsb", "lodsw", "lodsd", "stosb", "stosw", "stosd", "scasb", "scasw", "scasd", "cmpsb", "cmpsw", "cmpsd", "prefetch", "prefetcht0", "prefetchnta", "ret", "iretd", "fld", "lea", "fxch", "fcom", "fcomp", "pause", "sahf", "mov", "popad", "popfd", "pushfd", "pushad", "xlatb", "frstor", "fnsave", "fldenv", "fnstenv", "rcl", "jle", "je", "jbe", "int1", "push", "wait", "popa", "pshufw", "movq", "movlps", "movlpd", "movhpd", "call", "jmp", "bound", "fsub", "fsubrp", "pop", "arpl", "aam", "dec", "and", "add", "fiadd", "fisttp", "sub", "enter", "sldt", "les", "lds", "lfs", "hlt", "str", "cmpxchg8b"];

                var input = new ReversibleStream(rawX86);
                Instruction instruction;
                try
                {
                    instruction = Dissassembler.Decode(input, mode);
                    instruction.eip = address;
                }
                catch (Exception e)
                {
                    if (!objdump.StartsWith('v')) // AVX
                        Console.WriteLine($"Disassemble error on : {x86Bytes} ({objdump})");
                    continue;
                    //throw e;
                    //e.printStackTrace();
                }
                try
                {
                    if (instruction.op == ("invalid"))
                    {
                        string opname = objdump[..objdump.IndexOf(' ')];
                        if (!invalid.ContainsKey(opname))
                        {
                            invalid.Add(opname, x86Bytes + " " + objdump);
                            Console.WriteLine(x86Bytes + " -> " + instruction + " != " + objdump);
                        }
                        continue;
                    }
                    if ((instruction.op != "nop") || (!objdump.Contains("xchg")))
                        if (!excludes.Contains(instruction.op))
                            if (instruction.ToString().Replace("near ", "").Replace("0x", "").Replace("DWORD PTR ", "")!=(objdump.Replace("0x", "")))
                                Console.WriteLine(x86Bytes + " -> " + instruction + " != " + objdump);
                }
                catch (Exception e)
                {
                    Console.WriteLine($"Print error on : {x86Bytes} ({objdump})");
                    throw e;
                }
            }
        }
    }
}
