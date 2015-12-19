using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;
using System.Threading;
using OpenTK.Input;

namespace SharpAllods.Client.GUI
{
    class Widget : IDisposable
    {
        private int ClientX = 0;
        private int ClientY = 0;
        private int ClientWidth = 0;
        private int ClientHeight = 0;

        private int ScreenX = 0;
        private int ScreenY = 0;
        private int ScreenWidth = 0;
        private int ScreenHeight = 0;

        private Widget wParent = null;
        private List<Widget> Children = new List<Widget>();

        public Widget Parent
        {
            get
            {
                return wParent;
            }
        }

        public int X
        {
            get
            {
                return ClientX;
            }
        }

        public int Y
        {
            get
            {
                return ClientY;
            }
        }

        public int Width
        {
            get
            {
                return ClientWidth;
            }
        }

        public int Height
        {
            get
            {
                return ClientHeight;
            }
        }

        public int GlobalX
        {
            get
            {
                return ScreenX;
            }
        }

        public int GlobalY
        {
            get
            {
                return ScreenY;
            }
        }

        public Widget(int x, int y, int w, int h)
        {
            Resize(x, y, w, h);
        }

        public void Resize(int x, int y, int w, int h)
        {
            if (x != ClientX ||
                y != ClientY ||
                w != ClientWidth ||
                h != ClientHeight)
            {
                ClientX = x;
                ClientY = y;
                ClientWidth = w;
                ClientHeight = h;
                OnResize();

                int sx = 0;
                int sy = 0;
                Widget wp = this.wParent;
                while (wp != null)
                {
                    sx += wp.ClientX;
                    sy += wp.ClientY;
                    wp = wp.wParent;
                }

                UpdateClientRect(sx, sy);
                foreach (Widget wid in Children)
                    wid.OnParentResize();
            }
        }

        public void AddChild(Widget w)
        {
            if (w.wParent != null)
                w.wParent.RemoveChild(w);
            w.wParent = this;
            Children.Add(w);
        }

        public Widget RemoveChild(Widget w)
        {
            if (Children.Remove(w))
                return w;
            return null;
        }

        public void ClearChildren()
        {
            foreach (Widget w in Children)
                w.Dispose();
            Children.Clear();
        }

        internal void UpdateClientRect(int x, int y)
        {
            ScreenX = ClientX + x;
            ScreenY = ClientY + y;
            ScreenWidth = ClientWidth;
            ScreenHeight = ClientHeight;

            foreach (Widget w in Children)
                w.UpdateClientRect(ScreenX, ScreenY);
        }

        internal virtual void TreeTick()
        {
            OnTick();
            foreach (Widget w in Children)
                w.TreeTick();
        }

        internal virtual void TreeRender()
        {
            OnRender();
            foreach (Widget w in Children)
                w.TreeRender();
        }

        internal bool TreeMouseDownReverse(int button)
        {
            if (OnMouseDown(button))
                return true;
            if (wParent != null)
                return wParent.TreeMouseDownReverse(button);
            return false;
        }

        internal virtual void TreeMouseDown(int button)
        {
            if (Children.Count > 0)
            {
                foreach (Widget w in Children)
                    w.TreeMouseDown(button);
            }
            else // we've reached the top of the current branch
            {
                TreeMouseDownReverse(button); 
            }
        }

        // no matter how mouseup is propagated, its sent everywhere anyway.
        internal virtual void TreeMouseUp(int button)
        {
            OnMouseUp(button);
            foreach (Widget w in Children)
                w.TreeMouseUp(button);
        }

        internal virtual void TreeKeyDown(Key key)
        {
            OnKeyDown(key);
            foreach (Widget w in Children)
                w.TreeKeyDown(key);
        }

        internal virtual void TreeTextEntered(char ch)
        {
            OnTextEntered(ch);
            foreach (Widget w in Children)
                w.TreeTextEntered(ch);
        }

        internal virtual void TreeKeyUp(Key key)
        {
            OnKeyUp(key);
            foreach (Widget w in Children)
                w.TreeKeyUp(key);
        }

        public virtual void OnParentResize()
        {
            
        }

        public virtual void OnResize()
        {
            
        }

        public virtual void OnTick()
        {

        }

        public virtual void OnRender()
        {

        }

        public virtual bool OnMouseDown(int button)
        {
            return false;
        }

        public virtual void OnMouseUp(int button)
        {
            
        }

        public virtual void OnKeyDown(Key key)
        {

        }

        public virtual void OnTextEntered(char ch)
        {

        }

        public virtual void OnKeyUp(Key key)
        {

        }

        public virtual void Dispose()
        {
            // dispose all children
            foreach (Widget w in Children)
                w.Dispose();
        }
    }

    // no event handling until loaded. no rendering until loaded. forces mouse cursor to wait on widget.
    class LoadableWidget : Widget
    {
        public LoadableWidget(int x, int y, int w, int h) : base(x, y, w, h)
        {

        }

        private bool wLoaded = false;
        private bool wLoadedStarted = false;
        private Exception wException = null;

        internal void TreeLoad()
        {
            try
            {
                OnLoad();
            }
            catch (Exception e)
            {
                wException = e;
            }

            wLoaded = true;
        }

        internal override void TreeTick()
        {
            if (!wLoaded)
            {
                if (!wLoadedStarted)
                {
                    Thread thread = new Thread(new ThreadStart(this.TreeLoad));
                    thread.IsBackground = true;
                    thread.Start();
                    wLoadedStarted = true;
                }

                return;
            }

            if (wException != null)
                throw wException;

            base.TreeTick();
        }

        internal override void TreeRender()
        {
            if (!wLoaded)
            {
                Mouse.SetCursor(Mouse.CursorWait);
                return;
            }

            base.TreeRender();
        }

        internal override void TreeMouseDown(int button)
        {
            if (!wLoaded)
                return;
            base.TreeMouseDown(button);
        }

        internal override void TreeMouseUp(int button)
        {
            if (!wLoaded)
                return;
            base.TreeMouseUp(button);
        }

        internal override void TreeKeyDown(Key key)
        {
            if (!wLoaded)
                return;
            base.TreeKeyDown(key);
        }

        internal override void TreeKeyUp(Key key)
        {
            if (!wLoaded)
                return;
            base.TreeKeyUp(key);
        }

        internal override void TreeTextEntered(char ch)
        {
            if (!wLoaded)
                return;
            base.TreeTextEntered(ch);
        }

        public virtual void OnLoad()
        {
            
        }
    }

    class Widgets
    {
        internal static Widget RootWidget = new Widget(0, 0, 0, 0);

        public static void ResizeRoot(int w, int h)
        {
            RootWidget.Resize(0, 0, w, h);
        }

        public static void OnTick()
        {
            RootWidget.TreeTick();
        }

        public static void OnRender()
        {
            RootWidget.TreeRender();
        }

        public static void OnMouseDown(int button)
        {
            RootWidget.TreeMouseDown(button);
        }

        public static void OnMouseUp(int button)
        {
            RootWidget.TreeMouseUp(button);
        }

        public static void OnKeyDown(Key key)
        {
            RootWidget.TreeKeyDown(key);
        }

        public static void OnKeyUp(Key key)
        {
            RootWidget.TreeKeyUp(key);
        }

        public static void OnTextEntered(char ch)
        {
            RootWidget.TreeTextEntered(ch);
        }

        public static void SetRootWidget(Widget w) // this won't really be set as root. but will be added as the only child of the true root widget.
        {
            RootWidget.ClearChildren();
            if (w != null)
                RootWidget.AddChild(w);
        }
    }
}
