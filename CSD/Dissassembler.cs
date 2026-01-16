namespace CSD;

using static TemplateOperand;
using static Table;
public static class Dissassembler
{
    public static readonly TemplateInstruction[][] itab = Table.itab_list;
    public static readonly int vendor = VENDOR_INTEL;
    public static readonly TemplateInstruction ie_invalid = new("invalid", O_NONE, O_NONE, O_NONE, P_none);
    public static readonly TemplateInstruction ie_pause = new("pause", O_NONE, O_NONE, O_NONE, P_none);
    public static readonly TemplateInstruction ie_nop = new("nop", O_NONE, O_NONE, O_NONE, P_none);
    
    public static Instruction Decode(ReversibleStream input, int mode)
    {
        var offset = input.Index;
        var instruction = new Instruction();
        GetPrefixes(mode, input, instruction);
        SearchTable(mode, input, instruction);
        DoMode(mode, input, instruction);
        DisassembleOperands(mode, input, instruction);
        ResolveOperator(mode, input, instruction);
        instruction.Length = input.Index - offset;
        return instruction;
    }

    private static void GetPrefixes(int mode, ReversibleStream input, Instruction inst)
    {
        int curr;
        int i = 0;
        while (true)
        {
            curr = input.Current;
            input.Forward();
            i++;

            if ((mode == 64) && ((curr & 0xF0) == 0x40))
                inst.Prefix.Rex = curr;
            else
            {
                if (curr == 0x2E)
                {
                    inst.Prefix.seg = "cs";
                    inst.Prefix.Rex = 0;
                }
                else if (curr == 0x36)
                {
                    inst.Prefix.seg = "ss";
                    inst.Prefix.Rex = 0;
                }
                else if (curr == 0x3E)
                {
                    inst.Prefix.seg = "ds";
                    inst.Prefix.Rex = 0;
                }
                else if (curr == 0x26)
                {
                    inst.Prefix.seg = "es";
                    inst.Prefix.Rex = 0;
                }
                else if (curr == 0x64)
                {
                    inst.Prefix.seg = "fs";
                    inst.Prefix.Rex = 0;
                }
                else if (curr == 0x65)
                {
                    inst.Prefix.seg = "gs";
                    inst.Prefix.Rex = 0;
                }
                else if (curr == 0x67) //adress-size override prefix
                {
                    inst.Prefix.Adr = 0x67;
                    inst.Prefix.Rex = 0;
                }
                else if (curr == 0xF0)
                {
                    inst.Prefix.Lock = 0xF0;
                    inst.Prefix.Rex = 0;
                }
                else if (curr == 0x66)
                {
                    // the 0x66 sse prefix is only effective if no other sse prefix
                    // has already been specified.
                    if (inst.Prefix.Insn == 0)
                        inst.Prefix.Insn = 0x66;
                    inst.Prefix.Opr = 0x66;
                    inst.Prefix.Rex = 0;
                }
                else if (curr == 0xF2)
                {
                    inst.Prefix.Insn = 0xF2;
                    inst.Prefix.Repne = 0xF2;
                    inst.Prefix.Rex = 0;
                }
                else if (curr == 0xF3)
                {
                    inst.Prefix.Insn = 0xF3;
                    inst.Prefix.Rep = 0xF3;
                    inst.Prefix.Repe = 0xF3;
                    inst.Prefix.Rex = 0;
                }
                else
                    //No more prefixes
                    break;
            }
        }
        if (i >= MAX_INSTRUCTION_LENGTH)
            throw new InvalidOperationException("Max instruction Length exceeded");

        input.Backward();

        // speculatively determine the effective operand mode,
        // based on the prefixes and the current disassembly
        // mode. This may be inaccurate, but useful for mode
        // dependent decoding.
        switch (mode)
        {
            case 64:
                inst.OperandMode = REX_W(inst.Prefix.Rex) != 0 ? 64 : inst.Prefix.Opr != 0 ? 16 : P_DEF64(inst.Template.Prefix) != 0 ? 64 : 32;
                inst.AddressMode = inst.Prefix.Adr != 0 ? 32 : 64;
                break;
            case 32:
                inst.OperandMode = inst.Prefix.Opr != 0 ? 16 : 32;
                inst.AddressMode = inst.Prefix.Adr != 0 ? 16 : 32;
                break;
            case 16:
                inst.OperandMode = inst.Prefix.Opr != 0 ? 32 : 16;
                inst.AddressMode = inst.Prefix.Adr != 0 ? 32 : 16;
                break;
        }
    }

