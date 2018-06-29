namespace Tmds.DotnetOC
{
    interface IOpenShift
    {
        Result CheckDependencies();

        Result CheckConnection();

        Result<string[]> GetImageTagVersions(string name, string ocNamespace);

        Result Create(bool exists, string content);
    }
}
