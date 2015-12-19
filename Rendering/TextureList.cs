using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SharpAllods.Rendering
{
    class TextureList : IDisposable
    {
        public TextureList(Palette p, List<Texture> textures)
        {
            PalettePrivate = p;
            TexturesPrivate = textures;
        }

        private Palette PalettePrivate = null;
        public Palette Palette
        {
            get
            {
                return PalettePrivate;
            }
        }

        private List<Texture> TexturesPrivate = null;
        public List<Texture> Textures
        {
            get
            {
                return TexturesPrivate;
            }
        }

        public void Dispose()
        {
            if (Textures == null) return;
            for (int i = 0; i < Textures.Count; i++)
                Textures[i].Dispose();
        }
    }
}
