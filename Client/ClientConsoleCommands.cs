using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SharpAllods.Client
{
    class ClientConsoleCommands
    {
        public void quit()
        {
            AllodsWindow.Quit();
        }

        public void exit()
        {
            quit();
        }

        public void map(string filename)
        {
            Console.WriteLine("Switching to map from file \"{0}\"...", filename);
        }
    }
}