    private static void SearchTable(int mode, ReversibleStream input, Instruction inst)
    {
        bool did_peek = false;
        int peek;
        int curr = input.Current;
        input.Forward();

        int table = 0;
        TemplateInstruction e;

        // resolve xchg, nop, pause crazyness
        if (0x90 == curr)
        {
            if (!((mode == 64) && (REX_B(inst.Prefix.Rex) != 0)))
            {
                if (inst.Prefix.Rep != 0)
                {
                    inst.Prefix.Rep = 0;
                    e = ie_pause;
                }
                else
                    e = ie_nop;
                inst.Template = e;
                inst.OpCode = inst.Template.OpCode;
                return;
            }
        }
        else if (curr == 0x0F)
        {
            table = ITAB__0F;
            curr = input.Current;
            input.Forward();

            // 2byte opcodes can be modified by 0x66, F3, and F2 prefixes
            if (0x66 == inst.Prefix.Insn)
            {
                if (itab[ITAB__PFX_SSE66__0F][curr].OpCode!=("invalid"))
                {
                    table = ITAB__PFX_SSE66__0F;
                    //inst.pfx.opr = 0;
                }
            }
            else if (0xF2 == inst.Prefix.Insn)
            {
                if (itab[ITAB__PFX_SSEF2__0F][curr].OpCode!=("invalid"))
                {
                    table = ITAB__PFX_SSEF2__0F;
                    inst.Prefix.Repne = 0;
                }
            }
            else if (0xF3 == inst.Prefix.Insn)
            {
                if (itab[ITAB__PFX_SSEF3__0F][curr].OpCode!=("invalid"))
                {
                    table = ITAB__PFX_SSEF3__0F;
                    inst.Prefix.Repe = 0;
                    inst.Prefix.Rep = 0;
                }
            }
        }
        else
            table = ITAB__1BYTE;

        int index = curr;

        while (true)
        {
            e = itab[table][index];
            // if @operator constant is a standard instruction constant
            // our search is over.
            if (ops.Contains(e.OpCode))
            {
                if (e.OpCode==("invalid"))
                    if (did_peek)
                        input.Forward();
                inst.Template = e;
                inst.OpCode = e.OpCode;
                return;
            }

            table = e.Prefix;

            switch (e.OpCode)
            {
                case "grp_reg":
                    peek = input.Current;
                    did_peek = true;
                    index = MODRM_REG(peek);
                    break;
                case "grp_mod":
                    peek = input.Current;
                    did_peek = true;
                    index = MODRM_MOD(peek);
                    index = index == 3 ? ITAB__MOD_INDX__11 : ITAB__MOD_INDX__NOT_11;
                    break;
                case "grp_rm":
                    curr = input.Current;
                    input.Forward();
                    did_peek = false;
                    index = MODRM_RM(curr);
                    break;
                case "grp_x87":
                    curr = input.Current;
                    input.Forward();
                    did_peek = false;
                    index = curr - 0xC0;
                    break;
                case "grp_osize":
                    index = inst.OperandMode == 64 ? ITAB__MODE_INDX__64 : inst.OperandMode == 32 ? ITAB__MODE_INDX__32 : ITAB__MODE_INDX__16;
                    break;
                case "grp_asize":
                    index = inst.AddressMode == 64 ? ITAB__MODE_INDX__64 : inst.AddressMode == 32 ? ITAB__MODE_INDX__32 : ITAB__MODE_INDX__16;
                    break;
                case "grp_mode":
                    index = mode == 64 ? ITAB__MODE_INDX__64 : mode == 32 ? ITAB__MODE_INDX__32 : ITAB__MODE_INDX__16;
                    break;
                case "grp_vendor":
                    index = vendor == VENDOR_INTEL
                            ? ITAB__VENDOR_INDX__INTEL
                            : vendor == VENDOR_AMD ? ITAB__VENDOR_INDX__AMD : throw new SystemException("unrecognized vendor id");
                    break;
                case "d3vil":
                    throw new SystemException("invalid instruction @operator constant Id3vil");
                default:
                    throw new SystemException("invalid instruction @operator constant");
            }
        }
        //inst.zygote = e;
        //inst.@operator = e.@operator;
        //return;
    }

