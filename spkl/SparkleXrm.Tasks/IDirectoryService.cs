using System.Collections.Generic;

namespace SparkleXrm.Tasks
{
    public interface IDirectoryService
    {
        string GetApplicationDirectory();
        string SimpleSearch(string path, string search);
        List<string> Search(string path, string search);
        List<string> Glob(string path, string pattern);
        void SaveFile(string filename, byte[] content, bool overwrite);
    }
}