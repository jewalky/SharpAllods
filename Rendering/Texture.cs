using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenTK.Graphics.OpenGL;

namespace SharpAllods.Rendering
{
    class Texture : IDisposable
    {
        public Texture(int w, int h, uint[] pixels)
        {
            if (w <= 0 || h <= 0 || pixels == null || pixels.Length < w * h)
            {
                TexWidth = 0;
                TexHeight = 0;
                TexPixels = null;
            }
            else
            {
                TexWidth = w;
                TexHeight = h;
                TexPixels = pixels;
            }

            TexGL = 0;
            ContextVersion = 0;
        }

        private int TexWidth;
        private int TexHeight;
        private uint[] TexPixels;

        private int TexGL;
        private int ContextVersion;

        public int Width
        {
            get
            {
                return TexWidth;
            }
        }

        public int Height
        {
            get
            {
                return TexHeight;
            }
        }

        public uint[] Pixels
        {
            get
            {
                return TexPixels;
            }
        }

        public uint GetPixelAt(int x, int y)
        {
            if (x < 0 || x >= TexWidth ||
                y < 0 || y >= TexHeight ||
                TexPixels == null) return 0;
            return TexPixels[TexWidth * y + x];
        }

        public int TextureId
        {
            get
            {
                if (ContextVersion != AllodsWindow.ContextVersion && TexPixels != null)
                {
                    TexGL = GL.GenTexture();
                    GL.BindTexture(TextureTarget.TextureRectangle, TexGL);
                    GL.TexParameter(TextureTarget.TextureRectangle, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Nearest);
                    GL.TexParameter(TextureTarget.TextureRectangle, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Nearest);
                    unsafe
                    {
                        fixed (uint* pixels = TexPixels)
                        {
                            GL.TexImage2D(TextureTarget.TextureRectangle, 0, PixelInternalFormat.Four, TexWidth, TexHeight, 0, PixelFormat.Bgra, PixelType.UnsignedByte, (IntPtr)pixels);
                        }
                    }

                    ContextVersion = AllodsWindow.ContextVersion;
                }

                return TexGL;
            }
        }

        public void Dispose()
        {
            if (ContextVersion == AllodsWindow.ContextVersion)
            {
                GL.DeleteTexture(TexGL);
                TexGL = 0;
                ContextVersion = 0;
            }
        }

        public void Bind(int target = 0)
        {
            GL.ActiveTexture(TextureUnit.Texture0 + target);
            GL.BindTexture(TextureTarget.TextureRectangle, TextureId);
        }

        private Mesh PrivMesh = new Mesh(PrimitiveType.Quads);

        public void Render(string shader, int x, int y)
        {
            Render(shader, null, x, y, 0, 0, Width, Height);
        }

        public void Render(string shader, Palette palette, int x, int y)
        {
            Render(shader, palette, x, y, 0, 0, Width, Height);
        }

        public void Render(string shader, int x, int y, int cx, int cy, int cw, int ch)
        {
            Render(shader, null, x, y, cx, cy, cw, ch);
        }

        public void Render(string shader, Palette palette, int x, int y, int cx, int cy, int cw, int ch)
        {
            Bind(0);
            if (palette != null)
                palette.Bind(1);
            PrivMesh.SetVertex(0, 0f, 0f, 0f,
                              0f, 0f,
                              255, 255, 255, 255);
            PrivMesh.SetVertex(1, (float)cw, 0f, 0f,
                              (float)(cx + cw), 0f,
                              255, 255, 255, 255);
            PrivMesh.SetVertex(2, (float)cw, (float)ch, 0f,
                              (float)(cx + cw), (float)(cy + ch),
                              255, 255, 255, 255);
            PrivMesh.SetVertex(3, 0f, (float)ch, 0f,
                              0f, (float)(cy + ch),
                              255, 255, 255, 255);
            AllodsWindow.SetTranslation((float)x, (float)y, 0f);
            PrivMesh.Render(shader);
        }
    }
}
