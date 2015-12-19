using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using OpenTK.Graphics.OpenGL;
using SharpAllods.ImageFormats;
using SharpAllods.Rendering;

namespace SharpAllods.Client.GUI
{
    class MainMenu : LoadableWidget
    {
        private Texture TexMenu = null;
        private Texture TexMenuMask = null;
        private Texture[] TexButtons = new Texture[8] { null, null, null, null, null, null, null, null };
        private Texture[] TexButtonsP = new Texture[8] { null, null, null, null, null, null, null, null };
        private Texture[] TexTexts = new Texture[8] { null, null, null, null, null, null, null, null };
        private bool[] Enabled = new bool[8] { false, false, false, false, false, false, false, true };
        
        private int[,] PosButtons = { { 204, 52 },
                                      { 124, 156 },
                                      { 124, 252 },
                                      { 208, 340 },
                                      { 340, 52 },
                                      { 424, 152 },
                                      { 412, 260 },
                                      { 344, 348 } };

        private int ButtonHovered = 0;
        private int ButtonClicked = 0;

        public MainMenu()
            : base(0, 0, AllodsWindow.Width, AllodsWindow.Height)
        {

        }

        public override void OnParentResize()
        {
            Resize(X, Y, Parent.Width, Parent.Height);
        }

        public override void OnLoad()
        {
            TexMenu = Images.LoadImage("main/graphics/mainmenu/menu_.bmp");
            TexMenuMask = Images.LoadImage("main/graphics/mainmenu/menumask.bmp");
            
            for (int i = 0; i < 8; i++)
            {
                TexButtons[i] = Images.LoadImage(String.Format("main/graphics/mainmenu/button{0}.bmp", i + 1));
                TexButtonsP[i] = Images.LoadImage(String.Format("main/graphics/mainmenu/button{0}p.bmp", i + 1));
                TexTexts[i] = Images.LoadImage(String.Format("main/graphics/mainmenu/text{0}.bmp", i + 1));
            }
        }

        public override void OnRender()
        {
            Mouse.SetCursor(Mouse.CursorSelect);

            int tlX = GlobalX + Width / 2 - TexMenu.Width / 2;
            int tlY = GlobalY + Height / 2 - TexMenu.Height / 2;
            //PosButtons

            // display the background. always.
            TexMenu.Render("rgb", tlX, tlY);
            if (ButtonClicked > 0)
            {
                if (ButtonHovered > 0)
                    TexTexts[ButtonHovered - 1].Render("rgb", tlX + 232, tlY + 200);
                else TexTexts[ButtonClicked - 1].Render("rgb", tlX + 232, tlY + 200);
                TexButtonsP[ButtonClicked - 1].Render("rgb", tlX + PosButtons[ButtonClicked - 1, 0], tlY + PosButtons[ButtonClicked - 1, 1]);
            }
            else if (ButtonHovered > 0)
            {
                TexTexts[ButtonHovered - 1].Render("rgb", tlX + 232, tlY + 200);
                if (Enabled[ButtonHovered - 1])
                    TexButtons[ButtonHovered - 1].Render("rgb", tlX + PosButtons[ButtonHovered - 1, 0], tlY + PosButtons[ButtonHovered - 1, 1]);
            }
        }

        public override void OnTick()
        {
            if (ClientConsole.CheckXY(Mouse.X, Mouse.Y))
            {
                ButtonClicked = 0;
                ButtonHovered = 0;
                return;
            }

            uint px = TexMenuMask.GetPixelAt(Mouse.X - (GlobalX + Width / 2 - TexMenu.Width / 2),
                                             Mouse.Y - (GlobalY + Height / 2 - TexMenu.Height / 2));
            px &= 0xF0;
            px >>= 4;
            if (px >= 8 && px <= 16) px -= 7;
            else px = 0;
            ButtonHovered = (int)px;
        }

        public override bool OnMouseDown(int button)
        {
            if (button == 1)
            {
                if (ButtonHovered > 0 && Enabled[ButtonHovered - 1])
                    ButtonClicked = ButtonHovered;
                else ButtonClicked = 0;
            }

            return true;
        }

        public override void OnMouseUp(int button)
        {
            if (button == 1)
            {
                if (ButtonClicked > 0 && Enabled[ButtonClicked - 1])
                {
                    switch (ButtonClicked)
                    {
                        case 8:
                            // exit button
                            AllodsWindow.Quit();
                            break;
                        default:
                            break;
                    }
                }

                ButtonClicked = 0;
            }
        }

        public override void Dispose()
        {
            base.Dispose();
            if (TexMenu != null)
                TexMenu.Dispose();
            TexMenu = null;
            if (TexMenuMask != null)
                TexMenuMask.Dispose();
            TexMenuMask = null;

            for (int i = 0; i < 8; i++)
            {
                if (TexButtons[i] != null)
                    TexButtons[i].Dispose();
                TexButtons[i] = null;
                if (TexButtonsP[i] != null)
                    TexButtonsP[i].Dispose();
                TexButtonsP[i] = null;
                if (TexTexts[i] != null)
                    TexTexts[i].Dispose();
                TexTexts[i] = null;
            }
        }
    }
}
