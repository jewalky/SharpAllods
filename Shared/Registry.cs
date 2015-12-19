﻿using System;
using System.Collections.Generic;
using System.Text;
using System.IO;

namespace SharpAllods.Shared
{
    class Registry
    {
        private enum RegistryNodeType
        {
            Directory,
            String,
            Float,
            Int,
            Array,

            Null
        }

        private class RegistryNode
        {
            public string Name = "";

            public RegistryNodeType Type = RegistryNodeType.Null;

            public string ValueS = null;
            public double ValueF = 0.0;
            public int ValueI = 0;
            public int[] ValueA = null;

            public RegistryNode[] Children = null;
        }

        static readonly int RegistrySignature = 0x31415926;
        private RegistryNode Root;

        private static bool TreeTraverse(MemoryStream ms, BinaryReader msb, RegistryNode node, uint first, uint last, uint data_origin)
        {
            for (uint i = first; i < last; i++)
            {
                ms.Seek(0x18 + 0x20 * i, SeekOrigin.Begin);

                uint e_unk1 = msb.ReadUInt32();
                uint e_offset = msb.ReadUInt32(); // 0x1C
                uint e_count = msb.ReadUInt32(); // 0x18
                uint e_type = msb.ReadUInt32();// 0x14
                string e_name = Core.UnpackByteString(866, msb.ReadBytes(16));

                RegistryNode subnode = new RegistryNode();
                node.Children[i - first] = subnode;

                subnode.Name = e_name;
                if (e_type == 0) // string value
                {
                    subnode.Type = RegistryNodeType.String;
                    ms.Seek(data_origin + e_offset, SeekOrigin.Begin);
                    subnode.ValueS = Encoding.GetEncoding(866).GetString(msb.ReadBytes((int)e_count));
                }
                else if (e_type == 2) // dword value
                {
                    subnode.Type = RegistryNodeType.Int;
                    subnode.ValueI = (int)e_offset;
                }
                else if (e_type == 4) // float value
                {
                    // well, we gotta rewind and read it again
                    // C-style union trickery won't work
                    subnode.Type = RegistryNodeType.Float;
                    ms.Seek(-0x1C, SeekOrigin.Current);
                    subnode.ValueF = msb.ReadDouble();
                }
                else if (e_type == 6) // int array
                {
                    if ((e_count % 4) != 0)
                        return false;
                    uint e_acount = e_count / 4;
                    subnode.Type = RegistryNodeType.Array;
                    subnode.ValueA = new int[e_acount];
                    ms.Seek(data_origin + e_offset, SeekOrigin.Begin);
                    for (uint j = 0; j < e_acount; j++)
                        subnode.ValueA[j] = msb.ReadInt32();
                }
                else if (e_type == 1) // directory
                {
                    subnode.Type = RegistryNodeType.Directory;
                    subnode.Children = new RegistryNode[e_count];
                    if (!TreeTraverse(ms, msb, subnode, e_offset, e_offset + e_count, data_origin))
                        return false;
                }
            }

            return true;
        }

        public Registry(string filename)
        {
            try
            {
                MemoryStream ms = ResourceManager.OpenRead(filename);
                if (ms == null)
                {
                    Core.Abort("Couldn't load \"{0}\"", filename);
                    return;
                }

                BinaryReader msb = new BinaryReader(ms);
                if (msb.ReadUInt32() != RegistrySignature)
                {
                    ms.Close();
                    Core.Abort("Couldn't load \"{0}\" (not a registry file)", filename);
                    return;
                }

                uint root_offset = msb.ReadUInt32();
                uint root_size = msb.ReadUInt32();
                uint reg_flags = msb.ReadUInt32();
                uint reg_eatsize = msb.ReadUInt32();
                uint reg_junk = msb.ReadUInt32();

                Root = new RegistryNode();
                Root.Type = RegistryNodeType.Directory;
                Root.Name = "";
                Root.Children = new RegistryNode[root_size];
                if (!TreeTraverse(ms, msb, Root, root_offset, root_offset + root_size, 0x1C + 0x20 * reg_eatsize))
                {
                    ms.Close();
                    Core.Abort("Couldn't load \"{0}\" (invalid registry file)", filename);
                    return;
                }

                ms.Close();
            }
            catch (IOException)
            {
                Core.Abort("Couldn't load \"{0}\"", filename);
            }
        }

        private RegistryNode GetRegistryNode(string name1, string name2)
        {
            RegistryNode level1 = null;
            RegistryNode level2 = null;

            foreach (RegistryNode node in Root.Children)
            {
                if (node.Name.ToLower().Equals(name1.ToLower()))
                {
                    level1 = node;
                    break;
                }
            }

            if ((level1 == null) ||
                (level1.Type != RegistryNodeType.Directory))
                return null;

            foreach (RegistryNode node in level1.Children)
            {
                if (node.Name.ToLower().Equals(name2.ToLower()))
                {
                    level2 = node;
                    break;
                }
            }

            return level2;
        }

        public string GetString(string name1, string name2, string def)
        {
            RegistryNode node = GetRegistryNode(name1, name2);
            if ((node == null) ||
                (node.Type != RegistryNodeType.String)) return def;
            return node.ValueS;
        }

        public int GetInt(string name1, string name2, int def)
        {
            RegistryNode node = GetRegistryNode(name1, name2);
            if ((node == null) ||
                (node.Type != RegistryNodeType.Int)) return def;
            return node.ValueI;
        }

        public double GetFloat(string name1, string name2, double def)
        {
            RegistryNode node = GetRegistryNode(name1, name2);
            if ((node == null) ||
                (node.Type != RegistryNodeType.Float)) return def;
            return node.ValueF;
        }

        public int[] GetArray(string name1, string name2, int[] def)
        {
            RegistryNode node = GetRegistryNode(name1, name2);
            if ((node == null) ||
                (node.Type != RegistryNodeType.Array)) return def;
            return (int[])node.ValueA.Clone();
        }
    }
}
