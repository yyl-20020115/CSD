namespace CSD;

using static ZygoteOperand;
using static Table;
public static class Dissassembler
{

    public static readonly ZygoteInstruction[][] itab = Table.itab_list;
    public static readonly int vendor = VENDOR_INTEL;
    public static readonly ZygoteInstruction ie_invalid = new("invalid", O_NONE, O_NONE, O_NONE, P_none);
    public static readonly ZygoteInstruction ie_pause = new("pause", O_NONE, O_NONE, O_NONE, P_none);
    public static readonly ZygoteInstruction ie_nop = new("nop", O_NONE, O_NONE, O_NONE, P_none);

    
    public static Instruction Decode(ReversibleInputStream input, int mode)
    {
        input.ResetCounter();
        var instruction = new Instruction();
        GetPrefixes(mode, input, instruction);
        SearchTable(mode, input, instruction);
        DoMode(mode, input, instruction);
        DisasmOperands(mode, input, instruction);
        ResolveOperator(mode, input, instruction);
        instruction.x86Length = input.Counter;
        return instruction;
    }


    private static void GetPrefixes(int mode, ReversibleInputStream input, Instruction inst)
    {
        int curr;
        int i = 0;
        while (true)
        {
            curr = input.Byte;
            input.Forward();
            i++;

            if ((mode == 64) && ((curr & 0xF0) == 0x40))
                inst.pfx.rex = curr;
            else
            {
                if (curr == 0x2E)
                {
                    inst.pfx.seg = "cs";
                    inst.pfx.rex = 0;
                }
                else if (curr == 0x36)
                {
                    inst.pfx.seg = "ss";
                    inst.pfx.rex = 0;
                }
                else if (curr == 0x3E)
                {
                    inst.pfx.seg = "ds";
                    inst.pfx.rex = 0;
                }
                else if (curr == 0x26)
                {
                    inst.pfx.seg = "es";
                    inst.pfx.rex = 0;
                }
                else if (curr == 0x64)
                {
                    inst.pfx.seg = "fs";
                    inst.pfx.rex = 0;
                }
                else if (curr == 0x65)
                {
                    inst.pfx.seg = "gs";
                    inst.pfx.rex = 0;
                }
                else if (curr == 0x67) //adress-size override prefix
                {
                    inst.pfx.adr = 0x67;
                    inst.pfx.rex = 0;
                }
                else if (curr == 0xF0)
                {
                    inst.pfx._lock = 0xF0;
                    inst.pfx.rex = 0;
                }
                else if (curr == 0x66)
                {
                    // the 0x66 sse prefix is only effective if no other sse prefix
                    // has already been specified.
                    if (inst.pfx.insn == 0)
                        inst.pfx.insn = 0x66;
                    inst.pfx.opr = 0x66;
                    inst.pfx.rex = 0;
                }
                else if (curr == 0xF2)
                {
                    inst.pfx.insn = 0xF2;
                    inst.pfx.repne = 0xF2;
                    inst.pfx.rex = 0;
                }
                else if (curr == 0xF3)
                {
                    inst.pfx.insn = 0xF3;
                    inst.pfx.rep = 0xF3;
                    inst.pfx.repe = 0xF3;
                    inst.pfx.rex = 0;
                }
                else
                    //No more prefixes
                    break;
            }
        }
        if (i >= MAX_INSTRUCTION_LENGTH)
            throw new InvalidOperationException("Max instruction Length exceeded");

        input.Reverse();

        // speculatively determine the effective operand mode,
        // based on the prefixes and the current disassembly
        // mode. This may be inaccurate, but useful for mode
        // dependent decoding.
        if (mode == 64)
        {
            inst.opr_mode = REX_W(inst.pfx.rex) != 0 ? 64 : inst.pfx.opr != 0 ? 16 : P_DEF64(inst.zygote.prefix) != 0 ? 64 : 32;
            inst.adr_mode = inst.pfx.adr != 0 ? 32 : 64;
        }
        else if (mode == 32)
        {
            inst.opr_mode = inst.pfx.opr != 0 ? 16 : 32;
            inst.adr_mode = inst.pfx.adr != 0 ? 16 : 32;
        }
        else if (mode == 16)
        {
            inst.opr_mode = inst.pfx.opr != 0 ? 32 : 16;
            inst.adr_mode = inst.pfx.adr != 0 ? 32 : 16;
        }
    }

