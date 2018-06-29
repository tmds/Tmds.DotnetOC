using System;

namespace Tmds.DotnetOC
{
    class PhysicalConsole : IConsole
    {
        public void Write(string msg)
        {
            Console.Write(msg);
            Console.Out.Flush();
        }

        public void WriteLine(string msg)
        {
            Console.WriteLine(msg);
        }
    }
}