using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using OpenTK.Graphics.OpenGL;
using SharpAllods.Shared;
using SharpAllods.ImageFormats;
using SharpAllods.Rendering;

namespace SharpAllods.Client
{
    class FontMesh : IDisposable
    {
        internal int MeshWidth = 0;
        internal int MeshHeight = 0;
        internal Mesh MeshObject = new Mesh(PrimitiveType.Quads);
        internal Mesh ShadowMeshObject = null;
        internal Texture MeshTexture = null;

        internal byte LastR = 255;
        internal byte LastG = 255;
        internal byte LastB = 255;
        internal byte LastA = 255;

        public void Render(int x, int y, int shadow, byte r, byte g, byte b, byte a)
        {
            // if current rgba != LastRGBA, update MeshObject
            // if shadow != 0 and LastShadow == 0, update ShadowMeshObject

            if (shadow != 0 && ShadowMeshObject == null)
            {
                ShadowMeshObject = MeshObject.Clone();
                for (int i = 0; i < ShadowMeshObject.VertexCount; i++)
                    ShadowMeshObject.SetVertexColor(i, 0, 0, 0, 255);
            }

            if (r != LastR || g != LastG || b != LastB || a != LastA)
            {
                for (int i = 0; i < MeshObject.VertexCount; i++)
                    MeshObject.SetVertexColor(i, r, g, b, a);
                LastR = r;
                LastG = g;
                LastB = b;
                LastA = a;
            }

            MeshTexture.Bind();

            if (shadow != 0)
            {
                AllodsWindow.SetTranslation((float)x+shadow, (float)y+shadow, 0f);
                ShadowMeshObject.Render("stencil");
            }

            AllodsWindow.SetTranslation((float)x, (float)y, 0f);
            MeshObject.Render("stencil");
        }

        public void Dispose()
        {
            if (MeshTexture != null)
                MeshTexture.Dispose();
        }
    }

    class Font
    {
        private int[] Widths = new int[224];
        private int CellX = 16; // max Width for sprite
        private int CellY = 16; // max Height for sprite
        private Texture CombinedTexture = null;
        private int Spacing = 2;
        public readonly int LineHeight = 16;

        public Font(string filename, int spacing, int line_height, int space_width)
        {
            Spacing = spacing;
            LineHeight = line_height;

            string[] fns = filename.Split('.');
            string fnn = string.Join(".", fns, 0, fns.Length - 1);
            
            // first, load the data file.
            MemoryStream ms_dat = ResourceManager.OpenRead(fnn + ".dat");
            if (ms_dat == null)
            {
                Core.Abort("Couldn't load \"{0}\" as data file for \"{1}\"", fnn + ".dat", filename);
                return;
            }

            int count = (int)ms_dat.Length / 4;
            BinaryReader msb_dat = new BinaryReader(ms_dat);
            for (int i = 0; i < 224; i++)
            {
                if (i < count)
                    Widths[i] = msb_dat.ReadInt32();
                else Widths[i] = 0;
            }

            msb_dat.Close();

            TextureList tlF = Images.LoadSprite(filename);
            CellX = 0;
            CellY = 0;
            for (int i = 0; i < tlF.Textures.Count; i++)
            {
                if (tlF.Textures[i].Width > CellX)
                    CellX = tlF.Textures[i].Width;
                if (tlF.Textures[i].Height > CellY)
                    CellY = tlF.Textures[i].Height;
            }

            int tex_w = CellX * 16;
            int tex_h = CellY * 16;
            uint[] tex_pixels = new uint[tex_w * tex_h];

            unsafe
            {
                fixed (uint* opixels_fixed = tex_pixels)
                {
                    for (int i = 0; i < tlF.Textures.Count; i++)
                    {
                        int cX = CellX * (i % 16);
                        int cY = CellY * (i / 16);
                        uint* opixels = opixels_fixed + cX + cY * tex_w;
                        fixed (uint* ipixels_fixed = tlF.Textures[i].Pixels)
                        {
                            uint* ipixels = ipixels_fixed;
                            for (int y = 0; y < tlF.Textures[i].Height; y++)
                            {
                                for (int x = 0; x < tlF.Textures[i].Width; x++)
                                {
                                    *opixels++ = *ipixels++;
                                }

                                opixels += tex_w - CellX;
                            }
                        }
                    }
                }
            }

            Widths[0] = space_width;
            CombinedTexture = new Texture(tex_w, tex_h, tex_pixels);
        }

        public enum Align
        {
            Left,
            Right,
            Center,
            LeftRight
        }

        private const char MappedReturn = (char)0xFFFE;
        private const char MappedNewline = (char)0xFFFF;
        private char MapChar(char ch)
        {
            if (ch == '\n')
                return MappedNewline;
            if (ch < 0x20)
                return MappedReturn;
            if (ch <= 0x7F && ch >= 0x20)
                return (char)(ch - 32);
            if (ch >= 0x410 && ch <= 0x43F)
                return (char)(ch - 0x380);
            if (ch >= 0x440 && ch <= 0x44F)
                return (char)(ch - 0x370);
            if (ch == 0x401) return (char)0xA0;
            if (ch == 0x402) return (char)0xA1;
            return (char)0x5F;
        }