    private static void DoMode(int mode, ReversibleStream input, Instruction inst)
    {
        // propagate prefix effects 
        switch (mode)  // set 64bit-mode flags
        {
            case 64:
                // Check validity of  instruction m64 
                if (P_INV64(inst.Template.Prefix) != 0)
                    throw new InvalidOperationException("Invalid instruction");

                // effective rex prefix is the  effective mask for the 
                // instruction hard-coded in the opcode map.
                inst.Prefix.Rex = inst.Prefix.Rex & 0x40
                                | inst.Prefix.Rex & REX_PFX_MASK(inst.Template.Prefix);

                // calculate effective operand size 
                inst.OperandMode = REX_W(inst.Prefix.Rex) != 0 || P_DEF64(inst.Template.Prefix) != 0 ? 64 : inst.Prefix.Opr != 0 ? 16 : 32;

                // calculate effective address size
                inst.AddressMode = inst.Prefix.Adr != 0 ? 32 : 64;
                break;
            case 32:
                inst.OperandMode = inst.Prefix.Opr != 0 ? 16 : 32;
                inst.AddressMode = inst.Prefix.Adr != 0 ? 16 : 32;
                break;
            case 16:
                inst.OperandMode = inst.Prefix.Opr != 0 ? 32 : 16;
                inst.AddressMode = inst.Prefix.Adr != 0 ? 32 : 16;
                break;
        }
    }

    private static void ResolveOperator(int mode, ReversibleStream input, Instruction inst)
    {
        // far/near flags 
        inst.BranchDist = null;
        // readjust operand sizes for call/jmp instrcutions 
        if (inst.OpCode==("call") || inst.OpCode==("jmp"))
        {
            if (inst.Operand[0].Size == SZ_WP)
            {
                // WP: 16bit pointer 
                inst.Operand[0].Size = 16;
                inst.BranchDist = "far";
            }
            else if (inst.Operand[0].Size == SZ_DP)
            {
                // DP: 32bit pointer
                inst.Operand[0].Size = 32;
                inst.BranchDist = "far";
            }
            else if (inst.Operand[0].Size == 8)
                inst.BranchDist = "near";
        }
        else if (inst.OpCode==("3dnow"))
        {
            // resolve 3dnow weirdness 
            inst.OpCode = itab[ITAB__3DNOW][input.Current].OpCode;
        }
        // SWAPGS is only valid in 64bits mode
        if ((inst.OpCode==("swapgs")) && (mode != 64))
            throw new InvalidOperationException("SWAPGS only valid in 64 bit mode");
    }

