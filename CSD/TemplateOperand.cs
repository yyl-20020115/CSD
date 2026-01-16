namespace CSD;

public class TemplateOperand(int Type, int Size)
{
    public static readonly int VENDOR_INTEL = 0;
    public static readonly int VENDOR_AMD = 1;
    public static readonly int MAX_INSTRUCTION_LENGTH = 15;

    public static readonly Dictionary<string, List<string>?> GPR = [];

    static TemplateOperand()
    {
        GPR.Add("T_NONE", null);
        GPR.Add("8", [
        "al",   "cl",   "dl",   "bl", 
        "ah",   "ch",   "dh",   "bh",
        "spl",  "bpl",  "sil",  "dil", 
        "r8b",  "r9b",  "r10b", "r11b", 
        "r12b", "r13b", "r14b", "r15b"]);
        GPR.Add("16", [
        "ax",   "cx",   "dx",   "bx",
        "sp",   "bp",   "si",   "di",
        "r8w",  "r9w",  "r10w", "r11w",
        "r12w", "r13w", "r14w", "r15w"]);
        GPR.Add("32", [
        "eax",  "ecx",  "edx",  "ebx",
        "esp",  "ebp",  "esi",  "edi",
        "r8d",  "r9d",  "r10d", "r11d",
        "r12d", "r13d", "r14d", "r15d"]);
        GPR.Add("64", [
        "rax",  "rcx",  "rdx",  "rbx",
        "rsp",  "rbp",  "rsi",  "rdi",
        "r8",   "r9",   "r10",  "r11",
        "r12",  "r13",  "r14",  "r15"]);
        GPR.Add("T_SEG", [
        "es",   "cs",   "ss",   "ds",
        "fs",   "gs"]);
        GPR.Add("T_CRG", [
        "cr0",  "cr1",  "cr2",  "cr3",
        "cr4",  "cr5",  "cr6",  "cr7",
        "cr8",  "cr9",  "cr10", "cr11",
        "cr12", "cr13", "cr14", "cr15"]);
        GPR.Add("T_DBG", [
        "dr0",  "dr1",  "dr2",  "dr3",
        "dr4",  "dr5",  "dr6",  "dr7",
        "dr8",  "dr9",  "dr10", "dr11",
        "dr12", "dr13", "dr14", "dr15"]);
        GPR.Add("T_MMX", [
        "mm0",  "mm1",  "mm2",  "mm3",
        "mm4",  "mm5",  "mm6",  "mm7"]);
        GPR.Add("T_ST", [
        "st0",  "st1",  "st2",  "st3",
        "st4",  "st5",  "st6",  "st7"]);
        GPR.Add("T_XMM", [
        "xmm0",   "xmm1",   "xmm2",     "xmm3",
        "xmm4",   "xmm5",   "xmm6",     "xmm7",
        "xmm8",   "xmm9",   "xmm10",    "xmm11",
        "xmm12",  "xmm13",  "xmm14",    "xmm15"]);
        GPR.Add("IP", ["rip"]);
        GPR.Add("OP", [
        "OP_REG",   "OP_MEM",   "OP_PTR",
        "OP_IMM",   "OP_JIMM",  "OP_CONST"]);
    }

    public static readonly int P_none =    (0);
    public static readonly int P_c1 =      (1 << 0);
    public static readonly int P_rexb =    (1 << 1);
    public static readonly int P_depM =    (1 << 2);
    public static readonly int P_c3 =      (1 << 3);
    public static readonly int P_inv64 =   (1 << 4);
    public static readonly int P_rexw =    (1 << 5);
    public static readonly int P_c2 =      (1 << 6);
    public static readonly int P_def64 =   (1 << 7);
    public static readonly int P_rexr =    (1 << 8);
    public static readonly int P_oso =     (1 << 9);
    public static readonly int P_aso =     (1 << 10);
    public static readonly int P_rexx =    (1 << 11);
    public static readonly int P_ImpAddr = (1 << 12);

    public static int P_C0(int n) => (n >> 0) & 1;

    public static int P_REXB(int n) => (n >> 1) & 1;

    public static int P_DEPM(int n) => (n >> 2) & 1;

    public static int P_C2(int n) => (n >> 3) & 1;

    public static int P_INV64(int n) => (n >> 4) & 1;

    public static int P_REXW(int n) => (n >> 5) & 1;

