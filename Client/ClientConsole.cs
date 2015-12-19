using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SharpAllods.Rendering;
using SharpAllods.Shared;
using OpenTK.Graphics.OpenGL;
using OpenTK.Input;
using System.IO;
using SharpAllods.Client.GUI;

namespace SharpAllods.Client
{
    internal class ClientConsoleMessage
    {
        public string Text;
        public FontMesh Mesh;
        public int LastWidth; // if null, mesh should be regenerated

        public void UpdateMesh()
        {
            if (LastWidth != AllodsWindow.Width - 6 || Mesh == null)
            {
                if (Mesh != null)
                    Mesh.Dispose();
                Mesh = Fonts.Font2.Render(Text, Font.Align.Left, AllodsWindow.Width - 6, 0, true);
                LastWidth = AllodsWindow.Width - 6;
            }
        }
    }

    class ClientConsole
    {
        private static Mesh QMesh = new Mesh(PrimitiveType.Quads);
        private static bool Enabled = false;
        private static int Offset = 0;
        private static long LastTicks = 0;
        private static StringBuilder BufferedMessages = new StringBuilder();
        private static StringWriter BufferedWriter = new StringWriter(BufferedMessages);
        private static TextWriter ConsoleWriter = null;
        private static List<ClientConsoleMessage> Messages = new List<ClientConsoleMessage>();
        private static LineEditBase LineEdit = new LineEditBase(0, 0, 0, 0);
        private static FontMesh LineEditLeft = null;
        private static ClientConsoleCommands CmdHandler = new ClientConsoleCommands();

        // this writes things to the console (but not the client console)
        public static void WriteReal(string fmt, params object[] args)
        {
            /*if (ConsoleWriter != null)
                ConsoleWriter.Write(fmt, args);*/
            Console.Write(fmt, args);
        }

        // same but wraps WriteLine
        public static void WriteLineReal(string fmt, params object[] args)
        {
            /*if (ConsoleWriter != null)
                ConsoleWriter.WriteLine(fmt, args);*/
            Console.WriteLine(fmt, args);
        }

        public static bool IsEnabled
        {
            get
            {
                return Enabled;
            }
        }

        public static void AttachInterceptor()
        {
            BufferedMessages.Clear();
            ConsoleWriter = Console.Out;
            Console.SetOut(BufferedWriter);
        }

        public static void DetachInterceptor()
        {
            BufferedMessages.Clear();
            Console.SetOut(ConsoleWriter);
            ConsoleWriter = null;
        }

        public static bool CheckXY(int x, int y)
        {
            return (y <= Offset);
        }

        public static void OnTick()
        {
            int targetOffset = 0;
            if (Enabled) targetOffset = AllodsWindow.Height / 2;

            if (targetOffset != Offset)
            {
                int delay = 5;
                int step = 5;
                
                long tL = Core.GetTickCount() - LastTicks;

                if (tL > delay)
                {
                    while (tL > 0 && targetOffset != Offset)
                    {
                        if (targetOffset < Offset)
                        {
                            Offset -= step;
                            if (Offset < targetOffset)
                                Offset = targetOffset;
                        }
                        else if (targetOffset > Offset)
                        {
                            Offset += step;
                            if (Offset > targetOffset)
                                Offset = targetOffset;
                        }

                        tL -= delay;
                        LastTicks += delay;
                    }
                }
            }

            // process all buffered messages. these are to be added into the ingame console.
            if (BufferedMessages.Length > 0)
            {
                string lines = BufferedMessages.ToString();
                if (ConsoleWriter != null)
                    ConsoleWriter.Write(lines);

                bool hasNewline = lines.EndsWith("\n");
                string[] linesp = lines.Split('\n');
                if (hasNewline) linesp = linesp.Take(linesp.Length - 1).ToArray();

                // first line has special handling.
                int start = 0;
                if (Messages.Count > 0 && !Messages[Messages.Count - 1].Text.EndsWith("\n"))
                {
                    ClientConsoleMessage lastMsg = Messages[Messages.Count - 1];
                    lastMsg.Text += linesp[0];
                    lastMsg.LastWidth = 0; // force update
                    start = 1;
                }

                for (int i = start; i < linesp.Length; i++)
                {
                    ClientConsoleMessage msg = new ClientConsoleMessage();
                    msg.Text = linesp[i]+((i != linesp.Length - 1 || hasNewline) ? "\n" : "");
                    msg.LastWidth = 0;
                    msg.Mesh = null;
                    Messages.Add(msg);
                }

                BufferedMessages.Clear();
            }

            if (Enabled) LineEdit.TreeTick();
        }

