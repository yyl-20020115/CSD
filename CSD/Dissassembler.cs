namespace CSD;

using static TemplateOperand;
using static Table;
public static class Dissassembler
{
    public static readonly ZygoteInstruction[][] itab = Table.itab_list;
    public static readonly int vendor = VENDOR_INTEL;
    public static readonly ZygoteInstruction ie_invalid = new("invalid", O_NONE, O_NONE, O_NONE, P_none);
    public static readonly ZygoteInstruction ie_pause = new("pause", O_NONE, O_NONE, O_NONE, P_none);
    public static readonly ZygoteInstruction ie_nop = new("nop", O_NONE, O_NONE, O_NONE, P_none);
    
    public static Instruction Decode(ReversibleStream input, int mode)
    {
        var instruction = new Instruction();
        GetPrefixes(mode, input, instruction);
        SearchTable(mode, input, instruction);
        DoMode(mode, input, instruction);
        DisassembleOperands(mode, input, instruction);
        ResolveOperator(mode, input, instruction);
        instruction.Length = input.Counter;
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
                inst.prefix.rex = curr;
            else
            {
                if (curr == 0x2E)
                {
                    inst.prefix.seg = "cs";
                    inst.prefix.rex = 0;
                }
                else if (curr == 0x36)
                {
                    inst.prefix.seg = "ss";
                    inst.prefix.rex = 0;
                }
                else if (curr == 0x3E)
                {
                    inst.prefix.seg = "ds";
                    inst.prefix.rex = 0;
                }
                else if (curr == 0x26)
                {
                    inst.prefix.seg = "es";
                    inst.prefix.rex = 0;
                }
                else if (curr == 0x64)
                {
                    inst.prefix.seg = "fs";
                    inst.prefix.rex = 0;
                }
                else if (curr == 0x65)
                {
                    inst.prefix.seg = "gs";
                    inst.prefix.rex = 0;
                }
                else if (curr == 0x67) //adress-size override prefix
                {
                    inst.prefix.adr = 0x67;
                    inst.prefix.rex = 0;
                }
                else if (curr == 0xF0)
                {
                    inst.prefix._lock = 0xF0;
                    inst.prefix.rex = 0;
                }
                else if (curr == 0x66)
                {
                    // the 0x66 sse prefix is only effective if no other sse prefix
                    // has already been specified.
                    if (inst.prefix.insn == 0)
                        inst.prefix.insn = 0x66;
                    inst.prefix.opr = 0x66;
                    inst.prefix.rex = 0;
                }
                else if (curr == 0xF2)
                {
                    inst.prefix.insn = 0xF2;
                    inst.prefix.repne = 0xF2;
                    inst.prefix.rex = 0;
                }
                else if (curr == 0xF3)
                {
                    inst.prefix.insn = 0xF3;
                    inst.prefix.rep = 0xF3;
                    inst.prefix.repe = 0xF3;
                    inst.prefix.rex = 0;
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
                inst.operand_mode = REX_W(inst.prefix.rex) != 0 ? 64 : inst.prefix.opr != 0 ? 16 : P_DEF64(inst.zygote.prefix) != 0 ? 64 : 32;
                inst.address_mode = inst.prefix.adr != 0 ? 32 : 64;
                break;
            case 32:
                inst.operand_mode = inst.prefix.opr != 0 ? 16 : 32;
                inst.address_mode = inst.prefix.adr != 0 ? 16 : 32;
                break;
            case 16:
                inst.operand_mode = inst.prefix.opr != 0 ? 32 : 16;
                inst.address_mode = inst.prefix.adr != 0 ? 32 : 16;
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
        ZygoteInstruction e;

        // resolve xchg, nop, pause crazyness
        if (0x90 == curr)
        {
            if (!((mode == 64) && (REX_B(inst.prefix.rex) != 0)))
            {
                if (inst.prefix.rep != 0)
                {
                    inst.prefix.rep = 0;
                    e = ie_pause;
                }
                else
                    e = ie_nop;
                inst.zygote = e;
                inst.opcode = inst.zygote.opcode;
                return;
            }
        }
        else if (curr == 0x0F)
        {
            table = ITAB__0F;
            curr = input.Current;
            input.Forward();

            // 2byte opcodes can be modified by 0x66, F3, and F2 prefixes
            if (0x66 == inst.prefix.insn)
            {
                if (itab[ITAB__PFX_SSE66__0F][curr].opcode!=("invalid"))
                {
                    table = ITAB__PFX_SSE66__0F;
                    //inst.pfx.opr = 0;
                }
            }
            else if (0xF2 == inst.prefix.insn)
            {
                if (itab[ITAB__PFX_SSEF2__0F][curr].opcode!=("invalid"))
                {
                    table = ITAB__PFX_SSEF2__0F;
                    inst.prefix.repne = 0;
                }
            }
            else if (0xF3 == inst.prefix.insn)
            {
                if (itab[ITAB__PFX_SSEF3__0F][curr].opcode!=("invalid"))
                {
                    table = ITAB__PFX_SSEF3__0F;
                    inst.prefix.repe = 0;
                    inst.prefix.rep = 0;
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
            if (ops.Contains(e.opcode))
            {
                if (e.opcode==("invalid"))
                    if (did_peek)
                        input.Forward();
                inst.zygote = e;
                inst.opcode = e.opcode;
                return;
            }

            table = e.prefix;

            switch (e.opcode)
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
                    index = inst.operand_mode == 64 ? ITAB__MODE_INDX__64 : inst.operand_mode == 32 ? ITAB__MODE_INDX__32 : ITAB__MODE_INDX__16;
                    break;
                case "grp_asize":
                    index = inst.address_mode == 64 ? ITAB__MODE_INDX__64 : inst.address_mode == 32 ? ITAB__MODE_INDX__32 : ITAB__MODE_INDX__16;
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
                if (P_INV64(inst.zygote.prefix) != 0)
                    throw new InvalidOperationException("Invalid instruction");

                // effective rex prefix is the  effective mask for the 
                // instruction hard-coded in the opcode map.
                inst.prefix.rex = inst.prefix.rex & 0x40
                                | inst.prefix.rex & REX_PFX_MASK(inst.zygote.prefix);

                // calculate effective operand size 
                inst.operand_mode = REX_W(inst.prefix.rex) != 0 || P_DEF64(inst.zygote.prefix) != 0 ? 64 : inst.prefix.opr != 0 ? 16 : 32;

                // calculate effective address size
                inst.address_mode = inst.prefix.adr != 0 ? 32 : 64;
                break;
            case 32:
                inst.operand_mode = inst.prefix.opr != 0 ? 16 : 32;
                inst.address_mode = inst.prefix.adr != 0 ? 16 : 32;
                break;
            case 16:
                inst.operand_mode = inst.prefix.opr != 0 ? 32 : 16;
                inst.address_mode = inst.prefix.adr != 0 ? 32 : 16;
                break;
        }
    }

    private static void ResolveOperator(int mode, ReversibleStream input, Instruction inst)
    {
        // far/near flags 
        inst.branch_dist = null;
        // readjust operand sizes for call/jmp instrcutions 
        if (inst.opcode==("call") || inst.opcode==("jmp"))
        {
            if (inst.operand[0].size == SZ_WP)
            {
                // WP: 16bit pointer 
                inst.operand[0].size = 16;
                inst.branch_dist = "far";
            }
            else if (inst.operand[0].size == SZ_DP)
            {
                // DP: 32bit pointer
                inst.operand[0].size = 32;
                inst.branch_dist = "far";
            }
            else if (inst.operand[0].size == 8)
                inst.branch_dist = "near";
        }
        else if (inst.opcode==("3dnow"))
        {
            // resolve 3dnow weirdness 
            inst.opcode = itab[ITAB__3DNOW][input.Current].opcode;
        }
        // SWAPGS is only valid in 64bits mode
        if ((inst.opcode==("swapgs")) && (mode != 64))
            throw new InvalidOperationException("SWAPGS only valid in 64 bit mode");
    }

    private static void DisassembleOperands(int mode, ReversibleStream input, Instruction inst)
    {
        // get type
        var mopt = new int[inst.zygote.operand.Length];
        for (int i = 0; i < mopt.Length; i++)
            mopt[i] = inst.zygote.operand[i].type;
        // get size
        var mops = new int[inst.zygote.operand.Length];
        for (int i = 0; i < mops.Length; i++)
            mops[i] = inst.zygote.operand[i].size;

        if (mopt[2] != OP_NONE)
            inst.operand = [new Operand(), new Operand(), new Operand()];
        else if (mopt[1] != OP_NONE)
            inst.operand = [new Operand(), new Operand()];
        else if (mopt[0] != OP_NONE)
            inst.operand = [new Operand()];

        // These flags determine which operand to apply the operand size
        // cast to.
        if (inst.operand.Length > 0)
            inst.operand[0].cast = P_C0(inst.zygote.prefix);
        if (inst.operand.Length > 1)
            inst.operand[1].cast = P_C1(inst.zygote.prefix);
        if (inst.operand.Length > 2)
            inst.operand[2].cast = P_C2(inst.zygote.prefix);

        // iop = instruction operand 
        //iop = inst.operand

        if (mopt[0] == OP_A)
            DecodeA(mode, inst, input, inst.operand[0]);
        // M[b] ... 
        // E, G/P/V/I/CL/1/S 
        else if ((mopt[0] == OP_M) || (mopt[0] == OP_E))
        {
            if ((mopt[0] == OP_M) && (MODRM_MOD(input.Current) == 3))
                throw new InvalidOperationException("");
            if (mopt[1] == OP_G)
            {
                DecodeModRm(mode, inst, input, inst.operand[0], mops[0], "T_GPR", inst.operand[1], mops[1], "T_GPR");
                if (mopt[2] == OP_I)
                    DecodeImm(mode, inst, input, mops[2], inst.operand[2]);
                else if (mopt[2] == OP_CL)
                {
                    inst.operand[2].type = "OP_REG";
                    inst.operand[2]._base = "cl";
                    inst.operand[2].size = 8;
                }
            }
            else if (mopt[1] == OP_P)
                DecodeModRm(mode, inst, input, inst.operand[0], mops[0], "T_GPR", inst.operand[1], mops[1], "T_MMX");
            else if (mopt[1] == OP_V)
                DecodeModRm(mode, inst, input, inst.operand[0], mops[0], "T_GPR", inst.operand[1], mops[1], "T_XMM");
            else if (mopt[1] == OP_S)
                DecodeModRm(mode, inst, input, inst.operand[0], mops[0], "T_GPR", inst.operand[1], mops[1], "T_SEG");
            else
            {
                DecodeModRm(mode, inst, input, inst.operand[0], mops[0], "T_GPR", null, 0, "T_NONE");
                if (mopt[1] == OP_CL)
                {
                    inst.operand[1].type = "OP_REG";
                    inst.operand[1]._base = "cl";
                    inst.operand[1].size = 8;
                }
                else if (mopt[1] == OP_I1)
                {
                    inst.operand[1].type = "OP_IMM";
                    inst.operand[1].lval = 1;
                }
                else if (mopt[1] == OP_I)
                    DecodeImm(mode, inst, input, mops[1], inst.operand[1]);
            }
        }
        // G, E/PR[,I]/VR 
        else if (mopt[0] == OP_G)
        {
            if (mopt[1] == OP_M)
            {
                if (MODRM_MOD(input.Current) == 3)
                    throw new InvalidOperationException("invalid");
                DecodeModRm(mode, inst, input, inst.operand[1], mops[1], "T_GPR", inst.operand[0], mops[0], "T_GPR");
            }
            else if (mopt[1] == OP_E)
            {
                DecodeModRm(mode, inst, input, inst.operand[1], mops[1], "T_GPR", inst.operand[0], mops[0], "T_GPR");
                if (mopt[2] == OP_I)
                    DecodeImm(mode, inst, input, mops[2], inst.operand[2]);
            }
            else if (mopt[1] == OP_PR)
            {
                DecodeModRm(mode, inst, input, inst.operand[1], mops[1], "T_MMX", inst.operand[0], mops[0], "T_GPR");
                if (mopt[2] == OP_I)
                    DecodeImm(mode, inst, input, mops[2], inst.operand[2]);
            }
            else if (mopt[1] == OP_VR)
            {
                if (MODRM_MOD(input.Current) != 3)
                    throw new InvalidOperationException("Invalid instruction");
                DecodeModRm(mode, inst, input, inst.operand[1], mops[1], "T_XMM", inst.operand[0], mops[0], "T_GPR");
            }
            else if (mopt[1] == OP_W)
                DecodeModRm(mode, inst, input, inst.operand[1], mops[1], "T_XMM", inst.operand[0], mops[0], "T_GPR");
        }
        // AL..BH, I/O/DX 
        else if (ops8.Contains(mopt[0]))
        {
            inst.operand[0].type = "OP_REG";
            inst.operand[0]._base = GPR[("8")][(mopt[0] - OP_AL)];
            inst.operand[0].size = 8;

            if (mopt[1] == OP_I)
                DecodeImm(mode, inst, input, mops[1], inst.operand[1]);
            else if (mopt[1] == OP_DX)
            {
                inst.operand[1].type = "OP_REG";
                inst.operand[1]._base = "dx";
                inst.operand[1].size = 16;
            }
            else if (mopt[1] == OP_O)
                DecodeO(mode, inst, input, mops[1], inst.operand[1]);
        }
        // rAX[r8]..rDI[r15], I/rAX..rDI/O
        else if (ops2.Contains(mopt[0]))
        {
            inst.operand[0].type = "OP_REG";
            inst.operand[0]._base = ResolveGpr64(mode, inst, mopt[0]);

            if (mopt[1] == OP_I)
                DecodeImm(mode, inst, input, mops[1], inst.operand[1]);
            else if (ops64.Contains(mopt[1]))
            {
                inst.operand[1].type = "OP_REG";
                inst.operand[1]._base = ResolveGpr64(mode, inst, mopt[1]);
            }
            else if (mopt[1] == OP_O)
            {
                DecodeO(mode, inst, input, mops[1], inst.operand[1]);
                inst.operand[0].size = ResolveOperandSize(mode, inst, mops[1]);
            }
        }
        else if (ops3.Contains(mopt[0]))
        {
            int gpr = (mopt[0] - OP_ALr8b + (REX_B(inst.prefix.rex) << 3));
            /*if ((gpr in ["ah",	"ch",	"dh",	"bh",
              "spl",	"bpl",	"sil",	"dil",
              "r8b",	"r9b",	"r10b",	"r11b",
              "r12b",	"r13b",	"r14b",	"r15b",
                         ]) && (inst.pfx.rex != 0)) 
                         gpr = gpr + 4;*/
            inst.operand[0].type = "OP_REG";
            inst.operand[0]._base = GPR[("8")][gpr];
            if (mopt[1] == OP_I)
                DecodeImm(mode, inst, input, mops[1], inst.operand[1]);
        }
        // eAX..eDX, DX/I 
        else if (ops32.Contains(mopt[0]))
        {
            inst.operand[0].type = "OP_REG";
            inst.operand[0]._base = ResolveGpr32(inst, mopt[0]);
            if (mopt[1] == OP_DX)
            {
                inst.operand[1].type = "OP_REG";
                inst.operand[1]._base = "dx";
                inst.operand[1].size = 16;
            }
            else if (mopt[1] == OP_I)
                DecodeImm(mode, inst, input, mops[1], inst.operand[1]);
        }
        // ES..GS 
        else if (ops_segs.Contains(mopt[0]))
        {
            // in 64bits mode, only fs and gs are allowed 
            if (mode == 64)
                if ((mopt[0] != OP_FS) && (mopt[0] != OP_GS))
                    throw new InvalidOperationException("only fs and gs allowed in 64 bit mode");
            inst.operand[0].type = "OP_REG";
            inst.operand[0]._base = GPR[("T_SEG")][(mopt[0] - OP_ES)];
            inst.operand[0].size = 16;
        }
        // J 
        else if (mopt[0] == OP_J)
        {
            DecodeImm(mode, inst, input, mops[0], inst.operand[0]);
            // MK take care of signs
            long bound = 1L << (inst.operand[0].size - 1);
            if (inst.operand[0].lval > bound)
                inst.operand[0].lval = -(((2 * bound) - inst.operand[0].lval) % bound);
            inst.operand[0].type = "OP_JIMM";
        }
        // PR, I 
        else if (mopt[0] == OP_PR)
        {
            if (MODRM_MOD(input.Current) != 3)
                throw new InvalidOperationException("Invalid instruction");
            DecodeModRm(mode, inst, input, inst.operand[0], mops[0], "T_MMX", null, 0, "T_NONE");
            if (mopt[1] == OP_I)
                DecodeImm(mode, inst, input, mops[1], inst.operand[1]);
        }
        // VR, I 
        else if (mopt[0] == OP_VR)
        {
            if (MODRM_MOD(input.Current) != 3)
                throw new InvalidOperationException("Invalid instruction");
            DecodeModRm(mode, inst, input, inst.operand[0], mops[0], "T_XMM", null, 0, "T_NONE");
            if (mopt[1] == OP_I)
                DecodeImm(mode, inst, input, mops[1], inst.operand[1]);
        }
        // P, Q[,I]/W/E[,I],VR 
        else if (mopt[0] == OP_P)
        {
            if (mopt[1] == OP_Q)
            {
                DecodeModRm(mode, inst, input, inst.operand[1], mops[1], "T_MMX", inst.operand[0], mops[0], "T_MMX");
                if (mopt[2] == OP_I)
                    DecodeImm(mode, inst, input, mops[2], inst.operand[2]);
            }
            else if (mopt[1] == OP_W)
                DecodeModRm(mode, inst, input, inst.operand[1], mops[1], "T_XMM", inst.operand[0], mops[0], "T_MMX");
            else if (mopt[1] == OP_VR)
            {
                if (MODRM_MOD(input.Current) != 3)
                    throw new InvalidOperationException("Invalid instruction");
                DecodeModRm(mode, inst, input, inst.operand[1], mops[1], "T_XMM", inst.operand[0], mops[0], "T_MMX");
            }
            else if (mopt[1] == OP_E)
            {
                DecodeModRm(mode, inst, input, inst.operand[1], mops[1], "T_GPR", inst.operand[0], mops[0], "T_MMX");
                if (mopt[2] == OP_I)
                    DecodeImm(mode, inst, input, mops[2], inst.operand[2]);
            }
        }
        // R, C/D 
        else if (mopt[0] == OP_R)
        {
            if (mopt[1] == OP_C)
                DecodeModRm(mode, inst, input, inst.operand[0], mops[0], "T_GPR", inst.operand[1], mops[1], "T_CRG");
            else if (mopt[1] == OP_D)
                DecodeModRm(mode, inst, input, inst.operand[0], mops[0], "T_GPR", inst.operand[1], mops[1], "T_DBG");
        }
        // C, R 
        else if (mopt[0] == OP_C)
            DecodeModRm(mode, inst, input, inst.operand[1], mops[1], "T_GPR", inst.operand[0], mops[0], "T_CRG");
        // D, R 
        else if (mopt[0] == OP_D)
            DecodeModRm(mode, inst, input, inst.operand[1], mops[1], "T_GPR", inst.operand[0], mops[0], "T_DBG");
        // Q, P 
        else if (mopt[0] == OP_Q)
            DecodeModRm(mode, inst, input, inst.operand[0], mops[0], "T_MMX", inst.operand[1], mops[1], "T_MMX");
        // S, E 
        else if (mopt[0] == OP_S)
            DecodeModRm(mode, inst, input, inst.operand[1], mops[1], "T_GPR", inst.operand[0], mops[0], "T_SEG");
        // W, V 
        else if (mopt[0] == OP_W)
            DecodeModRm(mode, inst, input, inst.operand[0], mops[0], "T_XMM", inst.operand[1], mops[1], "T_XMM");
        // V, W[,I]/Q/M/E 
        else if (mopt[0] == OP_V)
        {
            if (mopt[1] == OP_W)
            {
                // special cases for movlps and movhps 
                if (MODRM_MOD(input.Current) == 3)
                {
                    if (inst.opcode==("movlps"))
                        inst.opcode = "movhlps";
                    else if (inst.opcode==("movhps"))
                        inst.opcode = "movlhps";
                }
                DecodeModRm(mode, inst, input, inst.operand[1], mops[1], "T_XMM", inst.operand[0], mops[0], "T_XMM");
                if (mopt[2] == OP_I)
                    DecodeImm(mode, inst, input, mops[2], inst.operand[2]);
            }
            else if (mopt[1] == OP_Q)
                DecodeModRm(mode, inst, input, inst.operand[1], mops[1], "T_MMX", inst.operand[0], mops[0], "T_XMM");
            else if (mopt[1] == OP_M)
            {
                if (MODRM_MOD(input.Current) == 3)
                    throw new InvalidOperationException("Invalid instruction");
                DecodeModRm(mode, inst, input, inst.operand[1], mops[1], "T_GPR", inst.operand[0], mops[0], "T_XMM");
            }
            else if (mopt[1] == OP_E)
                DecodeModRm(mode, inst, input, inst.operand[1], mops[1], "T_GPR", inst.operand[0], mops[0], "T_XMM");
            else if (mopt[1] == OP_PR)
                DecodeModRm(mode, inst, input, inst.operand[1], mops[1], "T_MMX", inst.operand[0], mops[0], "T_XMM");
        }
        // DX, eAX/AL
        else if (mopt[0] == OP_DX)
        {
            inst.operand[0].type = "OP_REG";
            inst.operand[0]._base = "dx";
            inst.operand[0].size = 16;

            if (mopt[1] == OP_eAX)
            {
                inst.operand[1].type = "OP_REG";
                inst.operand[1]._base = ResolveGpr32(inst, mopt[1]);
            }
            else if (mopt[1] == OP_AL)
            {
                inst.operand[1].type = "OP_REG";
                inst.operand[1]._base = "al";
                inst.operand[1].size = 8;
            }
        }
        // I, I/AL/eAX
        else if (mopt[0] == OP_I)
        {
            DecodeImm(mode, inst, input, mops[0], inst.operand[0]);
            if (mopt[1] == OP_I)
                DecodeImm(mode, inst, input, mops[1], inst.operand[1]);
            else if (mopt[1] == OP_AL)
            {
                inst.operand[1].type = "OP_REG";
                inst.operand[1]._base = "al";
                inst.operand[1].size = 8;
            }
            else if (mopt[1] == OP_eAX)
            {
                inst.operand[1].type = "OP_REG";
                inst.operand[1]._base = ResolveGpr32(inst, mopt[1]);
            }
        }
        // O, AL/eAX
        else if (mopt[0] == OP_O)
        {
            DecodeO(mode, inst, input, mops[0], inst.operand[0]);
            inst.operand[1].type = "OP_REG";
            inst.operand[1].size = ResolveOperandSize(mode, inst, mops[0]);
            if (mopt[1] == OP_AL)
                inst.operand[1]._base = "al";
            else if (mopt[1] == OP_eAX)
                inst.operand[1]._base = ResolveGpr32(inst, mopt[1]);
            else if (mopt[1] == OP_rAX)
                inst.operand[1]._base = ResolveGpr64(mode, inst, mopt[1]);
        }
        // 3
        else if (mopt[0] == OP_I3)
        {
            inst.operand[0].type = "OP_IMM";
            inst.operand[0].lval = 3;
        }
        // ST(n), ST(n) 
        else if (ops_st.Contains(mopt[0]))
        {
            inst.operand[0].type = "OP_REG";
            inst.operand[0]._base = GPR[("T_ST")][(mopt[0] - OP_ST0)];
            inst.operand[0].size = 0;

            if (ops_st.Contains(mopt[1]))
            {
                inst.operand[1].type = "OP_REG";
                inst.operand[1]._base = GPR[("T_ST")][(mopt[1] - OP_ST0)];
                inst.operand[1].size = 0;
            }
        }
        // AX 
        else if (mopt[0] == OP_AX)
        {
            inst.operand[0].type = "OP_REG";
            inst.operand[0]._base = "ax";
            inst.operand[0].size = 16;
        }
        // none 
        else
            for (int i = 0; i < inst.operand.Length; i++)
                inst.operand[i].type = null;
    }

    private static void DecodeA(int mode, Instruction inst, ReversibleStream input, Operand op)
    {
        //Decodes operands of the type seg:offset.
        if (inst.operand_mode == 16)
        {
            // seg16:off16 
            op.type = "OP_PTR";
            op.size = 32;
            op.dis_start = (int)input.Counter;
            op.ptr = new Pointer(input.Read16(), input.Read16());
        }
        else
        {
            // seg16:off32 
            op.type = "OP_PTR";
            op.size = 48;
            op.dis_start = (int)input.Counter;
            op.ptr = new Pointer(input.Read32(), input.Read16());
        }
    }

    private static void DecodeModRm(int mode, Instruction inst, ReversibleStream input, Operand op, int s, string rm_type, Operand opreg, int reg_size, string reg_type)
    {
        // get mod, r/m and reg fields
        int mod = MODRM_MOD(input.Current);
        int rm = (REX_B(inst.prefix.rex) << 3) | MODRM_RM(input.Current);
        int reg = (REX_R(inst.prefix.rex) << 3) | MODRM_REG(input.Current);

        if (reg_type==("T_DBG") || reg_type==("T_CRG")) // force these to be reg ops (mod is ignored)
            mod = 3;

        op.size = ResolveOperandSize(mode, inst, s);

        // if mod is 11b, then the m specifies a gpr/mmx/sse/control/debug 
        if (mod == 3)
        {
            op.type = "OP_REG";
            op._base = rm_type == "T_GPR" ? DecodeGpr(mode, inst, op.size, rm) : ResolveReg(rm_type, (REX_B(inst.prefix.rex) << 3) | (rm & 7));
        }
        // else its memory addressing 
        else
        {
            op.type = "OP_MEM";
            op.seg = inst.prefix.seg;
            // 64bit addressing 
            if (inst.address_mode == 64)
            {
                op._base = GPR[("64")][(rm)];

                // get offset type
                if (mod == 1)
                    op.offset = 8;
                else if (mod == 2)
                    op.offset = 32;
                else if ((mod == 0) && ((rm & 7) == 5))
                {
                    op._base = "rip";
                    op.offset = 32;
                }
                else
                    op.offset = 0;

                // Scale-Index-Base(SIB)
                if ((rm & 7) == 4)
                {
                    input.Forward();

                    op.scale = (1 << SIB_S(input.Current)) & ~1;
                    op.index = GPR[("64")][((SIB_I(input.Current) | (REX_X(inst.prefix.rex) << 3)))];
                    op._base = GPR[("64")][((SIB_B(input.Current) | (REX_B(inst.prefix.rex) << 3)))];

                    // special conditions for @base reference
                    if (op.index==("rsp"))
                    {
                        op.index = null;
                        op.scale = 0;
                    }

                    if ((op._base==("rbp")) || (op._base==("r13")))
                    {
                        if (mod == 0)
                            op._base = null;
                        if (mod == 1)
                            op.offset = 8;
                        else
                            op.offset = 32;
                    }
                }
            }
            // 32-Bit addressing mode 
            else if (inst.address_mode == 32)
            {
                // get @base 
                op._base = GPR[("32")][(rm)];

                // get offset type 
                if (mod == 1)
                    op.offset = 8;
                else if (mod == 2)
                    op.offset = 32;
                else if ((mod == 0) && (rm == 5))
                {
                    op._base = null;
                    op.offset = 32;
                }
                else
                    op.offset = 0;

                // Scale-Index-Base(SIB)
                if ((rm & 7) == 4)
                {
                    input.Forward();

                    op.scale = (1 << SIB_S(input.Current)) & ~1;
                    op.index = GPR[("32")][(SIB_I(input.Current) | (REX_X(inst.prefix.rex) << 3))];
                    op._base = GPR[("32")][(SIB_B(input.Current) | (REX_B(inst.prefix.rex) << 3))];

                    if (op.index==("esp"))
                    {
                        op.index = null;
                        op.scale = 0;
                    }

                    // special condition for @base reference 
                    if (op._base==("ebp"))
                    {
                        if (mod == 0)
                            op._base = null;
                        if (mod == 1)
                            op.offset = 8;
                        else
                            op.offset = 32;
                    }
                }
            }
            // 16bit addressing mode 
            else
            {
                switch (rm)
                {
                    case 0:
                        op._base = "bx";
                        op.index = "si";
                        break;
                    case 1:
                        op._base = "bx";
                        op.index = "di";
                        break;
                    case 2:
                        op._base = "bp";
                        op.index = "si";
                        break;
                    case 3:
                        op._base = "bp";
                        op.index = "di";
                        break;
                    case 4:
                        op._base = "si";
                        break;
                    case 5:
                        op._base = "di";
                        break;
                    case 6:
                        op._base = "bp";
                        break;
                    case 7:
                        op._base = "bx";
                        break;
                }

                if ((mod == 0) && (rm == 6))
                {
                    op.offset = 16;
                    op._base = null;
                }
                else if (mod == 1)
                    op.offset = 8;
                else if (mod == 2)
                    op.offset = 16;
            }
        }
        input.Forward();
        // extract offset, if any 
        if ((op.offset == 8) || (op.offset == 16) || (op.offset == 32) || (op.offset == 64))
        {
            op.dis_start = (int)input.Counter;
            op.lval = input.Read(op.offset);
            long bound = 1L << (int)(op.offset - 1);
            if (op.lval > bound)
                op.lval = -(((2 * bound) - op.lval) % bound);
        }

        // resolve register encoded in reg field
        if (opreg != null)
        {
            opreg.type = "OP_REG";
            opreg.size = ResolveOperandSize(mode, inst, reg_size);
            opreg._base = reg_type ==("T_GPR") ? DecodeGpr(mode, inst, opreg.size, reg) : ResolveReg(reg_type, reg);
        }
    }

    private static void DecodeImm(int mode, Instruction inst, ReversibleStream input, int s, Operand op)
    {
        op.size = ResolveOperandSize(mode, inst, s);
        op.type = "OP_IMM";
        op.imm_start = (int)input.Counter;
        op.lval = input.Read(op.size);
    }

    private static void DecodeO(int mode, Instruction inst, ReversibleStream input, int s, Operand op)
    {
        // offset
        op.seg = inst.prefix.seg;
        op.offset = inst.address_mode;
        op.dis_start = (int)input.Counter;
        op.lval = input.Read(inst.address_mode);
        op.type = "OP_MEM";
        op.size = ResolveOperandSize(mode, inst, s);
    }

    private static string ResolveGpr32(Instruction inst, int gpr_op)
    {
        int index = gpr_op - OP_eAX;
        return inst.operand_mode == 16 ? GPR[("16")][(index)] : GPR[("32")][(index)];
    }

    private static string? ResolveGpr64(int mode, Instruction inst, int gpr_op)
    {
        int index = (OP_rAXr8 <= gpr_op) && (OP_rDIr15 >= gpr_op) ? (gpr_op - OP_rAXr8) | (REX_B(inst.prefix.rex) << 3) : gpr_op - OP_rAX;
        return inst.operand_mode == 16
            ? GPR[("16")]?[(index)]
            : (mode == 32) || !((inst.operand_mode == 32) && (REX_W(inst.prefix.rex) == 0)) ? GPR[("32")][index] : GPR[("64")][(index)];
    }

    private static int ResolveOperandSize(int mode, Instruction inst, int s) => s == SZ_V
            ? inst.operand_mode
            : s == SZ_Z
            ? inst.operand_mode == 16 ? 16 : 32
            : s == SZ_P
            ? inst.operand_mode == 16 ? SZ_WP : SZ_DP
            : s == SZ_MDQ ? inst.operand_mode == 16 ? 32 : inst.operand_mode : s == SZ_RDQ ? mode == 64 ? 64 : 32 : s;

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
            && (inst.prefix.rex != 0) ? rm >= 4 ? GPR[("8")][(rm + 4)] : GPR[("8")][(rm)] : GPR[("8")][(rm)] : null;
    }

    private static string? ResolveReg(string regtype, int i) 
        => GPR[regtype]?[i];
}