    private static void DisassembleOperands(int mode, ReversibleStream input, Instruction inst)
    {
        // get type
        var mopt = new int[inst.Template.Operand.Length];
        for (int i = 0; i < mopt.Length; i++)
            mopt[i] = inst.Template.Operand[i].type;
        // get size
        var mops = new int[inst.Template.Operand.Length];
        for (int i = 0; i < mops.Length; i++)
            mops[i] = inst.Template.Operand[i].size;

        if (mopt[2] != OP_NONE)
            inst.Operand = [new Operand(), new Operand(), new Operand()];
        else if (mopt[1] != OP_NONE)
            inst.Operand = [new Operand(), new Operand()];
        else if (mopt[0] != OP_NONE)
            inst.Operand = [new Operand()];

        // These flags determine which operand to apply the operand size
        // cast to.
        if (inst.Operand.Length > 0)
            inst.Operand[0].Cast = P_C0(inst.Template.Prefix);
        if (inst.Operand.Length > 1)
            inst.Operand[1].Cast = P_C1(inst.Template.Prefix);
        if (inst.Operand.Length > 2)
            inst.Operand[2].Cast = P_C2(inst.Template.Prefix);

        // iop = instruction operand 
        //iop = inst.operand

        if (mopt[0] == OP_A)
            DecodeA(mode, inst, input, inst.Operand[0]);
        // M[b] ... 
        // E, G/P/V/I/CL/1/S 
        else if ((mopt[0] == OP_M) || (mopt[0] == OP_E))
        {
            if ((mopt[0] == OP_M) && (MODRM_MOD(input.Current) == 3))
                throw new InvalidOperationException("");
            if (mopt[1] == OP_G)
            {
                DecodeModRm(mode, inst, input, inst.Operand[0], mops[0], "T_GPR", inst.Operand[1], mops[1], "T_GPR");
                if (mopt[2] == OP_I)
                    DecodeImm(mode, inst, input, mops[2], inst.Operand[2]);
                else if (mopt[2] == OP_CL)
                {
                    inst.Operand[2].Type = "OP_REG";
                    inst.Operand[2].Base = "cl";
                    inst.Operand[2].Size = 8;
                }
            }
            else if (mopt[1] == OP_P)
                DecodeModRm(mode, inst, input, inst.Operand[0], mops[0], "T_GPR", inst.Operand[1], mops[1], "T_MMX");
            else if (mopt[1] == OP_V)
                DecodeModRm(mode, inst, input, inst.Operand[0], mops[0], "T_GPR", inst.Operand[1], mops[1], "T_XMM");
            else if (mopt[1] == OP_S)
                DecodeModRm(mode, inst, input, inst.Operand[0], mops[0], "T_GPR", inst.Operand[1], mops[1], "T_SEG");
            else
            {
                DecodeModRm(mode, inst, input, inst.Operand[0], mops[0], "T_GPR", null, 0, "T_NONE");
                if (mopt[1] == OP_CL)
                {
                    inst.Operand[1].Type = "OP_REG";
                    inst.Operand[1].Base = "cl";
                    inst.Operand[1].Size = 8;
                }
                else if (mopt[1] == OP_I1)
                {
                    inst.Operand[1].Type = "OP_IMM";
                    inst.Operand[1].Lval = 1;
                }
                else if (mopt[1] == OP_I)
                    DecodeImm(mode, inst, input, mops[1], inst.Operand[1]);
            }
        }
        // G, E/PR[,I]/VR 
        else if (mopt[0] == OP_G)
        {
            if (mopt[1] == OP_M)
            {
                if (MODRM_MOD(input.Current) == 3)
                    throw new InvalidOperationException("invalid");
                DecodeModRm(mode, inst, input, inst.Operand[1], mops[1], "T_GPR", inst.Operand[0], mops[0], "T_GPR");
            }
            else if (mopt[1] == OP_E)
            {
                DecodeModRm(mode, inst, input, inst.Operand[1], mops[1], "T_GPR", inst.Operand[0], mops[0], "T_GPR");
                if (mopt[2] == OP_I)
                    DecodeImm(mode, inst, input, mops[2], inst.Operand[2]);
            }
            else if (mopt[1] == OP_PR)
            {
                DecodeModRm(mode, inst, input, inst.Operand[1], mops[1], "T_MMX", inst.Operand[0], mops[0], "T_GPR");
                if (mopt[2] == OP_I)
                    DecodeImm(mode, inst, input, mops[2], inst.Operand[2]);
            }
            else if (mopt[1] == OP_VR)
            {
                if (MODRM_MOD(input.Current) != 3)
                    throw new InvalidOperationException("Invalid instruction");
                DecodeModRm(mode, inst, input, inst.Operand[1], mops[1], "T_XMM", inst.Operand[0], mops[0], "T_GPR");
            }
            else if (mopt[1] == OP_W)
                DecodeModRm(mode, inst, input, inst.Operand[1], mops[1], "T_XMM", inst.Operand[0], mops[0], "T_GPR");
        }
        // AL..BH, I/O/DX 
        else if (ops8.Contains(mopt[0]))
        {
            inst.Operand[0].Type = "OP_REG";
            inst.Operand[0].Base = GPR[("8")][(mopt[0] - OP_AL)];
            inst.Operand[0].Size = 8;

            if (mopt[1] == OP_I)
                DecodeImm(mode, inst, input, mops[1], inst.Operand[1]);
            else if (mopt[1] == OP_DX)
            {
                inst.Operand[1].Type = "OP_REG";
                inst.Operand[1].Base = "dx";
                inst.Operand[1].Size = 16;
            }
            else if (mopt[1] == OP_O)
                DecodeO(mode, inst, input, mops[1], inst.Operand[1]);
        }
        // rAX[r8]..rDI[r15], I/rAX..rDI/O
        else if (ops2.Contains(mopt[0]))
        {
            inst.Operand[0].Type = "OP_REG";
            inst.Operand[0].Base = ResolveGpr64(mode, inst, mopt[0]);

            if (mopt[1] == OP_I)
                DecodeImm(mode, inst, input, mops[1], inst.Operand[1]);
            else if (ops64.Contains(mopt[1]))
            {
                inst.Operand[1].Type = "OP_REG";
                inst.Operand[1].Base = ResolveGpr64(mode, inst, mopt[1]);
            }
            else if (mopt[1] == OP_O)
            {
                DecodeO(mode, inst, input, mops[1], inst.Operand[1]);
                inst.Operand[0].Size = ResolveOperandSize(mode, inst, mops[1]);
            }
        }
        else if (ops3.Contains(mopt[0]))
        {
            int gpr = (mopt[0] - OP_ALr8b + (REX_B(inst.Prefix.Rex) << 3));
            /*if ((gpr in ["ah",	"ch",	"dh",	"bh",
              "spl",	"bpl",	"sil",	"dil",
              "r8b",	"r9b",	"r10b",	"r11b",
              "r12b",	"r13b",	"r14b",	"r15b",
                         ]) && (inst.pfx.rex != 0)) 
                         gpr = gpr + 4;*/
            inst.Operand[0].Type = "OP_REG";
            inst.Operand[0].Base = GPR[("8")][gpr];
            if (mopt[1] == OP_I)
                DecodeImm(mode, inst, input, mops[1], inst.Operand[1]);
        }
        // eAX..eDX, DX/I 
        else if (ops32.Contains(mopt[0]))
        {
            inst.Operand[0].Type = "OP_REG";
            inst.Operand[0].Base = ResolveGpr32(inst, mopt[0]);
            if (mopt[1] == OP_DX)
            {
                inst.Operand[1].Type = "OP_REG";
                inst.Operand[1].Base = "dx";
                inst.Operand[1].Size = 16;
            }
            else if (mopt[1] == OP_I)
                DecodeImm(mode, inst, input, mops[1], inst.Operand[1]);
        }
        // ES..GS 
        else if (ops_segs.Contains(mopt[0]))
        {
            // in 64bits mode, only fs and gs are allowed 
            if (mode == 64)
                if ((mopt[0] != OP_FS) && (mopt[0] != OP_GS))
                    throw new InvalidOperationException("only fs and gs allowed in 64 bit mode");
            inst.Operand[0].Type = "OP_REG";
            inst.Operand[0].Base = GPR[("T_SEG")][(mopt[0] - OP_ES)];
            inst.Operand[0].Size = 16;
        }
        // J 
        else if (mopt[0] == OP_J)
        {
            DecodeImm(mode, inst, input, mops[0], inst.Operand[0]);
            // MK take care of signs
            long bound = 1L << (inst.Operand[0].Size - 1);
            if (inst.Operand[0].Lval > bound)
                inst.Operand[0].Lval = -(((2 * bound) - inst.Operand[0].Lval) % bound);
            inst.Operand[0].Type = "OP_JIMM";
        }
        // PR, I 
        else if (mopt[0] == OP_PR)
        {
            if (MODRM_MOD(input.Current) != 3)
                throw new InvalidOperationException("Invalid instruction");
            DecodeModRm(mode, inst, input, inst.Operand[0], mops[0], "T_MMX", null, 0, "T_NONE");
            if (mopt[1] == OP_I)
                DecodeImm(mode, inst, input, mops[1], inst.Operand[1]);
        }
        // VR, I 
        else if (mopt[0] == OP_VR)
        {
            if (MODRM_MOD(input.Current) != 3)
                throw new InvalidOperationException("Invalid instruction");
            DecodeModRm(mode, inst, input, inst.Operand[0], mops[0], "T_XMM", null, 0, "T_NONE");
            if (mopt[1] == OP_I)
                DecodeImm(mode, inst, input, mops[1], inst.Operand[1]);
        }
        // P, Q[,I]/W/E[,I],VR 
        else if (mopt[0] == OP_P)
        {
            if (mopt[1] == OP_Q)
            {
                DecodeModRm(mode, inst, input, inst.Operand[1], mops[1], "T_MMX", inst.Operand[0], mops[0], "T_MMX");
                if (mopt[2] == OP_I)
                    DecodeImm(mode, inst, input, mops[2], inst.Operand[2]);
            }
            else if (mopt[1] == OP_W)
                DecodeModRm(mode, inst, input, inst.Operand[1], mops[1], "T_XMM", inst.Operand[0], mops[0], "T_MMX");
            else if (mopt[1] == OP_VR)
            {
                if (MODRM_MOD(input.Current) != 3)
                    throw new InvalidOperationException("Invalid instruction");
                DecodeModRm(mode, inst, input, inst.Operand[1], mops[1], "T_XMM", inst.Operand[0], mops[0], "T_MMX");
            }
            else if (mopt[1] == OP_E)
            {
                DecodeModRm(mode, inst, input, inst.Operand[1], mops[1], "T_GPR", inst.Operand[0], mops[0], "T_MMX");
                if (mopt[2] == OP_I)
                    DecodeImm(mode, inst, input, mops[2], inst.Operand[2]);
            }
        }
        // R, C/D 
        else if (mopt[0] == OP_R)
        {
            if (mopt[1] == OP_C)
                DecodeModRm(mode, inst, input, inst.Operand[0], mops[0], "T_GPR", inst.Operand[1], mops[1], "T_CRG");
            else if (mopt[1] == OP_D)
                DecodeModRm(mode, inst, input, inst.Operand[0], mops[0], "T_GPR", inst.Operand[1], mops[1], "T_DBG");
        }
        // C, R 
        else if (mopt[0] == OP_C)
            DecodeModRm(mode, inst, input, inst.Operand[1], mops[1], "T_GPR", inst.Operand[0], mops[0], "T_CRG");
        // D, R 
        else if (mopt[0] == OP_D)
            DecodeModRm(mode, inst, input, inst.Operand[1], mops[1], "T_GPR", inst.Operand[0], mops[0], "T_DBG");
        // Q, P 
        else if (mopt[0] == OP_Q)
            DecodeModRm(mode, inst, input, inst.Operand[0], mops[0], "T_MMX", inst.Operand[1], mops[1], "T_MMX");
        // S, E 
        else if (mopt[0] == OP_S)
            DecodeModRm(mode, inst, input, inst.Operand[1], mops[1], "T_GPR", inst.Operand[0], mops[0], "T_SEG");
        // W, V 
        else if (mopt[0] == OP_W)
            DecodeModRm(mode, inst, input, inst.Operand[0], mops[0], "T_XMM", inst.Operand[1], mops[1], "T_XMM");
        // V, W[,I]/Q/M/E 
        else if (mopt[0] == OP_V)
        {
            if (mopt[1] == OP_W)
            {
                // special cases for movlps and movhps 
                if (MODRM_MOD(input.Current) == 3)
                {
                    if (inst.OpCode==("movlps"))
                        inst.OpCode = "movhlps";
                    else if (inst.OpCode==("movhps"))
                        inst.OpCode = "movlhps";
                }
                DecodeModRm(mode, inst, input, inst.Operand[1], mops[1], "T_XMM", inst.Operand[0], mops[0], "T_XMM");
                if (mopt[2] == OP_I)
                    DecodeImm(mode, inst, input, mops[2], inst.Operand[2]);
            }
            else if (mopt[1] == OP_Q)
                DecodeModRm(mode, inst, input, inst.Operand[1], mops[1], "T_MMX", inst.Operand[0], mops[0], "T_XMM");
            else if (mopt[1] == OP_M)
            {
                if (MODRM_MOD(input.Current) == 3)
                    throw new InvalidOperationException("Invalid instruction");
                DecodeModRm(mode, inst, input, inst.Operand[1], mops[1], "T_GPR", inst.Operand[0], mops[0], "T_XMM");
            }
            else if (mopt[1] == OP_E)
                DecodeModRm(mode, inst, input, inst.Operand[1], mops[1], "T_GPR", inst.Operand[0], mops[0], "T_XMM");
            else if (mopt[1] == OP_PR)
                DecodeModRm(mode, inst, input, inst.Operand[1], mops[1], "T_MMX", inst.Operand[0], mops[0], "T_XMM");
        }
        // DX, eAX/AL
        else if (mopt[0] == OP_DX)
        {
            inst.Operand[0].Type = "OP_REG";
            inst.Operand[0].Base = "dx";
            inst.Operand[0].Size = 16;

            if (mopt[1] == OP_eAX)
            {
                inst.Operand[1].Type = "OP_REG";
                inst.Operand[1].Base = ResolveGpr32(inst, mopt[1]);
            }
            else if (mopt[1] == OP_AL)
            {
                inst.Operand[1].Type = "OP_REG";
                inst.Operand[1].Base = "al";
                inst.Operand[1].Size = 8;
            }
        }
        // I, I/AL/eAX
        else if (mopt[0] == OP_I)
        {
            DecodeImm(mode, inst, input, mops[0], inst.Operand[0]);
            if (mopt[1] == OP_I)
                DecodeImm(mode, inst, input, mops[1], inst.Operand[1]);
            else if (mopt[1] == OP_AL)
            {
                inst.Operand[1].Type = "OP_REG";
                inst.Operand[1].Base = "al";
                inst.Operand[1].Size = 8;
            }
            else if (mopt[1] == OP_eAX)
            {
                inst.Operand[1].Type = "OP_REG";
                inst.Operand[1].Base = ResolveGpr32(inst, mopt[1]);
            }
        }
        // O, AL/eAX
        else if (mopt[0] == OP_O)
        {
            DecodeO(mode, inst, input, mops[0], inst.Operand[0]);
            inst.Operand[1].Type = "OP_REG";
            inst.Operand[1].Size = ResolveOperandSize(mode, inst, mops[0]);
            if (mopt[1] == OP_AL)
                inst.Operand[1].Base = "al";
            else if (mopt[1] == OP_eAX)
                inst.Operand[1].Base = ResolveGpr32(inst, mopt[1]);
            else if (mopt[1] == OP_rAX)
                inst.Operand[1].Base = ResolveGpr64(mode, inst, mopt[1]);
        }
        // 3
        else if (mopt[0] == OP_I3)
        {
            inst.Operand[0].Type = "OP_IMM";
            inst.Operand[0].Lval = 3;
        }
        // ST(n), ST(n) 
        else if (ops_st.Contains(mopt[0]))
        {
            inst.Operand[0].Type = "OP_REG";
            inst.Operand[0].Base = GPR[("T_ST")][(mopt[0] - OP_ST0)];
            inst.Operand[0].Size = 0;

            if (ops_st.Contains(mopt[1]))
            {
                inst.Operand[1].Type = "OP_REG";
                inst.Operand[1].Base = GPR[("T_ST")][(mopt[1] - OP_ST0)];
                inst.Operand[1].Size = 0;
            }
        }
        // AX 
        else if (mopt[0] == OP_AX)
        {
            inst.Operand[0].Type = "OP_REG";
            inst.Operand[0].Base = "ax";
            inst.Operand[0].Size = 16;
        }
        // none 
        else
            for (int i = 0; i < inst.Operand.Length; i++)
                inst.Operand[i].Type = null;
    }

