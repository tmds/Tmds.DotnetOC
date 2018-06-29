namespace Tmds.DotnetOC
{
    interface IConsole
    {
        void Write(string msg);

        void WriteLine(string msg);
    }

    static class ConsoleExtensions
    {
        public static void WriteErrorLine(this IConsole console, string msg)
        {
            console.Write("ERR ");
            console.WriteLine(msg);
        }

        public static void EmptyLine(this IConsole console)
        {
            console.WriteLine(string.Empty);
        }
    }
}