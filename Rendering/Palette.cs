using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using SharpAllods.Shared;

namespace SharpAllods.Rendering
{
    class Palette : IDisposable
    {
        private Texture PaletteTexture = null;
        private uint[] PaletteColors = null;

        public Palette(uint[] colors)
        {
            PaletteColors = new uint[256];
            for (int i = 0; i < 256; i++)
            {
                if (i < colors.Length)
                    PaletteColors[i] = colors[i];
                else PaletteColors[i] = 0;
            }

            PaletteTexture = new Texture(256, 1, PaletteColors);
        }

        public int TextureId
        {
            get
            {
                return PaletteTexture.TextureId;
            }
        }

        public void Bind(int n)
        {
            PaletteTexture.Bind(n);
        }

        public uint GetColorAt(int index)
        {
            if (index > 255 || index < 0)
                return 0;
            return PaletteTexture.GetPixelAt(index, 0);
        }

        public void Dispose()
        {
            PaletteTexture.Dispose();
        }
    }
}