    private static void SearchTable(int mode, ReversibleInputStream input, Instruction inst)
    {
        bool did_peek = false;
        int peek;
        int curr = input.Byte;
        input.Forward();

        int table = 0;
        ZygoteInstruction e;

        // resolve xchg, nop, pause crazyness
        if (0x90 == curr)
        {
            if (!((mode == 64) && (REX_B(inst.pfx.rex) != 0)))
            {
                if (inst.pfx.rep != 0)
                {
                    inst.pfx.rep = 0;
                    e = ie_pause;
                }
                else
                    e = ie_nop;
                inst.zygote = e;
                inst.op = inst.zygote.op;
                return;
            }
        }
        else if (curr == 0x0F)
        {
            table = ITAB__0F;
            curr = input.Byte;
            input.Forward();

            // 2byte opcodes can be modified by 0x66, F3, and F2 prefixes
            if (0x66 == inst.pfx.insn)
            {
                if (itab[ITAB__PFX_SSE66__0F][curr].op!=("invalid"))
                {
                    table = ITAB__PFX_SSE66__0F;
                    //inst.pfx.opr = 0;
                }
            }
            else if (0xF2 == inst.pfx.insn)
            {
                if (itab[ITAB__PFX_SSEF2__0F][curr].op!=("invalid"))
                {
                    table = ITAB__PFX_SSEF2__0F;
                    inst.pfx.repne = 0;
                }
            }
            else if (0xF3 == inst.pfx.insn)
            {
                if (itab[ITAB__PFX_SSEF3__0F][curr].op!=("invalid"))
                {
                    table = ITAB__PFX_SSEF3__0F;
                    inst.pfx.repe = 0;
                    inst.pfx.rep = 0;
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
            if (ops.Contains(e.op))
            {
                if (e.op==("invalid"))
                    if (did_peek)
                        input.Forward();
                inst.zygote = e;
                inst.op = e.op;
                return;
            }

            table = e.prefix;

            if (e.op==("grp_reg"))
            {
                peek = input.Byte;
                did_peek = true;
                index = MODRM_REG(peek);
            }
            else if (e.op==("grp_mod"))
            {
                peek = input.Byte;
                did_peek = true;
                index = MODRM_MOD(peek);
                index = index == 3 ? ITAB__MOD_INDX__11 : ITAB__MOD_INDX__NOT_11;
            }
            else if (e.op==("grp_rm"))
            {
                curr = input.Byte;
                input.Forward();
                did_peek = false;
                index = MODRM_RM(curr);
            }
            else if (e.op==("grp_x87"))
            {
                curr = input.Byte;
                input.Forward();
                did_peek = false;
                index = curr - 0xC0;
            }
            else if (e.op==("grp_osize"))
            {
                index = inst.opr_mode == 64 ? ITAB__MODE_INDX__64 : inst.opr_mode == 32 ? ITAB__MODE_INDX__32 : ITAB__MODE_INDX__16;
            }
            else if (e.op==("grp_asize"))
            {
                index = inst.adr_mode == 64 ? ITAB__MODE_INDX__64 : inst.adr_mode == 32 ? ITAB__MODE_INDX__32 : ITAB__MODE_INDX__16;
            }
            else if (e.op==("grp_mode"))
            {
                index = mode == 64 ? ITAB__MODE_INDX__64 : mode == 32 ? ITAB__MODE_INDX__32 : ITAB__MODE_INDX__16;
            }
            else if (e.op==("grp_vendor"))
            {
                index = vendor == VENDOR_INTEL
                    ? ITAB__VENDOR_INDX__INTEL
                    : vendor == VENDOR_AMD ? ITAB__VENDOR_INDX__AMD : throw new SystemException("unrecognized vendor id");
            }
            else if (e.op == "d3vil")
            {
                throw new SystemException("invalid instruction @operator constant Id3vil");
            }
            else
            {
                throw new SystemException("invalid instruction @operator constant");
            }
        }
        //inst.zygote = e;
        //inst.@operator = e.@operator;
        //return;
    }

    private static void DoMode(int mode, ReversibleInputStream input, Instruction inst)
    {
        // propagate prefix effects 
        if (mode == 64)  // set 64bit-mode flags
        {
            // Check validity of  instruction m64 
            if ((P_INV64(inst.zygote.prefix) != 0))
                throw new InvalidOperationException("Invalid instruction");

            // effective rex prefix is the  effective mask for the 
            // instruction hard-coded in the opcode map.
            inst.pfx.rex = ((inst.pfx.rex & 0x40)
                            | (inst.pfx.rex & REX_PFX_MASK(inst.zygote.prefix)));

            // calculate effective operand size 
            inst.opr_mode = (REX_W(inst.pfx.rex) != 0) || (P_DEF64(inst.zygote.prefix) != 0) ? 64 : inst.pfx.opr != 0 ? 16 : 32;

            // calculate effective address size
            inst.adr_mode = inst.pfx.adr != 0 ? 32 : 64;
        }
        else if (mode == 32) // set 32bit-mode flags
        {
            inst.opr_mode = inst.pfx.opr != 0 ? 16 : 32;
            inst.adr_mode = inst.pfx.adr != 0 ? 16 : 32;
        }
        else if (mode == 16) // set 16bit-mode flags
        {
            inst.opr_mode = inst.pfx.opr != 0 ? 32 : 16;
            inst.adr_mode = inst.pfx.adr != 0 ? 32 : 16;
        }
    }

    private static void ResolveOperator(int mode, ReversibleInputStream input, Instruction inst)
    {
        // far/near flags 
        inst.branch_dist = null;
        // readjust operand sizes for call/jmp instrcutions 
        if (inst.op==("call") || inst.op==("jmp"))
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
        else if (inst.op==("3dnow"))
        {
            // resolve 3dnow weirdness 
            inst.op = itab[ITAB__3DNOW][input.Byte].op;
        }
        // SWAPGS is only valid in 64bits mode
        if ((inst.op==("swapgs")) && (mode != 64))
            throw new InvalidOperationException("SWAPGS only valid in 64 bit mode");
    }

    private static void DisasmOperands(int mode, ReversibleInputStream input, Instruction inst)
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
            if ((mopt[0] == OP_M) && (MODRM_MOD(input.Byte) == 3))
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
                if (MODRM_MOD(input.Byte) == 3)
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
                if (MODRM_MOD(input.Byte) != 3)
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
            int gpr = (mopt[0] - OP_ALr8b + (REX_B(inst.pfx.rex) << 3));
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
            if (MODRM_MOD(input.Byte) != 3)
                throw new InvalidOperationException("Invalid instruction");
            DecodeModRm(mode, inst, input, inst.operand[0], mops[0], "T_MMX", null, 0, "T_NONE");
            if (mopt[1] == OP_I)
                DecodeImm(mode, inst, input, mops[1], inst.operand[1]);
        }
        // VR, I 
        else if (mopt[0] == OP_VR)
        {
            if (MODRM_MOD(input.Byte) != 3)
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
                if (MODRM_MOD(input.Byte) != 3)
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
                if (MODRM_MOD(input.Byte) == 3)
                {
                    if (inst.op==("movlps"))
                        inst.op = "movhlps";
                    else if (inst.op==("movhps"))
                        inst.op = "movlhps";
                }
                DecodeModRm(mode, inst, input, inst.operand[1], mops[1], "T_XMM", inst.operand[0], mops[0], "T_XMM");
                if (mopt[2] == OP_I)
                    DecodeImm(mode, inst, input, mops[2], inst.operand[2]);
            }
            else if (mopt[1] == OP_Q)
                DecodeModRm(mode, inst, input, inst.operand[1], mops[1], "T_MMX", inst.operand[0], mops[0], "T_XMM");
            else if (mopt[1] == OP_M)
            {
                if (MODRM_MOD(input.Byte) == 3)
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

    private static void DecodeA(int mode, Instruction inst, ReversibleInputStream input, Operand op)
    {
        //Decodes operands of the type seg:offset.
        if (inst.opr_mode == 16)
        {
            // seg16:off16 
            op.type = "OP_PTR";
            op.size = 32;
            op.dis_start = input.Counter;
            op.ptr = new Pointer(input.Read16(), input.Read16());
        }
        else
        {
            // seg16:off32 
            op.type = "OP_PTR";
            op.size = 48;
            op.dis_start = input.Counter;
            op.ptr = new Pointer(input.Read32(), input.Read16());
        }
    }

    private static void DecodeModRm(int mode, Instruction inst, ReversibleInputStream input, Operand op, int s, string rm_type, Operand opreg, int reg_size, string reg_type)
    {
        // get mod, r/m and reg fields
        int mod = MODRM_MOD(input.Byte);
        int rm = (REX_B(inst.pfx.rex) << 3) | MODRM_RM(input.Byte);
        int reg = (REX_R(inst.pfx.rex) << 3) | MODRM_REG(input.Byte);

        if (reg_type==("T_DBG") || reg_type==("T_CRG")) // force these to be reg ops (mod is ignored)
            mod = 3;

        op.size = ResolveOperandSize(mode, inst, s);

        // if mod is 11b, then the m specifies a gpr/mmx/sse/control/debug 
        if (mod == 3)
        {
            op.type = "OP_REG";
            op._base = rm_type == "T_GPR" ? DecodeGpr(mode, inst, op.size, rm) : ResolveReg(rm_type, (REX_B(inst.pfx.rex) << 3) | (rm & 7));
        }
        // else its memory addressing 
        else
        {
            op.type = "OP_MEM";
            op.seg = inst.pfx.seg;
            // 64bit addressing 
            if (inst.adr_mode == 64)
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

                    op.scale = (1 << SIB_S(input.Byte)) & ~1;
                    op.index = GPR[("64")][((SIB_I(input.Byte) | (REX_X(inst.pfx.rex) << 3)))];
                    op._base = GPR[("64")][((SIB_B(input.Byte) | (REX_B(inst.pfx.rex) << 3)))];

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
            else if (inst.adr_mode == 32)
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

                    op.scale = (1 << SIB_S(input.Byte)) & ~1;
                    op.index = GPR[("32")][(SIB_I(input.Byte) | (REX_X(inst.pfx.rex) << 3))];
                    op._base = GPR[("32")][(SIB_B(input.Byte) | (REX_B(inst.pfx.rex) << 3))];

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
                if (rm == 0)
                {
                    op._base = "bx";
                    op.index = "si";
                }
                else if (rm == 1)
                {
                    op._base = "bx";
                    op.index = "di";
                }
                else if (rm == 2)
                {
                    op._base = "bp";
                    op.index = "si";
                }
                else if (rm == 3)
                {
                    op._base = "bp";
                    op.index = "di";
                }
                else if (rm == 4)
                    op._base = "si";
                else if (rm == 5)
                    op._base = "di";
                else if (rm == 6)
                    op._base = "bp";
                else if (rm == 7)
                    op._base = "bx";

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
            op.dis_start = input.Counter;
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

    private static void DecodeImm(int mode, Instruction inst, ReversibleInputStream input, int s, Operand op)
    {
        op.size = ResolveOperandSize(mode, inst, s);
        op.type = "OP_IMM";
        op.imm_start = input.Counter;
        op.lval = input.Read(op.size);
    }

    private static void DecodeO(int mode, Instruction inst, ReversibleInputStream input, int s, Operand op)
    {
        // offset
        op.seg = inst.pfx.seg;
        op.offset = inst.adr_mode;
        op.dis_start = input.Counter;
        op.lval = input.Read(inst.adr_mode);
        op.type = "OP_MEM";
        op.size = ResolveOperandSize(mode, inst, s);
    }

    private static string ResolveGpr32(Instruction inst, int gpr_op)
    {
        int index = gpr_op - OP_eAX;
        if (inst.opr_mode == 16)
            return GPR[("16")][(index)];
        return GPR[("32")][(index)];
    }

    private static string ResolveGpr64(int mode, Instruction inst, int gpr_op)
    {
        int index = (OP_rAXr8 <= gpr_op) && (OP_rDIr15 >= gpr_op) ? (gpr_op - OP_rAXr8) | (REX_B(inst.pfx.rex) << 3) : gpr_op - OP_rAX;
        return inst.opr_mode == 16
            ? GPR[("16")][(index)]
            : (mode == 32) || !((inst.opr_mode == 32) && (REX_W(inst.pfx.rex) == 0)) ? GPR[("32")][index] : GPR[("64")][(index)];
    }

    private static int ResolveOperandSize(int mode, Instruction inst, int s) => s == SZ_V
            ? inst.opr_mode
            : s == SZ_Z
            ? inst.opr_mode == 16 ? 16 : 32
            : s == SZ_P
            ? inst.opr_mode == 16 ? SZ_WP : SZ_DP
            : s == SZ_MDQ ? inst.opr_mode == 16 ? 32 : inst.opr_mode : s == SZ_RDQ ? mode == 64 ? 64 : 32 : s;

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
            && (inst.pfx.rex != 0) ? rm >= 4 ? GPR[("8")][(rm + 4)] : GPR[("8")][(rm)] : GPR[("8")][(rm)] : null;
    }

    private static string ResolveReg(string regtype, int i) 
        => GPR[regtype][i];
}