    private static void DecodeA(int mode, Instruction inst, ReversibleStream input, Operand op)
    {
        //Decodes operands of the type seg:offset.
        if (inst.OperandMode == 16)
        {
            // seg16:off16 
            op.Type = "OP_PTR";
            op.Size = 32;
            op.DisStart = (int)input.Index;
            op.Pointer = new Pointer(input.Read16(), input.Read16());
        }
        else
        {
            // seg16:off32 
            op.Type = "OP_PTR";
            op.Size = 48;
            op.DisStart = (int)input.Index;
            op.Pointer = new Pointer(input.Read32(), input.Read16());
        }
    }

    private static void DecodeModRm(int mode, Instruction inst, ReversibleStream input, Operand op, int s, string rm_type, Operand opreg, int reg_size, string reg_type)
    {
        // get mod, r/m and reg fields
        int mod = MODRM_MOD(input.Current);
        int rm = (REX_B(inst.Prefix.Rex) << 3) | MODRM_RM(input.Current);
        int reg = (REX_R(inst.Prefix.Rex) << 3) | MODRM_REG(input.Current);

        if (reg_type==("T_DBG") || reg_type==("T_CRG")) // force these to be reg ops (mod is ignored)
            mod = 3;

        op.Size = ResolveOperandSize(mode, inst, s);

        // if mod is 11b, then the m specifies a gpr/mmx/sse/control/debug 
        if (mod == 3)
        {
            op.Type = "OP_REG";
            op.Base = rm_type == "T_GPR" ? DecodeGpr(mode, inst, op.Size, rm) : ResolveReg(rm_type, (REX_B(inst.Prefix.Rex) << 3) | (rm & 7));
        }
        // else its memory addressing 
        else
        {
            op.Type = "OP_MEM";
            op.Segment = inst.Prefix.seg;
            // 64bit addressing 
            if (inst.AddressMode == 64)
            {
                op.Base = GPR[("64")][(rm)];

                // get offset type
                if (mod == 1)
                    op.Offset = 8;
                else if (mod == 2)
                    op.Offset = 32;
                else if ((mod == 0) && ((rm & 7) == 5))
                {
                    op.Base = "rip";
                    op.Offset = 32;
                }
                else
                    op.Offset = 0;

                // Scale-Index-Base(SIB)
                if ((rm & 7) == 4)
                {
                    input.Forward();

                    op.Scale = (1 << SIB_S(input.Current)) & ~1;
                    op.Index = GPR[("64")][((SIB_I(input.Current) | (REX_X(inst.Prefix.Rex) << 3)))];
                    op.Base = GPR[("64")][((SIB_B(input.Current) | (REX_B(inst.Prefix.Rex) << 3)))];

                    // special conditions for @base reference
                    if (op.Index==("rsp"))
                    {
                        op.Index = null;
                        op.Scale = 0;
                    }

                    if ((op.Base==("rbp")) || (op.Base==("r13")))
                    {
                        if (mod == 0)
                            op.Base = null;
                        if (mod == 1)
                            op.Offset = 8;
                        else
                            op.Offset = 32;
                    }
                }
            }
            // 32-Bit addressing mode 
            else if (inst.AddressMode == 32)
            {
                // get @base 
                op.Base = GPR[("32")][(rm)];

                // get offset type 
                if (mod == 1)
                    op.Offset = 8;
                else if (mod == 2)
                    op.Offset = 32;
                else if ((mod == 0) && (rm == 5))
                {
                    op.Base = null;
                    op.Offset = 32;
                }
                else
                    op.Offset = 0;

                // Scale-Index-Base(SIB)
                if ((rm & 7) == 4)
                {
                    input.Forward();

                    op.Scale = (1 << SIB_S(input.Current)) & ~1;
                    op.Index = GPR[("32")][(SIB_I(input.Current) | (REX_X(inst.Prefix.Rex) << 3))];
                    op.Base = GPR[("32")][(SIB_B(input.Current) | (REX_B(inst.Prefix.Rex) << 3))];

                    if (op.Index==("esp"))
                    {
                        op.Index = null;
                        op.Scale = 0;
                    }

                    // special condition for @base reference 
                    if (op.Base==("ebp"))
                    {
                        if (mod == 0)
                            op.Base = null;
                        if (mod == 1)
                            op.Offset = 8;
                        else
                            op.Offset = 32;
                    }
                }
            }
            // 16bit addressing mode 
            else
            {
                switch (rm)
                {
                    case 0:
                        op.Base = "bx";
                        op.Index = "si";
                        break;
                    case 1:
                        op.Base = "bx";
                        op.Index = "di";
                        break;
                    case 2:
                        op.Base = "bp";
                        op.Index = "si";
                        break;
                    case 3:
                        op.Base = "bp";
                        op.Index = "di";
                        break;
                    case 4:
                        op.Base = "si";
                        break;
                    case 5:
                        op.Base = "di";
                        break;
                    case 6:
                        op.Base = "bp";
                        break;
                    case 7:
                        op.Base = "bx";
                        break;
                }

                if ((mod == 0) && (rm == 6))
                {
                    op.Offset = 16;
                    op.Base = null;
                }
                else if (mod == 1)
                    op.Offset = 8;
                else if (mod == 2)
                    op.Offset = 16;
            }
        }
        input.Forward();
        // extract offset, if any 
        if ((op.Offset == 8) || (op.Offset == 16) || (op.Offset == 32) || (op.Offset == 64))
        {
            op.DisStart = (int)input.Index;
            op.Lval = input.Read(op.Offset);
            long bound = 1L << (int)(op.Offset - 1);
            if (op.Lval > bound)
                op.Lval = -(((2 * bound) - op.Lval) % bound);
        }

        // resolve register encoded in reg field
        if (opreg != null)
        {
            opreg.Type = "OP_REG";
            opreg.Size = ResolveOperandSize(mode, inst, reg_size);
            opreg.Base = reg_type ==("T_GPR") ? DecodeGpr(mode, inst, opreg.Size, reg) : ResolveReg(reg_type, reg);
        }
    }

