using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SharpAllods.Rendering;
using SharpAllods.Shared;
using SharpAllods.ImageFormats;
using OpenTK.Graphics.OpenGL;

namespace SharpAllods.Client
{
    class MouseCursor : IDisposable
    {
        internal int OffsetX = 0;
        internal int OffsetY = 0;
        internal TextureList Sprite = null;
        internal long Delay = 0;
        private bool OwnImage = true;
        private long LastTicks = 0;
        private int CurrentFrame = 0;

        public MouseCursor(string filename, int offsx, int offsy, long delay)
        {
            Sprite = Images.LoadSprite(filename);
            OffsetX = offsx;
            OffsetY = offsy;
            Delay = delay;
            OwnImage = true;
        }

        public MouseCursor(TextureList sprite, int offsx, int offsy, long delay)
        {
            Sprite = sprite;
            OffsetX = offsx;
            OffsetY = offsy;
            Delay = delay;
            OwnImage = false;
        }

        public void Render(int x, int y)
        {
            if (Sprite == null) return;

            if (Delay > 0)
            {
                long tL = Core.GetTickCount() - LastTicks;
                if (tL > Delay)
                {
                    while (tL > 0)
                    {
                        CurrentFrame = (CurrentFrame + 1) % Sprite.Textures.Count;
                        tL -= Delay;
                        LastTicks += Delay;
                    }
                }
            }
            else CurrentFrame = 0;

            // draw one image
            Texture curTex = Sprite.Textures[CurrentFrame];
            curTex.Render("paletted", Sprite.Palette, x - OffsetX, y - OffsetY);
        }

        public void Dispose()
        {
            if (OwnImage)
                Sprite.Dispose();
        }
    }

    class Mouse
    {
        public static int X = 0;
        public static int Y = 0;

        public static MouseCursor CursorDefault = null;
        public static MouseCursor CursorWait = null;
        public static MouseCursor CursorSelect = null;

        private static MouseCursor ActiveCursor = null;

        public static void LoadAll()
        {
            CursorDefault = new MouseCursor("graphics/cursors/default/sprites.16a", 4, 4, 0);
            CursorWait = new MouseCursor("graphics/cursors/wait/sprites.16a", 16, 16, 40);
            CursorSelect = new MouseCursor("graphics/cursors/select/sprites.16a", 3, 3, 0);
        }

        public static void UnsetCursor()
        {
            ActiveCursor = null;
        }

        public static void SetCursor(MouseCursor cur)
        {
            ActiveCursor = cur;
        }

        public static void SetCursor(TextureList tl, int offsx, int offsy, long delay)
        {
            if (ActiveCursor == null ||
                ActiveCursor.Sprite != tl ||
                ActiveCursor.OffsetX != offsx ||
                ActiveCursor.OffsetY != offsy ||
                ActiveCursor.Delay != delay)
            {
                ActiveCursor = new MouseCursor(tl, offsx, offsy, delay);
            }
        }

        public static void Render()
        {
            if (ActiveCursor != null)
                ActiveCursor.Render(X, Y);
        }
    }
}
