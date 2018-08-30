namespace Vostok.Logging.File.Rolling
{
    internal interface IFileNameTuner
    {
        string RestoreExtension(string file);
        string RemoveExtension(string file);
    }
}