        public static void OnRender()
        {
            if (Offset != 0)
            {
                int consoleHeight = AllodsWindow.Height / 2;

                QMesh.SetVertex(0, 0f, 0f, 0f, 0f, 0f, 64, 64, 64, 192);
                QMesh.SetVertex(1, (float)AllodsWindow.Width, 0f, 0f, 0f, 0f, 64, 64, 64, 192);
                QMesh.SetVertex(2, (float)AllodsWindow.Width, (float)consoleHeight, 0f, 0f, 0f, 16, 16, 16, 192);
                QMesh.SetVertex(3, 0f, (float)consoleHeight, 0f, 0f, 0f, 16, 16, 16, 192);

                QMesh.SetVertex(4, 0f, (float)consoleHeight, 0f, 0f, 0f, 255, 120, 0, 255);
                QMesh.SetVertex(5, (float)AllodsWindow.Width, (float)consoleHeight, 0f, 0f, 0f, 150, 72, 0, 255);
                QMesh.SetVertex(6, (float)AllodsWindow.Width, (float)(consoleHeight + 2), 0f, 0f, 0f, 73, 33, 0, 255);
                QMesh.SetVertex(7, 0f, (float)(consoleHeight + 2), 0f, 0f, 0f, 73, 33, 0, 255);

                QMesh.SetVertex(8, 0f, (float)(consoleHeight + 2), 0f, 0f, 0f, 0, 0, 0, 255);
                QMesh.SetVertex(9, (float)AllodsWindow.Width, (float)(consoleHeight + 2), 0f, 0f, 0f, 0, 0, 0, 255);
                QMesh.SetVertex(10, (float)AllodsWindow.Width, (float)(consoleHeight + 8), 0f, 0f, 0f, 0, 0, 0, 0);
                QMesh.SetVertex(11, 0f, (float)(consoleHeight + 8), 0f, 0f, 0f, 0, 0, 0, 0);

                AllodsWindow.SetTranslation(0f, (float)(Offset - consoleHeight), 0f);
                QMesh.Render("notex");

                // now draw last messages. starts with consoleHeight-Fonts.Font2.LineHeight*2
                int textY = Offset - Fonts.Font2.LineHeight * 2;
                int textX = 3;

                // draw text field
                if (LineEditLeft == null) LineEditLeft = Fonts.Font2.Render(">", Font.Align.Left, 0, 0, false);
                LineEditLeft.Render(textX, textY + 6, 1, 255, 255, 255, 255);
                LineEdit.Resize(12, textY + 4, AllodsWindow.Width-16, Fonts.Font2.LineHeight+4);
                LineEdit.SetFont(Fonts.Font2);
                LineEdit.TreeRender();
        
                for (int i = Messages.Count - 1; i >= 0; i--)
                {
                    ClientConsoleMessage msg = Messages[i];
                    if (msg.Text.Length > 0)
                    {
                        msg.UpdateMesh();
                        if (textY + msg.Mesh.MeshHeight < 0)
                            break;

                        textY -= msg.Mesh.MeshHeight;
                        msg.Mesh.Render(textX, textY, 1, 255, 255, 255, 255);
                    }
                }
            }
        }

        public static bool OnMouseDown(int button)
        {
            if (Enabled) LineEdit.TreeMouseDown(button);
            return CheckXY(Mouse.X, Mouse.Y);
        }

        public static void OnMouseUp(int button)
        {
            if (Enabled) LineEdit.TreeMouseUp(button);
        }

        static bool IgnoreNextChar = false;
        public static void OnKeyDown(Key key)
        {
            if (key == Key.Tilde)
            {
                Enabled = !Enabled;
                LastTicks = Core.GetTickCount();
                IgnoreNextChar = true;
                return;
            }
            else if (key == Key.Enter || key == Key.KeypadEnter)
            {
                string cmd = LineEdit.Value;
                LineEdit.Value = "";
                if (cmd.Trim().Length <= 0)
                    return;
                Console.WriteLine("> {0}", cmd);
                string[] args = SplitArguments(cmd);
                args[0] = args[0].ToLower();

                bool cmdFound = false;
                // now, go through ClientConsoleCommands
                System.Reflection.MethodInfo[] cmds = CmdHandler.GetType().GetMethods();
                for (int i = 0; i < cmds.Length; i++)
                {
                    if (cmds[i].Name.ToLower() == args[0] &&
                        cmds[i].IsPublic)
                    {
                        try
                        {
                            cmds[i].Invoke(CmdHandler, args.Skip(1).ToArray());
                            cmdFound = true;
                        }
                        catch (System.Reflection.TargetParameterCountException e)
                        {
                            if (args.Length - 1 < cmds[i].GetParameters().Length)
                                Console.WriteLine("{0}: too few arguments.", args[0]);
                            else Console.WriteLine("{0}: too many arguments.", args[0]);
                            cmdFound = true;
                        }
                        catch (ArgumentException e) // not a command, commands accept strings
                        {
                            
                        }
                        break;
                    }
                }

                if (!cmdFound)
                {
                    Console.WriteLine("{0}: command not found.", args[0]);
                }

                return;
            }

            if (Enabled) LineEdit.TreeKeyDown(key);
        }

        public static void OnKeyUp(Key key)
        {
            if (Enabled) LineEdit.TreeKeyUp(key);
        }

        public static void OnTextEntered(char ch)
        {
            if (Enabled && !IgnoreNextChar) LineEdit.TreeTextEntered(ch);
            IgnoreNextChar = false;
        }

        public static string[] SplitArguments(string commandLine)
        {
            var parmChars = commandLine.ToCharArray();
            var inSingleQuote = false;
            var inDoubleQuote = false;
            for (var index = 0; index < parmChars.Length; index++)
            {
                if (parmChars[index] == '"' && !inSingleQuote)
                {
                    inDoubleQuote = !inDoubleQuote;
                    parmChars[index] = '\n';
                }
                if (parmChars[index] == '\'' && !inDoubleQuote)
                {
                    inSingleQuote = !inSingleQuote;
                    parmChars[index] = '\n';
                }
                if (!inSingleQuote && !inDoubleQuote && parmChars[index] == ' ')
                    parmChars[index] = '\n';
            }
            return (new string(parmChars)).Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries);
        }
    }
}