    public static int P_C1(int n) => (n >> 6) & 1;

    public static int P_DEF64(int n) => (n >> 7) & 1;

    public static int P_REXR(int n) => (n >> 8) & 1;

    public static int P_OSO(int n) => (n >> 9) & 1;

    public static int P_ASO(int n) => (n >> 10) & 1;

    public static int P_REXX(int n) => (n >> 11) & 1;

    public static int P_IMPADDR(int n) => (n >> 12) & 1;

    // rex prefix bits 
    public static int REX_W(int r) => (0xF & r) >> 3;

    public static int REX_R(int r) => (0x7 & r) >> 2;

    public static int REX_X(int r) => (0x3 & r) >> 1;

    public static int REX_B(int r) => (0x1 & r) >> 0;

    public static int REX_PFX_MASK(int n) 
            => ((P_REXW(n) << 3) |
                (P_REXR(n) << 2) |
                (P_REXX(n) << 1) |
                (P_REXB(n) << 0));

    // scable-index-base bits 
    public static int SIB_S(int b) => b >> 6;

    public static int SIB_I(int b) => (b >> 3) & 7;

    public static int SIB_B(int b) => b & 7;

    // modrm bits 
    public static int MODRM_REG(int b) => (b >> 3) & 7;

    public static int MODRM_NNN(int b) => (b >> 3) & 7;

    public static int MODRM_MOD(int b) => (b >> 6) & 3;

    public static int MODRM_RM(int b) => b & 7;

    // operand types
    public static readonly int OP_NONE =   0;

    public static readonly int OP_A =      1;
    public static readonly int OP_E =      2;
    public static readonly int OP_M =      3;
    public static readonly int OP_G =      4 ;
    public static readonly int OP_I =      5;

    public static readonly int OP_AL =     6;
    public static readonly int OP_CL =     7;
    public static readonly int OP_DL =     8;
    public static readonly int OP_BL =     9;
    public static readonly int OP_AH =     10;
    public static readonly int OP_CH =     11;
    public static readonly int OP_DH =     12;
    public static readonly int OP_BH =     13;

    public static readonly int OP_ALr8b =  14;
    public static readonly int OP_CLr9b =  15;
    public static readonly int OP_DLr10b = 16;
    public static readonly int OP_BLr11b = 17;
    public static readonly int OP_AHr12b = 18;
    public static readonly int OP_CHr13b = 19;
    public static readonly int OP_DHr14b = 20;
    public static readonly int OP_BHr15b = 21;

    public static readonly int OP_AX =     22;
    public static readonly int OP_CX =     23;
    public static readonly int OP_DX =     24;
    public static readonly int OP_BX =     25;
    public static readonly int OP_SI =     26;
    public static readonly int OP_DI =     27;
    public static readonly int OP_SP =     28;
    public static readonly int OP_BP =     29;

    public static readonly int OP_rAX =    30;
    public static readonly int OP_rCX =    31;
    public static readonly int OP_rDX =    32;
    public static readonly int OP_rBX =    33;
    public static readonly int OP_rSP =    34;
    public static readonly int OP_rBP =    35;
    public static readonly int OP_rSI =    36;
    public static readonly int OP_rDI =    37;

    public static readonly int OP_rAXr8 =  38;
    public static readonly int OP_rCXr9 =  39;
    public static readonly int OP_rDXr10 = 40;
    public static readonly int OP_rBXr11 = 41;
    public static readonly int OP_rSPr12 = 42;
    public static readonly int OP_rBPr13 = 43;
    public static readonly int OP_rSIr14 = 44;
    public static readonly int OP_rDIr15 = 45;

    public static readonly int OP_eAX =    46;
    public static readonly int OP_eCX =    47;
    public static readonly int OP_eDX =    48;
    public static readonly int OP_eBX =    49;
    public static readonly int OP_eSP =    50;
    public static readonly int OP_eBP =    51;
    public static readonly int OP_eSI =    52;
    public static readonly int OP_eDI =    53;

    public static readonly int OP_ES =     54;
    public static readonly int OP_CS =     55;
    public static readonly int OP_SS =     56;
    public static readonly int OP_DS =     57;
    public static readonly int OP_FS =     58;
    public static readonly int OP_GS =     59;