    private static void DecodeImm(int mode, Instruction inst, ReversibleStream input, int s, Operand op)
    {
        op.Size = ResolveOperandSize(mode, inst, s);
        op.Type = "OP_IMM";
        op.ImmStart = (int)input.Index;
        op.Lval = input.Read(op.Size);
    }

    private static void DecodeO(int mode, Instruction inst, ReversibleStream input, int s, Operand op)
    {
        // offset
        op.Segment = inst.Prefix.seg;
        op.Offset = inst.AddressMode;
        op.DisStart = (int)input.Index;
        op.Lval = input.Read(inst.AddressMode);
        op.Type = "OP_MEM";
        op.Size = ResolveOperandSize(mode, inst, s);
    }

    private static string ResolveGpr32(Instruction inst, int gpr_op)
    {
        int index = gpr_op - OP_eAX;
        return inst.OperandMode == 16 ? GPR[("16")][(index)] : GPR[("32")][(index)];
    }

    private static string? ResolveGpr64(int mode, Instruction inst, int gpr_op)
    {
        int index = (OP_rAXr8 <= gpr_op) && (OP_rDIr15 >= gpr_op) ? (gpr_op - OP_rAXr8) | (REX_B(inst.Prefix.Rex) << 3) : gpr_op - OP_rAX;
        return inst.OperandMode == 16
            ? GPR[("16")]?[(index)]
            : (mode == 32) || !((inst.OperandMode == 32) && (REX_W(inst.Prefix.Rex) == 0)) ? GPR[("32")][index] : GPR[("64")][(index)];
    }

    private static int ResolveOperandSize(int mode, Instruction inst, int s) => s == SZ_V
            ? inst.OperandMode
            : s == SZ_Z
            ? inst.OperandMode == 16 ? 16 : 32
            : s == SZ_P
            ? inst.OperandMode == 16 ? SZ_WP : SZ_DP
            : s == SZ_MDQ ? inst.OperandMode == 16 ? 32 : inst.OperandMode : s == SZ_RDQ ? mode == 64 ? 64 : 32 : s;

    private static string DecodeGpr(int mode, Instruction inst, int s, int rm)
    {
        s = ResolveOperandSize(mode, inst, s);
        return s == 64
            ? GPR[("64")][(rm)]
            : (s == SZ_DP) || (s == 32)
            ? GPR[("32")][(rm)]
            : (s == SZ_WP) || (s == 16)
            ? GPR[("16")][(rm)]
            : s == 8 ? (mode == 64) 
            && (inst.Prefix.Rex != 0) ? rm >= 4 ? GPR[("8")][(rm + 4)] : GPR[("8")][(rm)] : GPR[("8")][(rm)] : null;
    }

    private static string? ResolveReg(string regtype, int i) 
        => GPR[regtype]?[i];
}
