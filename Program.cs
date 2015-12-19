using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SharpAllods.Shared;
using SharpAllods.Client;
using SharpAllods.Client.GUI;

namespace SharpAllods
{
    class Program
    {
        private static void AddResourceWrapper(string res)
        {
            Console.Write(" * Adding {0}... ", res);
            if (ResourceManager.AddResource(res))
                Console.Write("ok.\n");
            else Console.Write("fail!\n");
        }

        [STAThread]
        public static void Main(string[] args)
        {
            AddResourceWrapper("main.res");
            AddResourceWrapper("graphics.res");
            AddResourceWrapper("music.res");
            AddResourceWrapper("sfx.res");
            AddResourceWrapper("world.res");
            AddResourceWrapper("patch.res");
            AddResourceWrapper("scenario.res");

            // lets assume that we aren't hosting the server
            Mouse.LoadAll();
            Fonts.LoadAll();
            Widgets.SetRootWidget(new MainMenu());
            AllodsWindow.SetVideoMode(1024, 768, false);
            // redirect console to clientconsole. note that with this, all messages are tied to the main client loop.
            ClientConsole.AttachInterceptor();
            AllodsWindow.Run();
        }
    }
}