    public static readonly int OP_ST0 =    60;
    public static readonly int OP_ST1 =    61;
    public static readonly int OP_ST2 =    62;
    public static readonly int OP_ST3 =    63;
    public static readonly int OP_ST4 =    64;
    public static readonly int OP_ST5 =    65;
    public static readonly int OP_ST6 =    66;
    public static readonly int OP_ST7 =    67;

    public static readonly int OP_J =      68;
    public static readonly int OP_S =      69;
    public static readonly int OP_O =      70;
    public static readonly int OP_I1 =     71;
    public static readonly int OP_I3 =     72;
    public static readonly int OP_V =      73;
    public static readonly int OP_W =      74;
    public static readonly int OP_Q =      75;
    public static readonly int OP_P =      76;
    public static readonly int OP_R =      77;
    public static readonly int OP_C =      78;
    public static readonly int OP_D =      79;
    public static readonly int OP_VR =     80;
    public static readonly int OP_PR =     81;

    // operand size constants 
    public static readonly int SZ_NA =     0;
    public static readonly int SZ_Z =      1;
    public static readonly int SZ_V =      2;
    public static readonly int SZ_P =      3;
    public static readonly int SZ_WP =     4;
    public static readonly int SZ_DP =     5;
    public static readonly int SZ_MDQ =    6;
    public static readonly int SZ_RDQ =    7;

    public static readonly int SZ_B =      8;
    public static readonly int SZ_W =      16;
    public static readonly int SZ_D =      32;
    public static readonly int SZ_Q =      64;
    public static readonly int SZ_T =      80;

    public static List<int> ops8 = [OP_AL, OP_CL, OP_DL, OP_BL, OP_AH, OP_CH, OP_DH, OP_BH];

    public static List<int> ops32 = [
            OP_eAX, OP_eCX, OP_eDX, OP_eBX,
            OP_eSP, OP_eBP, OP_eSI, OP_eDI];

    public static List<int> ops64 = [
            OP_rAX, OP_rCX, OP_rDX, OP_rBX,
            OP_rSP, OP_rBP, OP_rSI, OP_rDI];

    public static List<int> ops2 = [
            OP_rAXr8, OP_rCXr9, OP_rDXr10, OP_rBXr11,
            OP_rSPr12, OP_rBPr13, OP_rSIr14, OP_rDIr15,
            OP_rAX, OP_rCX, OP_rDX, OP_rBX,
            OP_rSP, OP_rBP, OP_rSI, OP_rDI];

    public static List<int> ops3 = [
            OP_ALr8b, OP_CLr9b, OP_DLr10b, OP_BLr11b,
            OP_AHr12b, OP_CHr13b, OP_DHr14b, OP_BHr15b];

    public static List<int> ops_st = [
            OP_ST0, OP_ST1, OP_ST2, OP_ST3,
            OP_ST4, OP_ST5, OP_ST6, OP_ST7];

    public static List<int> ops_segs = [
            OP_ES, OP_CS, OP_DS, OP_SS, OP_FS, OP_GS];

    public readonly int type = Type, size = Size;

