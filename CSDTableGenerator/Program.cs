using System.Xml.Linq;

namespace CSD;

public static class TableGen
{
    private static readonly HashSet<string> spl_mnm_types = [];
    private static readonly Dictionary<string, string> vend_dict = [];
    private static readonly Dictionary<string, string> mode_dict = [];
    private static readonly Dictionary<string, string[]> operand_dict = [];
    private static readonly Dictionary<string, string> pfx_dict = [];
    private static readonly string default_opr = "O_NONE, O_NONE, O_NONE";

    static TableGen()
    {
        spl_mnm_types.Add("d3vil");
        spl_mnm_types.Add("na");
        spl_mnm_types.Add("grp_reg");
        spl_mnm_types.Add("grp_rm");
        spl_mnm_types.Add("grp_vendor");
        spl_mnm_types.Add("grp_x87");
        spl_mnm_types.Add("grp_mode");
        spl_mnm_types.Add("grp_osize");
        spl_mnm_types.Add("grp_asize");
        spl_mnm_types.Add("grp_mod");
        spl_mnm_types.Add("none");
        vend_dict.Add("AMD", "00");
        vend_dict.Add("INTEL", "01");
        mode_dict.Add("16", "00");
        mode_dict.Add("32", "01");
        mode_dict.Add("64", "02");
        operand_dict.Add("Ap", ["OP_A", "SZ_P"]);
        operand_dict.Add("E", ["OP_E", "SZ_NA"]);
        operand_dict.Add("Eb", ["OP_E", "SZ_B"]);
        operand_dict.Add("Ew", ["OP_E", "SZ_W"]);
        operand_dict.Add("Ev", ["OP_E", "SZ_V"]);
        operand_dict.Add("Ed", ["OP_E", "SZ_D"]);
        operand_dict.Add("Ez", ["OP_E", "SZ_Z"]);
        operand_dict.Add("Ex", ["OP_E", "SZ_MDQ"]);
        operand_dict.Add("Ep", ["OP_E", "SZ_P"]);
        operand_dict.Add("G", ["OP_G", "SZ_NA"]);
        operand_dict.Add("Gb", ["OP_G", "SZ_B"]);
        operand_dict.Add("Gw", ["OP_G", "SZ_W"]);
        operand_dict.Add("Gv", ["OP_G", "SZ_V"]);
        operand_dict.Add("Gvw", ["OP_G", "SZ_MDQ"]);
        operand_dict.Add("Gd", ["OP_G", "SZ_D"]);
        operand_dict.Add("Gx", ["OP_G", "SZ_MDQ"]);
        operand_dict.Add("Gz", ["OP_G", "SZ_Z"]);
        operand_dict.Add("M", ["OP_M", "SZ_NA"]);
        operand_dict.Add("Mb", ["OP_M", "SZ_B"]);
        operand_dict.Add("Mw", ["OP_M", "SZ_W"]);
        operand_dict.Add("Ms", ["OP_M", "SZ_W"]);
        operand_dict.Add("Md", ["OP_M", "SZ_D"]);
        operand_dict.Add("Mq", ["OP_M", "SZ_Q"]);
        operand_dict.Add("Mt", ["OP_M", "SZ_T"]);
        operand_dict.Add("I1", ["OP_I1", "SZ_NA"]);
        operand_dict.Add("I3", ["OP_I3", "SZ_NA"]);
        operand_dict.Add("Ib", ["OP_I", "SZ_B"]);
        operand_dict.Add("Isb", ["OP_I", "SZ_SB"]);
        operand_dict.Add("Iw", ["OP_I", "SZ_W"]);
        operand_dict.Add("Iv", ["OP_I", "SZ_V"]);
        operand_dict.Add("Iz", ["OP_I", "SZ_Z"]);
        operand_dict.Add("Jv", ["OP_J", "SZ_V"]);
        operand_dict.Add("Jz", ["OP_J", "SZ_Z"]);
        operand_dict.Add("Jb", ["OP_J", "SZ_B"]);
        operand_dict.Add("R", ["OP_R", "SZ_RDQ"]);
        operand_dict.Add("C", ["OP_C", "SZ_NA"]);
        operand_dict.Add("D", ["OP_D", "SZ_NA"]);
        operand_dict.Add("S", ["OP_S", "SZ_NA"]);
        operand_dict.Add("Ob", ["OP_O", "SZ_B"]);
        operand_dict.Add("Ow", ["OP_O", "SZ_W"]);
        operand_dict.Add("Ov", ["OP_O", "SZ_V"]);
        operand_dict.Add("V", ["OP_V", "SZ_NA"]);
        operand_dict.Add("W", ["OP_W", "SZ_NA"]);
        operand_dict.Add("P", ["OP_P", "SZ_NA"]);
        operand_dict.Add("Q", ["OP_Q", "SZ_NA"]);
        operand_dict.Add("VR", ["OP_VR", "SZ_NA"]);
        operand_dict.Add("PR", ["OP_PR", "SZ_NA"]);
        operand_dict.Add("AL", ["OP_AL", "SZ_NA"]);
        operand_dict.Add("CL", ["OP_CL", "SZ_NA"]);
        operand_dict.Add("DL", ["OP_DL", "SZ_NA"]);
        operand_dict.Add("BL", ["OP_BL", "SZ_NA"]);
        operand_dict.Add("AH", ["OP_AH", "SZ_NA"]);
        operand_dict.Add("CH", ["OP_CH", "SZ_NA"]);
        operand_dict.Add("DH", ["OP_DH", "SZ_NA"]);
        operand_dict.Add("BH", ["OP_BH", "SZ_NA"]);
        operand_dict.Add("AX", ["OP_AX", "SZ_NA"]);
        operand_dict.Add("CX", ["OP_CX", "SZ_NA"]);
        operand_dict.Add("DX", ["OP_DX", "SZ_NA"]);
        operand_dict.Add("BX", ["OP_BX", "SZ_NA"]);
        operand_dict.Add("SI", ["OP_SI", "SZ_NA"]);
        operand_dict.Add("DI", ["OP_DI", "SZ_NA"]);
        operand_dict.Add("SP", ["OP_SP", "SZ_NA"]);
        operand_dict.Add("BP", ["OP_BP", "SZ_NA"]);
        operand_dict.Add("eAX", ["OP_eAX", "SZ_NA"]);
        operand_dict.Add("eCX", ["OP_eCX", "SZ_NA"]);
        operand_dict.Add("eDX", ["OP_eDX", "SZ_NA"]);
        operand_dict.Add("eBX", ["OP_eBX", "SZ_NA"]);
        operand_dict.Add("eSI", ["OP_eSI", "SZ_NA"]);
        operand_dict.Add("eDI", ["OP_eDI", "SZ_NA"]);
        operand_dict.Add("eSP", ["OP_eSP", "SZ_NA"]);
        operand_dict.Add("eBP", ["OP_eBP", "SZ_NA"]);
        operand_dict.Add("rAX", ["OP_rAX", "SZ_NA"]);
        operand_dict.Add("rCX", ["OP_rCX", "SZ_NA"]);
        operand_dict.Add("rBX", ["OP_rBX", "SZ_NA"]);
        operand_dict.Add("rDX", ["OP_rDX", "SZ_NA"]);
        operand_dict.Add("rSI", ["OP_rSI", "SZ_NA"]);
        operand_dict.Add("rDI", ["OP_rDI", "SZ_NA"]);
        operand_dict.Add("rSP", ["OP_rSP", "SZ_NA"]);
        operand_dict.Add("rBP", ["OP_rBP", "SZ_NA"]);
        operand_dict.Add("ES", ["OP_ES", "SZ_NA"]);
        operand_dict.Add("CS", ["OP_CS", "SZ_NA"]);
        operand_dict.Add("DS", ["OP_DS", "SZ_NA"]);
        operand_dict.Add("SS", ["OP_SS", "SZ_NA"]);
        operand_dict.Add("GS", ["OP_GS", "SZ_NA"]);
        operand_dict.Add("FS", ["OP_FS", "SZ_NA"]);
        operand_dict.Add("ST0", ["OP_ST0", "SZ_NA"]);
        operand_dict.Add("ST1", ["OP_ST1", "SZ_NA"]);
        operand_dict.Add("ST2", ["OP_ST2", "SZ_NA"]);
        operand_dict.Add("ST3", ["OP_ST3", "SZ_NA"]);
        operand_dict.Add("ST4", ["OP_ST4", "SZ_NA"]);
        operand_dict.Add("ST5", ["OP_ST5", "SZ_NA"]);
        operand_dict.Add("ST6", ["OP_ST6", "SZ_NA"]);
        operand_dict.Add("ST7", ["OP_ST7", "SZ_NA"]);
        operand_dict.Add("", ["OP_NONE", "SZ_NA"]);
        operand_dict.Add("ALr8b", ["OP_ALr8b", "SZ_NA"]);
        operand_dict.Add("CLr9b", ["OP_CLr9b", "SZ_NA"]);
        operand_dict.Add("DLr10b", ["OP_DLr10b", "SZ_NA"]);
        operand_dict.Add("BLr11b", ["OP_BLr11b", "SZ_NA"]);
        operand_dict.Add("AHr12b", ["OP_AHr12b", "SZ_NA"]);
        operand_dict.Add("CHr13b", ["OP_CHr13b", "SZ_NA"]);
        operand_dict.Add("DHr14b", ["OP_DHr14b", "SZ_NA"]);
        operand_dict.Add("BHr15b", ["OP_BHr15b", "SZ_NA"]);
        operand_dict.Add("rAXr8", ["OP_rAXr8", "SZ_NA"]);
        operand_dict.Add("rCXr9", ["OP_rCXr9", "SZ_NA"]);
        operand_dict.Add("rDXr10", ["OP_rDXr10", "SZ_NA"]);
        operand_dict.Add("rBXr11", ["OP_rBXr11", "SZ_NA"]);
        operand_dict.Add("rSPr12", ["OP_rSPr12", "SZ_NA"]);
        operand_dict.Add("rBPr13", ["OP_rBPr13", "SZ_NA"]);
        operand_dict.Add("rSIr14", ["OP_rSIr14", "SZ_NA"]);
        operand_dict.Add("rDIr15", ["OP_rDIr15", "SZ_NA"]);
        operand_dict.Add("jWP", ["OP_J", "SZ_WP"]);
        operand_dict.Add("jDP", ["OP_J", "SZ_DP"]);
        pfx_dict.Add("aso", "P_aso");
        pfx_dict.Add("oso", "P_oso");
        pfx_dict.Add("rexw", "P_rexw");
        pfx_dict.Add("rexb", "P_rexb");
        pfx_dict.Add("rexx", "P_rexx");
        pfx_dict.Add("rexr", "P_rexr");
        pfx_dict.Add("inv64", "P_inv64");
        pfx_dict.Add("def64", "P_def64");
        pfx_dict.Add("depM", "P_depM");
        pfx_dict.Add("cast1", "P_c1");
        pfx_dict.Add("cast2", "P_c2");
        pfx_dict.Add("cast3", "P_c3");
    }

