namespace Vostok.Logging.File.Rolling.Helpers
{
    internal interface IFileNameTuner
    {
        string RestoreExtension(string file);

        string RemoveExtension(string file);
    }
}