    // operands
    public static readonly TemplateOperand O_rSPr12 =  new(OP_rSPr12, SZ_NA);
    public static readonly TemplateOperand O_BL =      new (OP_BL, SZ_NA);
    public static readonly TemplateOperand O_BH =      new (OP_BH, SZ_NA);
    public static readonly TemplateOperand O_BP =      new (OP_BP, SZ_NA);
    public static readonly TemplateOperand O_AHr12b =  new (OP_AHr12b, SZ_NA);
    public static readonly TemplateOperand O_BX =      new (OP_BX, SZ_NA);
    public static readonly TemplateOperand O_Jz =      new (OP_J, SZ_Z);
    public static readonly TemplateOperand O_Jv =      new (OP_J, SZ_V);
    public static readonly TemplateOperand O_Jb =      new (OP_J, SZ_B);
    public static readonly TemplateOperand O_rSIr14 =  new (OP_rSIr14, SZ_NA);
    public static readonly TemplateOperand O_GS =      new (OP_GS, SZ_NA);
    public static readonly TemplateOperand O_D =       new (OP_D, SZ_NA);
    public static readonly TemplateOperand O_rBPr13 =  new (OP_rBPr13, SZ_NA);
    public static readonly TemplateOperand O_Ob =      new (OP_O, SZ_B);
    public static readonly TemplateOperand O_P =       new (OP_P, SZ_NA);
    public static readonly TemplateOperand O_Ow =      new (OP_O, SZ_W);
    public static readonly TemplateOperand O_Ov =      new (OP_O, SZ_V);
    public static readonly TemplateOperand O_Gw =      new (OP_G, SZ_W);
    public static readonly TemplateOperand O_Gv =      new (OP_G, SZ_V);
    public static readonly TemplateOperand O_rDX =     new (OP_rDX, SZ_NA);
    public static readonly TemplateOperand O_Gx =      new (OP_G, SZ_MDQ);
    public static readonly TemplateOperand O_Gd =      new (OP_G, SZ_D);
    public static readonly TemplateOperand O_Gb =      new (OP_G, SZ_B);
    public static readonly TemplateOperand O_rBXr11 =  new (OP_rBXr11, SZ_NA);
    public static readonly TemplateOperand O_rDI =     new (OP_rDI, SZ_NA);
    public static readonly TemplateOperand O_rSI =     new (OP_rSI, SZ_NA);
    public static readonly TemplateOperand O_ALr8b =   new (OP_ALr8b, SZ_NA);
    public static readonly TemplateOperand O_eDI =     new (OP_eDI, SZ_NA);
    public static readonly TemplateOperand O_Gz =      new (OP_G, SZ_Z);
    public static readonly TemplateOperand O_eDX =     new (OP_eDX, SZ_NA);
    public static readonly TemplateOperand O_DHr14b =  new (OP_DHr14b, SZ_NA);
    public static readonly TemplateOperand O_rSP =     new (OP_rSP, SZ_NA);
    public static readonly TemplateOperand O_PR =      new (OP_PR, SZ_NA);
    public static readonly TemplateOperand O_NONE =    new (OP_NONE, SZ_NA);
    public static readonly TemplateOperand O_rCX =     new (OP_rCX, SZ_NA);
    public static readonly TemplateOperand O_jWP =     new (OP_J, SZ_WP);
    public static readonly TemplateOperand O_rDXr10 =  new (OP_rDXr10, SZ_NA);
    public static readonly TemplateOperand O_Md =      new (OP_M, SZ_D);
    public static readonly TemplateOperand O_C =       new (OP_C, SZ_NA);
    public static readonly TemplateOperand O_G =       new (OP_G, SZ_NA);
    public static readonly TemplateOperand O_Mb =      new (OP_M, SZ_B);
    public static readonly TemplateOperand O_Mt =      new (OP_M, SZ_T);
    public static readonly TemplateOperand O_S =       new (OP_S, SZ_NA);
    public static readonly TemplateOperand O_Mq =      new (OP_M, SZ_Q);
    public static readonly TemplateOperand O_W =       new (OP_W, SZ_NA);
    public static readonly TemplateOperand O_ES =      new (OP_ES, SZ_NA);
    public static readonly TemplateOperand O_rBX =     new (OP_rBX, SZ_NA);
    public static readonly TemplateOperand O_Ed =      new (OP_E, SZ_D);
    public static readonly TemplateOperand O_DLr10b =  new (OP_DLr10b, SZ_NA);
    public static readonly TemplateOperand O_Mw =      new (OP_M, SZ_W);
    public static readonly TemplateOperand O_Eb =      new (OP_E, SZ_B);
    public static readonly TemplateOperand O_Ex =      new (OP_E, SZ_MDQ);
    public static readonly TemplateOperand O_Ez =      new (OP_E, SZ_Z);
    public static readonly TemplateOperand O_Ew =      new (OP_E, SZ_W);
    public static readonly TemplateOperand O_Ev =      new (OP_E, SZ_V);
    public static readonly TemplateOperand O_Ep =      new (OP_E, SZ_P);
    public static readonly TemplateOperand O_FS =      new (OP_FS, SZ_NA);
    public static readonly TemplateOperand O_Ms =      new (OP_M, SZ_W);
    public static readonly TemplateOperand O_rAXr8 =   new (OP_rAXr8, SZ_NA);
    public static readonly TemplateOperand O_eBP =     new (OP_eBP, SZ_NA);
    public static readonly TemplateOperand O_Isb =     new (OP_I, SZ_B);
    public static readonly TemplateOperand O_eBX =     new (OP_eBX, SZ_NA);
    public static readonly TemplateOperand O_rCXr9 =   new (OP_rCXr9, SZ_NA);
    public static readonly TemplateOperand O_jDP =     new (OP_J, SZ_DP);
    public static readonly TemplateOperand O_CH =      new (OP_CH, SZ_NA);
    public static readonly TemplateOperand O_CL =      new (OP_CL, SZ_NA);
    public static readonly TemplateOperand O_R =       new (OP_R, SZ_RDQ);
    public static readonly TemplateOperand O_V =       new (OP_V, SZ_NA);
    public static readonly TemplateOperand O_CS =      new (OP_CS, SZ_NA);
    public static readonly TemplateOperand O_CHr13b =  new (OP_CHr13b, SZ_NA);
    public static readonly TemplateOperand O_eCX =     new (OP_eCX, SZ_NA);
    public static readonly TemplateOperand O_eSP =     new (OP_eSP, SZ_NA);
    public static readonly TemplateOperand O_SS =      new (OP_SS, SZ_NA);
    public static readonly TemplateOperand O_SP =      new (OP_SP, SZ_NA);
    public static readonly TemplateOperand O_BLr11b =  new (OP_BLr11b, SZ_NA);
    public static readonly TemplateOperand O_SI =      new (OP_SI, SZ_NA);
    public static readonly TemplateOperand O_eSI =     new (OP_eSI, SZ_NA);
    public static readonly TemplateOperand O_DL =      new (OP_DL, SZ_NA);
    public static readonly TemplateOperand O_DH =      new (OP_DH, SZ_NA);
    public static readonly TemplateOperand O_DI =      new (OP_DI, SZ_NA);
    public static readonly TemplateOperand O_DX =      new (OP_DX, SZ_NA);
    public static readonly TemplateOperand O_rBP =     new (OP_rBP, SZ_NA);
    public static readonly TemplateOperand O_Gvw =     new (OP_G, SZ_MDQ);
    public static readonly TemplateOperand O_I1 =      new (OP_I1, SZ_NA);
    public static readonly TemplateOperand O_I3 =      new (OP_I3, SZ_NA);
    public static readonly TemplateOperand O_DS =      new (OP_DS, SZ_NA);
    public static readonly TemplateOperand O_ST4 =     new (OP_ST4, SZ_NA);
    public static readonly TemplateOperand O_ST5 =     new (OP_ST5, SZ_NA);
    public static readonly TemplateOperand O_ST6 =     new (OP_ST6, SZ_NA);
    public static readonly TemplateOperand O_ST7 =     new (OP_ST7, SZ_NA);
    public static readonly TemplateOperand O_ST0 =     new (OP_ST0, SZ_NA);
    public static readonly TemplateOperand O_ST1 =     new (OP_ST1, SZ_NA);
    public static readonly TemplateOperand O_ST2 =     new (OP_ST2, SZ_NA);
    public static readonly TemplateOperand O_ST3 =     new (OP_ST3, SZ_NA);
    public static readonly TemplateOperand O_E =       new (OP_E, SZ_NA);
    public static readonly TemplateOperand O_AH =      new (OP_AH, SZ_NA);
    public static readonly TemplateOperand O_M =       new (OP_M, SZ_NA);
    public static readonly TemplateOperand O_AL =      new (OP_AL, SZ_NA);
    public static readonly TemplateOperand O_CLr9b =   new (OP_CLr9b, SZ_NA);
    public static readonly TemplateOperand O_Q =       new (OP_Q, SZ_NA);
    public static readonly TemplateOperand O_eAX =     new (OP_eAX, SZ_NA);
    public static readonly TemplateOperand O_VR =      new (OP_VR, SZ_NA);
    public static readonly TemplateOperand O_AX =      new (OP_AX, SZ_NA);
    public static readonly TemplateOperand O_rAX =     new (OP_rAX, SZ_NA);
    public static readonly TemplateOperand O_Iz =      new (OP_I, SZ_Z);
    public static readonly TemplateOperand O_rDIr15 =  new (OP_rDIr15, SZ_NA);
    public static readonly TemplateOperand O_Iw =      new (OP_I, SZ_W);
    public static readonly TemplateOperand O_Iv =      new (OP_I, SZ_V);
    public static readonly TemplateOperand O_Ap =      new (OP_A, SZ_P);
    public static readonly TemplateOperand O_CX =      new (OP_CX, SZ_NA);
    public static readonly TemplateOperand O_Ib =      new (OP_I, SZ_B);
    public static readonly TemplateOperand O_BHr15b =  new (OP_BHr15b, SZ_NA);

}