    public static List<string?> mnm_list = [];

    public static int Main(string[] args)
    {
        var path = args.Length == 0 ? "x86optable.xml" : args[0];
        var table_path = args.Length <= 1 ? "Table.cs" : args[1];

        var dom = XDocument.Load(path);
        var list = dom.Elements("instruction").ToList();

        Dictionary<string, Dictionary<string, Dictionary<string, string>>> tables = [];
        Dictionary<string, int> table_sizes = [];
        for (int i = 0; i < list.Count; i++)
        {
            var n = list[i];
            var mnemonic = n.Attribute("mnemonic")?.Value;
            //Console.WriteLine(mnemonic);
            if (mnm_list.Contains(mnemonic))
                throw new InvalidOperationException("Multiple opcode definition for " + mnemonic);
            mnm_list.Add(mnemonic);
            var iclass = "";
            var vendor = "";
            var children = n.Elements().ToList();
            for (int j = 0; j < children.Count; j++)
            {
                var c = children[i];
                if (c is not XElement element)
                    continue;
                if (element.Name.LocalName == ("vendor"))
                    vendor = element.Value.Trim();
                if (element.Name.LocalName == ("class"))
                    iclass = element.Value.Trim();
            }

            // get each opcode definition
            for (int j = 0; j < children.Count; j++)
            {
                var c = children[i];
                if (c.Name.LocalName != ("opcode"))
                    continue;
                var opcode = c.Value;
                var parts = opcode.Split(";");
                List<string> flags = [];
                List<string> pfx_c = [];
                string[] opc;
                string[] opr = [];
                string[] pfx = [];

                var cast = c.Attribute("cast");
                if (null != cast)
                    pfx_c.Add("P_c" + cast.Value);

                var imp = c.Attribute("imp_addr");
                if ((null != imp) && (int.TryParse(imp.Value, out var v) && v != 0))
                    pfx_c.Add("P_ImpAddr");

                var mode = c.Attribute("mode");
                if (null != mode)
                {
                    var modef = mode.Value.Trim().Split(" ");
                    for (int m = 0; m < modef.Length; m++)
                        if (!pfx_dict.TryGetValue(modef[m], out string? value))
                            Console.WriteLine($"Warning: unrecognised mode attribute {modef[m]}");
                        else
                            pfx_c.Add(value);
                }

                // prefices, opcode bytes, operands
                if (parts.Length == 1)
                    opc = parts[0].Split(" ");
                else if (parts.Length == 2)
                {
                    opc = parts[0].Split(" ");
                    opr = parts[1].Trim().Split(" ");
                    for (int p = 0; p < opc.Length; p++)
                        if (pfx_dict.ContainsKey(opc[p]))
                        {
                            pfx = parts[0].Split(" ");
                            opc = parts[1].Split(" ");
                            break;
                        }
                }
                else if (parts.Length == 3)
                {
                    pfx = parts[0].Trim().Split(" ");
                    opc = parts[1].Trim().Split(" ");
                    opr = parts[2].Trim().Split(" ");
                }
                else
                    throw new InvalidOperationException("Invalid opcode definition for " + mnemonic);
                for (int k = 0; k < opc.Length; k++)
                    opc[k] = opc[k].ToUpper();

                if (mnemonic == ("pause") || (mnemonic == ("nop") && opc[0] == ("90")) || mnemonic == ("invalid") || mnemonic == ("db"))
                    continue;

                // prefix
                for (int k = 0; k < pfx.Length; k++)
                {
                    if ((pfx[k].Length > 0) && !pfx_dict.ContainsKey(pfx[k]))
                        Console.WriteLine("Error: invalid prefix specification: " + pfx[k]);
                    if (pfx[k].Trim().Length > 0)
                        pfx_c.Add(pfx_dict[(pfx[k])]);
                }
                if (pfx.Length == 0 || ((pfx.Length == 1) && pfx[0].Trim().Length == 0))
                    pfx_c.Add("P_none");
                pfx = [.. pfx_c];

                // operands
                string[] opr_c = ["O_NONE", "O_NONE", "O_NONE"];
                for (int k = 0; k < opr.Length; k++)
                {
                    if (!string.IsNullOrEmpty(opr[k]) && !operand_dict.ContainsKey(opr[k]))
                        Console.WriteLine("Error: Invalid operand " + opr[k]);
                    if (opr[k].Trim().Length == 0)
                        opr[k] = "NONE";
                    opr_c[k] = "O_" + opr[k];
                }
                opr = [$"{opr_c[0] + ",":-8} {opr_c[1] + ",":-8} {opr_c[2]}"];

                var table_sse = "";
                var table_name = "itab__1byte";
                var table_size = 256;
                var table_index = "";

                for (int k = 0; k < opc.Length; k++)
                {
                    var op = opc[k];
                    if (op.StartsWith("SSE"))
                        table_sse = op;
                    else if (op == ("0F") && (table_sse.Length > 0))
                    {
                        table_name = "itab__pfx_" + table_sse + "__0f";
                        table_size = 256;
                        table_sse = "";
                    }
                    else if (op == ("0F"))
                    {
                        table_name = "itab__0f";
                        table_size = 256;
                    }
                    else if (op.StartsWith("/X87="))
                    {
                        Dictionary<string, string> tmp = [];
                        tmp.Add("type", "grp_x87");
                        tmp.Add("name", table_name + "__op_" + table_index + "__x87");
                        tables[(table_name)].Add(table_index, tmp);
                        table_name = tables[(table_name)][(table_index)][("name")];
                        table_index = string.Format("X2", int.TryParse(op.AsSpan(5, 2), System.Globalization.NumberStyles.HexNumber, null, out var res) ? res : 0);
                        table_size = 64;
                    }
                    else if (op.StartsWith("/RM="))
                    {
                        Dictionary<string, string> tmp = [];
                        tmp.Add("type", "grp_rm");
                        tmp.Add("name", table_name + "__op_" + table_index + "__rm");
                        tables[(table_name)].Add(table_index, tmp);
                        table_name = tables[(table_name)][(table_index)][("name")];
                        table_index = string.Format("X2", int.TryParse(op.AsSpan(4, op.Length - 4), System.Globalization.NumberStyles.HexNumber, null, out int res) ? res : 0);
                        table_size = 8;
                    }
                    else if (op.StartsWith("/MOD="))
                    {
                        Dictionary<string, string> tmp = [];
                        tmp.Add("type", "grp_mod");
                        tmp.Add("name", table_name + "__op_" + table_index + "__mod");
                        tables[(table_name)].Add(table_index, tmp);
                        table_name = tables[(table_name)][(table_index)][("name")];
                        string v2 = op.Substring(5, 7);
                        if (op.Length == 8)
                            v2 = op.Substring(5, 8);

                        if (v2 == ("!11"))
                            table_index = "00";
                        else if (v2 == ("11"))
                            table_index = "01";
                        table_size = 2;
                    }
                    else if (op.StartsWith("/O"))
                    {
                        Dictionary<string, string> tmp = [];
                        tmp.Add("type", "grp_osize");
                        tmp.Add("name", table_name + "__op_" + table_index + "__osize");
                        tables[(table_name)].Add(table_index, tmp);
                        table_name = tables[(table_name)][(table_index)][("name")];
                        table_index = mode_dict[op.Substring(2, 4 - 2)];
                        table_size = 3;
                    }
                    else if (op.StartsWith("/A"))
                    {
                        Dictionary<string, string> tmp = [];
                        tmp.Add("type", "grp_asize");
                        tmp.Add("name", table_name + "__op_" + table_index + "__asize");
                        tables[(table_name)].Add(table_index, tmp);
                        table_name = tables[(table_name)][(table_index)][("name")];
                        table_index = mode_dict[(op.Substring(2, 4 - 2))];
                        table_size = 3;
                    }
                    else if (op.StartsWith("/M"))
                    {
                        Dictionary<string, string> tmp = [];
                        tmp.Add("type", "grp_mode");
                        tmp.Add("name", table_name + "__op_" + table_index + "__mode");
                        tables[(table_name)].Add(table_index, tmp);
                        table_name = tables[(table_name)][(table_index)][("name")];
                        table_index = mode_dict[(op.Substring(2, 4 - 2))];
                        table_size = 3;
                    }
                    else if (op.StartsWith("/3DNOW"))
                    {
                        table_name = "itab__3dnow";
                        table_size = 256;
                    }
                    else if (op.StartsWith("/"))
                    {
                        Dictionary<string, string> tmp = [];
                        tmp.Add("type", "grp_reg");
                        tmp.Add("name", table_name + "__op_" + table_index + "__reg");
                        tables[(table_name)].Add(table_index, tmp);
                        table_name = tables[(table_name)][(table_index)][("name")];
                        table_index = string.Format("X2", int.TryParse(op.Substring(1, 2), out var vx) ? vx : 0);
                        table_size = 8;
                    }
                    else
                        table_index = op;

                    if (!tables.ContainsKey(table_name))
                    {
                        tables.Add(table_name, []);
                        table_sizes.Add(table_name, table_size);
                    }
                }
                if (vendor.Length > 0)
                {
                    Dictionary<string, string> tmp = [];
                    tmp.Add("type", "grp_vendor");
                    tmp.Add("name", table_name + "__op_" + table_index + "__vendor");
                    tables[(table_name)].Add(table_index, tmp);
                    table_name = (string)tables[(table_name)][(table_index)][("name")];
                    table_index = vend_dict[(vendor)];
                    table_size = 2;
                    if (!tables.ContainsKey(table_name))
                    {
                        tables.Add(table_name, []);
                        table_sizes.Add(table_name, table_size);
                    }
                }

                Dictionary<string, string> leaf = [];
                leaf.Add("type", "leaf");
                leaf.Add("name", mnemonic??"");
                string pfx_string = "";
                if (pfx.Length > 0)
                {
                    pfx_string = pfx[0];
                    for (int cc = 1; cc < pfx.Length; cc++)
                        pfx_string += "|" + pfx[cc];
                }
                leaf.Add("pfx", pfx_string);
                string opr_string = "";
                if (opr.Length > 0)
                {
                    opr_string = opr[0];
                    for (int cc = 1; cc < opr.Length; cc++)
                        opr_string += "|" + opr[cc];
                }
                leaf.Add("opr", opr_string);
                //TODO:NOTICE: flags are joined
                leaf.Add("flags",string.Join(",", flags));
                tables[(table_name)].Add(table_index, leaf);
            }
        }

        // now print to file
        using var writer = new StreamWriter(table_path);
        writer.Write("namespace CSD;\n\n");
        writer.Write("using static ZygoteOperand;\n\n");
        writer.Write("public class Table\n{\n");

        writer.Write("\n");
        writer.Write("public static readonly int ITAB__VENDOR_INDX__AMD = 0;\n");
        writer.Write("public static readonly int ITAB__VENDOR_INDX__INTEL = 1;\n");

        writer.Write("\n");
        writer.Write("public static readonly int ITAB__MODE_INDX__16 = 0;\n");
        writer.Write("public static readonly int ITAB__MODE_INDX__32 = 1;\n");
        writer.Write("public static readonly int ITAB__MODE_INDX__64 = 2;\n");

        writer.Write("\n");
        writer.Write("public static readonly int ITAB__MOD_INDX__NOT_11 = 0;\n");
        writer.Write("public static readonly int ITAB__MOD_INDX__11 = 1;\n");

        // Generate enumeration of the tables
        List<string> table_names = [.. tables.Keys];
        table_names.Sort();

        writer.Write("\n");
        int h = 0;
        foreach (var name in table_names)
        {
            writer.Write("public static readonly int " + name.ToUpper() + " = " + h + ";\n");
            h++;
        }

        // Generate operators list
        writer.Write("\npublic static readonly List<string> ops = [\n");
        foreach (var m in mnm_list)
            writer.Write("  \"" + m + "\",\n");
        writer.Write("];\n\n");

        writer.Write("\npublic static readonly List<string> operator_spl = [\n");
        foreach (var m in spl_mnm_types)
            writer.Write("  \"" + m + "\",\n");
        writer.Write("];\n\n");

        writer.Write("\npublic static readonly List<string> operators_str = [\n");
        foreach (var m in mnm_list)
            writer.Write("  \"" + m + "\",\n");
        writer.Write("];\n\n");

        // Generate instruction tables
        foreach (var t in table_names)
        {
            writer.Write("private static readonly ZygoteInstruction[] " + t.ToLower() + " = new ZygoteInstruction[]{\n");
            for (int i = 0; i < table_sizes[t]; i++)
            {
                string index = $"{i:X02}";// string.format("%02X", i);
                Dictionary<string, string> tmp = [];
                tmp.Add("type", "invalid");
                if (tables[t].TryGetValue(index, out Dictionary<string, string>? value))
                    writer.Write(Centry(index, value));
                else
                    writer.Write(Centry(index, tmp));
            }
            writer.Write("};\n");
        }

        writer.Write("\n// the order of this table matches itab_index ");
        writer.Write("\npublic static readonly ZygoteInstruction[][] itab_list = new ZygoteInstruction[][]{\n");
        foreach (string name in table_names)
            writer.Write("    " + name.ToLower() + ",\n");
        writer.Write("};\n");
        writer.Write("}\n");
        return 0;
    }

    private static string Centry(string i, Dictionary<string, string> defmap)
    {
        string opr, mnm, pfx;
        if (defmap[("type")][..3] == ("grp"))
        {
            opr = default_opr;
            mnm = "\"" + defmap[("type")].ToLower() + "\"";
            pfx = defmap[("name")].ToUpper();
        }
        else if (defmap[("type")] == ("leaf"))
        {
            mnm = "\"" + defmap[("name")].ToLower() + "\"";
            opr = defmap[("opr")];
            pfx = defmap[("pfx")];
            if (mnm.Length == 0)
                mnm = "\'na\'";
            if (opr.Length == 0)
                opr = default_opr;
            if ((pfx == null) || (pfx.Length == 0))
                pfx = "P_none";
        }
        else
        {
            opr = default_opr;
            mnm = "\"invalid\"";
            pfx = "P_none";
        }
        return $"  new ZygoteInstruction( {mnm + ",":-16} {opr + ",":-26} {pfx} ),\n";
    }
}
