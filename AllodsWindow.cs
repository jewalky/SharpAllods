using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;
using OpenTK.Input;
using System.Diagnostics;
using System.Threading;
using SharpAllods.Rendering;
using SharpAllods.ImageFormats;
using SharpAllods.Client;
using SharpAllods.Client.GUI;

namespace SharpAllods
{
    class AllodsWindow
    {
        private static GameWindow Window = null;
        private static bool WindowFullscreen = false;
        private static int WindowContextVersion = 0;
        private static int WindowLoadedContextVersion = 0;
        private static float ShadersTranslateX = 0f;
        private static float ShadersTranslateY = 0f;
        private static float ShadersTranslateZ = 0f;

        public static int ContextVersion
        {
            get
            {
                return WindowLoadedContextVersion;
            }
        }

        public static int Width
        {
            get
            {
                return (Window != null) ? Window.Width : 0;
            }
        }

        public static int Height
        {
            get
            {
                return (Window != null) ? Window.Height : 0;
            }
        }

        public static bool Fullscreen
        {
            get
            {
                return (Window != null) ? WindowFullscreen : false;
            }
        }

        public static uint GLVersion
        {
            get
            {
                if (Window == null)
                    return 0;
                
                try
                {
                    Window.Context.MakeCurrent(Window.WindowInfo);
                }
                catch (GraphicsContextException)
                {
                    return 0;
                }

                string[] glver = GL.GetString(StringName.Version).Split('.');
                uint ver_major = uint.Parse(glver[0]);
                uint ver_minor = (glver.Length > 1) ? uint.Parse(glver[1]) : 0;
                uint ver_ll = (glver.Length > 2) ? uint.Parse(glver[2]) : 0;

                return ((ver_major & 0xFF) << 24) | ((ver_minor & 0xFF) << 16) | (ver_ll & 0xFFFF);
            }
        }

        public static string GLVersionString
        {
            get
            {
                if (Window == null)
                    return "<error>";

                uint ver = GLVersion;

                if (ver == 0)
                    return "<error>";

                return String.Format("{0}.{1}.{2}", (ver & 0xFF00000) >> 24, (ver & 0x00FF0000) >> 16, (ver & 0x0000FFFF));
            }
        }

        public static void ActivateContext()
        {
            Window.Context.MakeCurrent(Window.WindowInfo);
        }

        public static void Quit()
        {
            Window.Close();
            Window.Dispose();
            Window = null;
        }

        public static bool SetVideoMode(int w, int h, bool fs)
        {
            if (Window != null)
            {
                Window.Close();
                Window.Dispose();
            }

            Window = new GameWindow(w, h, new GraphicsMode(new ColorFormat(32)), "SharpAllods", (fs ? GameWindowFlags.Fullscreen : 0) | GameWindowFlags.FixedWindow, DisplayDevice.Default, 2, 1, GraphicsContextFlags.Default);

            Console.WriteLine(" * Raw GL context version is {0}.", GL.GetString(StringName.Version));
            Console.WriteLine(" * GL context version {0} is available.", GLVersionString);
            if (GLVersion < 0x02010000)
            {
                Window.Close();
                Window.Dispose();
                Window = null;
                Console.WriteLine(" ! Supported version is not valid. At least OpenGL 2.1 is required.");
                return false;
            }

            Window.WindowBorder = WindowBorder.Fixed;
            Window.Visible = true;
            WindowFullscreen = fs;
            WindowContextVersion++;

            Console.WriteLine(" * Video mode {0}x{1}{2} initialized.", w, h, fs?" (fullscreen)":"");

            // remove system cursor
            //Window.CursorVisible = false; // this locks the mouse inside the window
            Window.Cursor = OpenTK.MouseCursor.Empty;
            // set window handlers
            Window.MouseMove += new EventHandler<MouseMoveEventArgs>(OnWindowMouseMove);
            Window.KeyDown += new EventHandler<KeyboardKeyEventArgs>(OnWindowKeyDown);
            Window.KeyUp += new EventHandler<KeyboardKeyEventArgs>(OnWindowKeyUp);
            Window.KeyPress += new EventHandler<KeyPressEventArgs>(OnWindowKeyPress);
            Window.MouseDown += new EventHandler<MouseButtonEventArgs>(OnWindowMouseDown);
            Window.MouseUp += new EventHandler<MouseButtonEventArgs>(OnWindowMouseUp);

            Widgets.ResizeRoot(w, h);

            return true;
        }

        // this is called manually from UI code
        public static bool Step()
        {
            if (Window == null)
                return false;

            Window.ProcessEvents();
            OnWindowTick();
            
            if (Window == null || Window.IsExiting)
            {
                Console.WriteLine(" * Window exited.");
                if (Window != null)
                    Window.Dispose();
                Window = null;
                return false;
            }

            try
            {
                Window.Context.MakeCurrent(Window.WindowInfo);
            }
            catch (GraphicsContextException)
            {
                Console.WriteLine(" * OpenGL context exited.");
                Window.Close();
                Window.Dispose();
                Window = null;
                return false;
            }

            OnWindowDisplay();
            Window.SwapBuffers();

            // delay 1ms
            Thread.Sleep(1);

            return true;
        }

        public static void Run()
        {
            while (Step()) continue;
        }