        private string[] Wrap(string text, int w, bool wrapping)
        {
            List<string> lines = new List<string>();
            if (wrapping)
            {
                string line = "";
                int line_sep = -1;
                int line_wd = 0;
                int[] line_breakers = new int[]{MapChar('.'), MapChar(','),
                                                MapChar('='), MapChar('-'), MapChar('+'),
                                                MapChar('/'), MapChar('*'), MapChar(' ')};

                for (int i = 0; i < text.Length; i++)
                {
                    char c = text[i];

                    if (c == MappedNewline) // \n
                    {
                        lines.Add(line);
                        line = "";
                        line_sep = -1;
                        line_wd = 0;
                        continue;
                    }

                    if (line_wd + Width(c) > w)
                    {
                        if (line_sep >= 0)
                        {
                            lines.Add(line.Substring(0, line_sep));
                        }
                        else
                        {
                            line_sep = 0;
                            lines.Add(line);
                        }

                        line = line.Substring(line_sep);
                        line_sep = -1;
                        line_wd = 0;
                    }

                    if (line_breakers.Contains(c))
                        line_sep = line.Length + 1;

                    line += c;
                    line_wd += Width(c);
                }

                if (line.Length > 0)
                    lines.Add(line);
            }
            else
            {
                lines.Add(text);
            }

            return lines.ToArray();
        }

        internal int Width(char ch)
        {
            if (ch > Widths.Length)
                return 0;
            if (ch == 0)
                return Widths[0];
            return Widths[ch] + Spacing;
        }

        public int Width(string text)
        {
            int line_wd = 0;
            for (int i = 0; i < text.Length; i++)
            {
                line_wd += Width(MapChar(text[i]));
            }

            return line_wd;
        }

        public FontMesh Render(string text, Align align, int width, int height, bool wrapping)
        {
            FontMesh nm = new FontMesh();
            Mesh m = new Mesh(PrimitiveType.Quads);

            // todo: wrap text / output
            string text2 = "";
            for (int i = 0; i < text.Length; i++)
                text2 += MapChar(text[i]);

            string[] wrapped = Wrap(text2, width, wrapping);

            int vx = 0;

            nm.MeshWidth = 0;
            nm.MeshHeight = 0;

            float y = 0f;
            for (int i = 0; i < wrapped.Length; i++)
            {
                if (wrapped[i].Length > 0)
                {
                    float x = 0f;

                    int line_wd2 = 0;
                    int line_wd = 0;
                    int line_spccnt = 0;

                    float spc_width = (float)(Widths[0]);

                    if (wrapped[i][wrapped[i].Length - 1] == 0)
                        wrapped[i] = wrapped[i].Substring(0, wrapped[i].Length - 1); // remove last space if any

                    for (int j = 0; j < wrapped[i].Length; j++)
                    {
                        char c = wrapped[i][j];
                        int wd = (c != 0) ? Width(c) : (int)spc_width;
                        line_wd += wd;
                        if (c == 0) // space
                            line_spccnt++;
                        else if (c != 0) line_wd2 += wd;
                    }

                    if (align == Align.LeftRight && line_spccnt > 0 && i != wrapped.Length - 1)
                        spc_width = (float)(width - line_wd2) / line_spccnt;

                    if (align == Align.Right)
                        x = (float)(width - line_wd);
                    else if (align == Align.Center)
                        x = (float)(width / 2 - line_wd / 2);

                    int rw = 0;
                    for (int j = 0; j < wrapped[i].Length; j++)
                    {
                        char c = wrapped[i][j];
                        float sx1 = (float)((c % 16) * CellX);
                        float sx2 = (float)((c % 16) * CellX + CellX);
                        float sy1 = (float)((c / 16) * CellY);
                        float sy2 = (float)((c / 16) * CellY + CellY);

                        m.SetVertex(vx++, x, y, 0f, sx1, sy1, 255, 255, 255, 255);
                        m.SetVertex(vx++, x + CellX, y, 0f, sx2, sy1, 255, 255, 255, 255);
                        m.SetVertex(vx++, x + CellX, y + CellY, 0f, sx2, sy2, 255, 255, 255, 255);
                        m.SetVertex(vx++, x, y + CellY, 0f, sx1, sy2, 255, 255, 255, 255);

                        rw = (int)(x + Width(c));
                        x += (c != 0) ? (float)Width(c) : spc_width;
                    }

                    if (rw > nm.MeshWidth)
                        nm.MeshWidth = rw;
                }

                y += (float)LineHeight;
                nm.MeshHeight += LineHeight;
            }

            nm.MeshObject = m;
            nm.MeshTexture = CombinedTexture;

            return nm;
        }
    }

    class Fonts
    {
        private static Font ObjFont1 = null;
        private static Font ObjFont2 = null;
        private static Font ObjFont3 = null;
        private static Font ObjFont4 = null;

        public static Font Font1
        {
            get
            {
                return ObjFont1;
            }
        }

        public static Font Font2
        {
            get
            {
                return ObjFont2;
            }
        }

        public static Font Font3
        {
            get
            {
                return ObjFont3;
            }
        }

        public static Font Font4
        {
            get
            {
                return ObjFont4;
            }
        }

        public static void LoadAll()
        {
            ObjFont1 = new Font("graphics/font1/font1.16", 2, 16, 8);
            ObjFont2 = new Font("graphics/font2/font2.16", 2, 10, 6);
            ObjFont3 = new Font("graphics/font3/font3.16", 1, 6, 4);
            ObjFont4 = new Font("graphics/font4/font4.16a", 2, 16, 8);
        }
    }
}
