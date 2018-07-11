namespace Tmds.DotnetOC
{
    static class PathUtils
    {
        public static string ApplicationDirectory
            => System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);

        public static string ApplicationPath(string filename)
            => System.IO.Path.Combine(ApplicationDirectory, filename);
    }
}