        public static void OnWindowLoad()
        {
            if (WindowLoadedContextVersion != WindowContextVersion)
            {
                Window.VSync = VSyncMode.Off;
                GL.EnableClientState(ArrayCap.VertexArray);
                GL.EnableClientState(ArrayCap.ColorArray);
                GL.EnableClientState(ArrayCap.TextureCoordArray);
                GL.Enable(EnableCap.TextureRectangle);
                GL.Enable(EnableCap.Blend);
                GL.BlendFunc(BlendingFactorSrc.SrcAlpha, BlendingFactorDest.OneMinusSrcAlpha);
                WindowLoadedContextVersion = WindowContextVersion;
            }

            // set necessary uniforms
            Shader s_rgb = Shaders.Get("rgb");
            Shader s_stencil = Shaders.Get("stencil");
            Shader s_paletted = Shaders.Get("paletted");
            s_rgb.SetUniform("texture1", 0);
            s_stencil.SetUniform("texture1", 0);
            s_paletted.SetUniform("texture1", 0);
            s_paletted.SetUniform("texture2", 1);

            SetViewport(0, 0, Width, Height);
            SetTranslation(0f, 0f, 0f);
        }

        public static void SetTranslation(float x, float y, float z)
        {
            if (ShadersTranslateX != x ||
                ShadersTranslateY != y ||
                ShadersTranslateZ != z)
            {
                ShadersTranslateX = x;
                ShadersTranslateY = y;
                ShadersTranslateZ = z;
                Shader s_rgb = Shaders.Get("rgb");
                s_rgb.SetUniform("translate", ShadersTranslateX, ShadersTranslateY, ShadersTranslateZ);
                Shader s_stencil = Shaders.Get("stencil");
                s_stencil.SetUniform("translate", ShadersTranslateX, ShadersTranslateY, ShadersTranslateZ);
                Shader s_paletted = Shaders.Get("paletted");
                s_paletted.SetUniform("translate", ShadersTranslateX, ShadersTranslateY, ShadersTranslateZ);
                Shader s_notex = Shaders.Get("notex");
                s_notex.SetUniform("translate", ShadersTranslateX, ShadersTranslateY, ShadersTranslateZ);
            }
        }

        public static void SetViewport(int x, int y, int w, int h)
        {
            GL.Viewport(x, Height-(y+h), w, h);
            Shader s_rgb = Shaders.Get("rgb");
            Shader s_stencil = Shaders.Get("stencil");
            Shader s_paletted = Shaders.Get("paletted");
            Shader s_notex = Shaders.Get("notex");
            s_rgb.SetUniform("screen", (float)x, (float)y, (float)w, (float)h);
            s_stencil.SetUniform("screen", (float)x, (float)y, (float)w, (float)h);
            s_paletted.SetUniform("screen", (float)x, (float)y, (float)w, (float)h);
            s_notex.SetUniform("screen", (float)x, (float)y, (float)w, (float)h);
        }

        public static void UnbindTexture(int target)
        {
            GL.ActiveTexture(TextureUnit.Texture0 + target);
            GL.BindTexture(TextureTarget.TextureRectangle, 0);
        }

        public static void OnWindowTick()
        {
            Widgets.OnTick();
            ClientConsole.OnTick();
        }

        public static int ActualFramerate
        {
            get
            {
                return FPS;
            }
        }

        private static int FPSCounter = 0;
        private static int FPS = 0;
        private static Stopwatch FPSWatch = new Stopwatch();
        public static void OnWindowDisplay()
        {
            OnWindowLoad();

            FPSCounter++;

            if (!FPSWatch.IsRunning) FPSWatch.Start();
            if (FPSWatch.ElapsedMilliseconds > 1000)
            {
                FPS = FPSCounter;
                FPSCounter = 0;
                FPSWatch.Restart();
                ClientConsole.WriteLineReal(" ~ FPS = {0}.", FPS);
            }

            GL.ClearColor(0.0f, 0.0f, 0.0f, 1.0f);
            GL.Clear(ClearBufferMask.ColorBufferBit);
            
            Client.Mouse.SetCursor(Client.Mouse.CursorDefault);

            Widgets.OnRender();
            ClientConsole.OnRender();

            Client.Mouse.Render();
        }

        // event handlers!
        public static void OnWindowMouseMove(object sender, MouseMoveEventArgs args)
        {
            if (sender != Window)
                return;

            Client.Mouse.X = args.X;
            Client.Mouse.Y = args.Y;
        }

        public static void OnWindowMouseDown(object sender, MouseButtonEventArgs args)
        {
            if (sender != Window)
                return;

            //
            int button = 0;
            if (args.Button == MouseButton.Left)
                button = 1;
            else if (args.Button == MouseButton.Right)
                button = 2;
            else if (args.Button == MouseButton.Middle)
                button = 3;
            if (!ClientConsole.OnMouseDown(button))
                Widgets.OnMouseDown(button);
        }

        public static void OnWindowMouseUp(object sender, MouseButtonEventArgs args)
        {
            if (sender != Window)
                return;

            //
            int button = 0;
            if (args.Button == MouseButton.Left)
                button = 1;
            else if (args.Button == MouseButton.Right)
                button = 2;
            else if (args.Button == MouseButton.Middle)
                button = 3;
            Widgets.OnMouseUp(button);
            ClientConsole.OnMouseUp(button);
        }

        public static void OnWindowKeyDown(object sender, KeyboardKeyEventArgs args)
        {
            if (sender != Window)
                return;
            
            //
            Widgets.OnKeyDown(args.Key);
            ClientConsole.OnKeyDown(args.Key);
        }

        public static void OnWindowKeyUp(object sender, KeyboardKeyEventArgs args)
        {
            if (sender != Window)
                return;

            //
            Widgets.OnKeyUp(args.Key);
            ClientConsole.OnKeyUp(args.Key);
        }

        public static void OnWindowKeyPress(object sender, KeyPressEventArgs args)
        {
            if (sender != Window)
                return;

            //
            Widgets.OnTextEntered(args.KeyChar);
            ClientConsole.OnTextEntered(args.KeyChar);
        }
    }
}
