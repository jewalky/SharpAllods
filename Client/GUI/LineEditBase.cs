using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SharpAllods.Client;
using SharpAllods.Shared;
using SharpAllods.Rendering;
using OpenTK.Graphics.OpenGL;
using OpenTK.Input;

namespace SharpAllods.Client.GUI
{
    class LineEditBase : Widget
    {
        // properties
        public string Value
        {
            get
            {
                return CurrentString;
            }

            set
            {
                CurrentString = value;
                bool ksm = Selection1 == Selection2;
                if (Selection1 > CurrentString.Length)
                    Selection1 = CurrentString.Length;
                if (Selection2 > CurrentString.Length)
                    Selection2 = CurrentString.Length;
                if (!ksm) Selection1 = Selection2;
                CursorVisible = true;
            }
        }

        public int Cursor1
        {
            get
            {
                return Selection1;
            }

            set
            {
                Selection1 = value;
                if (Selection1 > CurrentString.Length)
                    Selection1 = CurrentString.Length;
                if (Selection1 < 0) Selection1 = 0;
            }
        }

        public int Cursor2
        {
            get
            {
                return Selection2;
            }

            set
            {
                Selection2 = value;
                if (Selection2 > CurrentString.Length)
                    Selection2 = CurrentString.Length;
                if (Selection2 < 0) Selection2 = 0;
            }
        }

        private Font LEFont = Fonts.Font1;
        private FontMesh CurrentMesh = null;
        private string CurrentStringOld = null;
        private string CurrentString = "";

        private int Selection1 = 0;
        private int Selection2 = 0;

        private long LastTicks = 0;
        private bool CursorVisible = true;

        private bool SelectingKeyboard = false;

        private Mesh CursorMesh = new Mesh(PrimitiveType.Lines);
        private Mesh SelectionMesh = new Mesh(PrimitiveType.Quads);

        public void SetFont(Font font)
        {
            LEFont = font;
        }

        public LineEditBase(int x, int y, int w, int h) : base(x, y, w, h)
        {

        }

        public override void OnRender()
        {
            //Console.WriteLine("LineEditBase render");
            int selection1x = LEFont.Width(CurrentString.Substring(0, Selection1))+2;
            int selection2x = LEFont.Width(CurrentString.Substring(0, Selection2))+2;

            int pselection1x = selection1x;
            int pselection2x = selection2x;

            if (pselection2x < pselection1x)
            {
                pselection2x = selection1x;
                pselection1x = selection2x;
            }

            if (selection1x != selection2x)
            {
                byte sr = 0;
                byte sg = 0;
                byte sb = 0;
                SelectionMesh.SetVertex(0, selection1x, 0, 0, 0, 0, sr, sg, sb, 255);
                SelectionMesh.SetVertex(1, selection2x, 0, 0, 0, 0, sr, sg, sb, 255);
                SelectionMesh.SetVertex(2, selection2x, Height, 0, 0, 0, sr, sg, sb, 255);
                SelectionMesh.SetVertex(3, selection1x, Height, 0, 0, 0, sr, sg, sb, 255);
                AllodsWindow.SetTranslation(GlobalX, GlobalY, 0f);
                SelectionMesh.Render("notex");
            }

            if (CurrentStringOld != CurrentString)
            {
                if (CurrentMesh != null) CurrentMesh.Dispose();
                CurrentMesh = LEFont.Render(CurrentString, Font.Align.Left, Width, Height, false);
                CurrentStringOld = CurrentString;
            }

            CurrentMesh.Render(GlobalX+2, GlobalY+2, 1, 255, 255, 255, 255);

            if (CursorVisible)
            {
                CursorMesh.SetVertex(0, selection2x, 0, 0, 0, 0, 255, 255, 255, 255);
                CursorMesh.SetVertex(1, selection2x, Height, 0, 0, 0, 255, 255, 255, 255);
                AllodsWindow.SetTranslation(GlobalX, GlobalY, 0f);
                CursorMesh.Render("notex");
            }
        }

        public override void OnTick()
        {
            if (Core.GetTickCount() - LastTicks > 500)
            {
                CursorVisible = !CursorVisible;
                LastTicks = Core.GetTickCount();
            }
        }

        public override bool OnMouseDown(int button)
        {
            return true;
        }

        public override void OnMouseUp(int button)
        {
            
        }

        public override void OnKeyDown(Key key)
        {
            if (key == Key.Left)
            {
                if (Selection2 > 0)
                    Selection2--;
                if (!SelectingKeyboard)
                    Selection1 = Selection2;
                CursorVisible = true;
            }
            else if (key == Key.Right)
            {
                if (Selection2 < CurrentString.Length)
                    Selection2++;
                if (!SelectingKeyboard)
                    Selection1 = Selection2;
                CursorVisible = true;
            }
            else if (key == Key.LShift || key == Key.RShift)
            {
                SelectingKeyboard = true;
            }
            else if (key == Key.BackSpace || key == Key.Delete)
            {
                if (Selection1 == Selection2)
                {
                    if (key == Key.BackSpace)
                    {
                        if (Selection2 > 0)
                        {
                            CurrentString = CurrentString.Remove(Selection2 - 1, 1);
                            Selection2--;
                        }
                    }
                    else
                    {
                        if (Selection2 < CurrentString.Length)
                        {
                            CurrentString = CurrentString.Remove(Selection2, 1);
                        }
                    }
                }
                else
                {
                    int s1, s2;
                    if (Selection1 < Selection2)
                    {
                        s1 = Selection1;
                        s2 = Selection2;
                    }
                    else
                    {
                        s1 = Selection2;
                        s2 = Selection1;
                    }

                    CurrentString = CurrentString.Remove(s1, s2 - s1);
                    Selection2 = s1;
                }

                Selection1 = Selection2;
                CursorVisible = true;
            }
            else if (key == Key.Home || key == Key.PageUp)
            {
                Selection2 = 0;
                if (!SelectingKeyboard)
                    Selection1 = Selection2;
                CursorVisible = true;
            }
            else if (key == Key.End || key == Key.PageDown)
            {
                Selection2 = CurrentString.Length;
                if (!SelectingKeyboard)
                    Selection1 = Selection2;
                CursorVisible = true;
            }
        }

        public override void OnKeyUp(Key key)
        {
            if (key == Key.LShift || key == Key.RShift)
            {
                SelectingKeyboard = false;
            }
        }

        public override void OnTextEntered(char ch)
        {
            //Console.WriteLine("Selection2 = {0}, CurrentString = '{1}', ch = '{2}'", Selection2, CurrentString, ch);
            // check largest input.
            string ns = CurrentString.Insert(Selection2, new String(ch, 1));
            if (LEFont.Width(ns) <= Width - 4)
            {
                CurrentString = ns;
                Selection2++;
                Selection1 = Selection2;
            }
            
            CursorVisible = true;
        }

        public override void Dispose()
        {
            base.Dispose();
        }
    }
}
