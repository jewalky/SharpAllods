using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Drawing;
using System.Drawing.Imaging;
using SharpAllods.Rendering;
using SharpAllods.Shared;

namespace SharpAllods.ImageFormats
{
    class Images
    {
        public static Texture LoadImage(string filename)
        {
            return LoadImage(filename, 0, false);
        }

        public static Texture LoadImage(string filename, uint colormask)
        {
            return LoadImage(filename, colormask, true);
        }

        private static Texture LoadImage(string filename, uint colormask, bool has_colormask)
        {
            MemoryStream ms = ResourceManager.OpenRead(filename);
            if (ms == null)
            {
                Core.Abort("Couldn't load \"{0}\"", filename);
                return null;
            }

            Image image = Image.FromStream(ms);
            ms.Close();

            Bitmap bitmap = new Bitmap(image);
            uint[] pixels = new uint[bitmap.Width * bitmap.Height];
            int w = bitmap.Width;
            int h = bitmap.Height;

            BitmapData bmd = bitmap.LockBits(new Rectangle(0, 0, bitmap.Width, bitmap.Height), ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
            try
            {
                unsafe
                {
                    fixed (uint* opixels_fixed = pixels)
                    {
                        uint* opixels = opixels_fixed;
                        uint* ipixels = (uint*)bmd.Scan0;
                        if (!has_colormask)
                        {
                            for (int i = 0; i < bitmap.Height * bitmap.Width; i++)
                                *opixels++ = *ipixels++;
                        }
                        else
                        {
                            for (int i = 0; i < bitmap.Height * bitmap.Width; i++)
                            {
                                uint opixel = *opixels++;
                                if ((opixel & 0x00F0F0F0) == (colormask & 0x00F0F0F0))
                                    *ipixels++ = opixel;
                                else *ipixels++ = 0;
                            }
                        }
                    }
                }
            }
            finally
            {
                bitmap.UnlockBits(bmd);
                bitmap.Dispose();
                image.Dispose();
            }

            return new Texture(w, h, pixels);
        }

        public static Palette LoadPalette(string filename, int offset = 0x36)
        {
            MemoryStream ms = ResourceManager.OpenRead(filename);
            if (ms == null)
            {
                Core.Abort("Couldn't load \"{0}\"", filename);
                return null;
            }

            BinaryReader msb = new BinaryReader(ms);
            ms.Seek(offset, SeekOrigin.Begin);

            uint[] colors = new uint[256];

            for (int i = 0; i < 256; i++)
                colors[i] = msb.ReadUInt32();

            msb.Close();

            return new Palette(colors);
        }

        private static void SpriteAddIXIY(ref int ix, ref int iy, uint w, uint add)
        {
            int x = ix;
            int y = iy;
            for (int i = 0; i < add; i++)
            {
                x++;
                if (x >= w)
                {
                    y++;
                    x = x - (int)w;
                }
            }

            ix = x;
            iy = y;
        }

        public static TextureList Load256(string filename)
        {
            MemoryStream ms = ResourceManager.OpenRead(filename);
            if (ms == null)
            {
                Core.Abort("Couldn't load \"{0}\"", filename);
                return null;
            }

            BinaryReader msb = new BinaryReader(ms);

            ms.Position = ms.Length - 4;
            int count = msb.ReadInt32() & 0x7FFFFFFF;

            ms.Position = 0;
            uint[] colors = new uint[256];
            for (int i = 0; i < 256; i++)
                colors[i] = msb.ReadUInt32();

            Palette palette = new Palette(colors);
            List<Texture> textures = new List<Texture>();

            for (int i = 0; i < count; i++)
            {
                uint w = msb.ReadUInt32();
                uint h = msb.ReadUInt32();
                uint ds = msb.ReadUInt32();
                long cpos = ms.Position;

                if (w == 0 || h == 0 || ds == 0)
                {
                    Core.Abort("Invalid sprite \"{0}\": NULL frame #{1}", filename, i);
                    return null;
                }

                uint[] pixels = new uint[w * h];

                int ix = 0;
                int iy = 0;
                int ids = (int)ds;
                while (ids > 0)
                {
                    ushort ipx = msb.ReadByte();
                    ipx |= (ushort)(ipx << 8);
                    ipx &= 0xC03F;
                    ids--;

                    if ((ipx & 0xC000) > 0)
                    {
                        if ((ipx & 0xC000) == 0x4000)
                        {
                            ipx &= 0x3F;
                            SpriteAddIXIY(ref ix, ref iy, w, ipx * w);
                        }
                        else
                        {
                            ipx &= 0x3F;
                            SpriteAddIXIY(ref ix, ref iy, w, ipx);
                        }
                    }
                    else
                    {
                        ipx &= 0x3F;
                        for (int j = 0; j < ipx; j++)
                        {
                            uint ss = msb.ReadByte();
                            uint px = (ss << 16) | (ss << 8) | (ss) | 0xFF000000;
                            pixels[iy * w + ix] = px;
                            SpriteAddIXIY(ref ix, ref iy, w, 1);
                        }

                        ids -= ipx;
                    }
                }

                textures.Add(new Texture((int)w, (int)h, pixels));
                ms.Position = cpos + ds;
            }

            msb.Close();
            return new TextureList(palette, textures);
        }

        public static TextureList Load16A(string filename)
        {
            MemoryStream ms = ResourceManager.OpenRead(filename);
            if (ms == null)
            {
                Core.Abort("Couldn't load \"{0}\"", filename);
                return null;
            }

            BinaryReader msb = new BinaryReader(ms);

            ms.Position = ms.Length - 4;
            int count = msb.ReadInt32() & 0x7FFFFFFF;

            ms.Position = 0;
            uint[] colors = new uint[256];
            for (int i = 0; i < 256; i++)
                colors[i] = msb.ReadUInt32();

            Palette palette = new Palette(colors);
            List<Texture> textures = new List<Texture>();

            for (int i = 0; i < count; i++)
            {
                uint w = msb.ReadUInt32();
                uint h = msb.ReadUInt32();
                uint ds = msb.ReadUInt32();
                long cpos = ms.Position;

                if (w == 0 || h == 0 || ds == 0)
                {
                    Core.Abort("Invalid sprite \"{0}\": NULL frame #{1}", filename, i);
                    return null;
                }

                uint[] pixels = new uint[w * h];

                int ix = 0;
                int iy = 0;
                int ids = (int)ds;
                while (ids > 0)
                {
                    ushort ipx = msb.ReadUInt16();
                    ipx &= 0xC03F;
                    ids -= 2;

                    if ((ipx & 0xC000) > 0)
                    {
                        if ((ipx & 0xC000) == 0x4000)
                        {
                            ipx &= 0x3F;
                            SpriteAddIXIY(ref ix, ref iy, w, ipx * w);
                        }
                        else
                        {
                            ipx &= 0x3F;
                            SpriteAddIXIY(ref ix, ref iy, w, ipx);
                        }
                    }
                    else
                    {
                        ipx &= 0x3F;
                        for (int j = 0; j < ipx; j++)
                        {
                            uint ss = msb.ReadUInt16();
                            uint alpha = (((ss & 0xFF00) >> 9) & 0x0F) + (((ss & 0xFF00) >> 5) & 0xF0);
                            uint idx = ((ss & 0xFF00) >> 1) + ((ss & 0x00FF) >> 1);
                            idx &= 0xFF;
                            alpha &= 0xFF;
                            uint px = (idx << 16) | (idx << 8) | (idx) | (alpha << 24);
                            pixels[iy * w + ix] = px;
                            SpriteAddIXIY(ref ix, ref iy, w, 1);
                        }

                        ids -= ipx * 2;
                    }
                }

                textures.Add(new Texture((int)w, (int)h, pixels));
                ms.Position = cpos + ds;
            }

            msb.Close();
            return new TextureList(palette, textures);
        }

        public static TextureList Load16(string filename)
        {
            MemoryStream ms = ResourceManager.OpenRead(filename);
            if (ms == null)
            {
                Core.Abort("Couldn't load \"{0}\"", filename);
                return null;
            }

            BinaryReader msb = new BinaryReader(ms);

            ms.Position = ms.Length - 4;
            int count = msb.ReadInt32() & 0x7FFFFFFF;

            ms.Position = 0;
            Palette palette = null;
            List<Texture> textures = new List<Texture>();

            for (int i = 0; i < count; i++)
            {
                uint w = msb.ReadUInt32();
                uint h = msb.ReadUInt32();
                uint ds = msb.ReadUInt32();
                long cpos = ms.Position;

                if (w == 0 || h == 0 || ds == 0)
                {
                    Core.Abort("Invalid sprite \"{0}\": NULL frame #{1}", filename, i);
                    return null;
                }

                uint[] pixels = new uint[w * h];

                int ix = 0;
                int iy = 0;
                int ids = (int)ds;
                while (ids > 0)
                {
                    ushort ipx = msb.ReadByte();
                    ipx |= (ushort)(ipx << 8);
                    ipx &= 0xC03F;
                    ids -= 1;

                    if ((ipx & 0xC000) > 0)
                    {
                        if ((ipx & 0xC000) == 0x4000)
                        {
                            ipx &= 0x3F;
                            SpriteAddIXIY(ref ix, ref iy, w, ipx * w);
                        }
                        else
                        {
                            ipx &= 0x3F;
                            SpriteAddIXIY(ref ix, ref iy, w, ipx);
                        }
                    }
                    else
                    {
                        ipx &= 0x3F;

                        byte[] bytes = new byte[ipx];
                        for (int j = 0; j < ipx; j++)
                            bytes[j] = msb.ReadByte();

                        for (int j = 0; j < ipx; j++)
                        {
                            uint alpha1 = (bytes[j] & 0x0Fu) | ((bytes[j] & 0x0Fu) << 4);
                            uint px1 = 0x00FFFFFF | (alpha1 << 24);
                            pixels[iy * w + ix] = px1;
                            SpriteAddIXIY(ref ix, ref iy, w, 1);

                            if (j != ipx - 1 || (bytes[bytes.Length - 1] & 0xF0) > 0)
                            {
                                uint alpha2 = (bytes[j] & 0xF0u) | ((bytes[j] & 0xF0u) >> 4);
                                uint px2 = 0x00FFFFFF | (alpha2 << 24);
                                pixels[iy * w + ix] = px2;
                                SpriteAddIXIY(ref ix, ref iy, w, 1);
                            }
                        }

                        ids -= ipx;
                    }
                }

                textures.Add(new Texture((int)w, (int)h, pixels));
                ms.Position = cpos + ds;
            }

            msb.Close();
            return new TextureList(palette, textures);
        }

        public static TextureList LoadSprite(string filename)
        {
            string[] extf = filename.Split('.');
            string ext = extf[extf.Length - 1].ToLower();
            if (ext == "16a")
                return Load16A(filename);
            else if (ext == "256")
                return Load256(filename);
            else if (ext == "16")
                return Load16(filename);
            else
            {
                Core.Abort("Couldn't guess the sprite type of \"{0}\"", filename);
                return null;
            }
        }
    }
}
