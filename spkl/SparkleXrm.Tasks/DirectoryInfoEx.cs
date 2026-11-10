using Microsoft.Extensions.FileSystemGlobbing;
using Microsoft.Extensions.FileSystemGlobbing.Abstractions;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace SparkleXrm.Tasks
{
    public class DirectoryService : IDirectoryService
    {
        public string GetApplicationDirectory()
        {
            string path = new FileInfo(Assembly.GetExecutingAssembly().Location).DirectoryName;

            return path;
        }

        public string SimpleSearch(string path, string search)
        {
            try
            {
                foreach (string f in Directory.GetFiles(path, search, SearchOption.AllDirectories))
                {
                    return f;
                }

                return null;
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Finds files by name in the folder and all sub-folders. Multiple names are separated by '|'.
        /// </summary>
        public List<string> Search(string path, string search)
        {
            var patterns = search
                .Split('|')
                .Select(s => s.StartsWith("..") || s.StartsWith("**") || s.StartsWith("\\") ? s : "**\\" + s);

            return Match(path, patterns);
        }

        /// <summary>
        /// Matches glob patterns relative to the folder exactly as written. Multiple patterns are separated by '|'.
        /// </summary>
        public List<string> Glob(string path, string pattern)
        {
            return Match(path, pattern.Split('|'));
        }

        private static List<string> Match(string path, IEnumerable<string> patterns)
        {
            var matcher = new Matcher(StringComparison.InvariantCultureIgnoreCase);
            foreach (var pattern in patterns)
            {
                matcher.AddInclude(pattern);
            }

            var globbMatch = matcher.Execute(new DirectoryInfoWrapper(new DirectoryInfo(path)));

            var matchList = new List<string>();
            foreach (var file in globbMatch.Files)
            {
                var fullFilePath = Path.Combine(path, file.Path.Replace("/", "\\"));
                var fileInfo = new FileInfo(fullFilePath);
                matchList.Add(fileInfo.FullName);
            }
            return matchList;

        }

        public void SaveFile(string filename, byte[] content, bool overwrite)
        {
            var fileInfo = new FileInfo(filename);
            if (!fileInfo.Directory.Exists) fileInfo.Directory.Create();

            if (File.Exists(filename) && !overwrite)
            {
                Console.WriteLine($"File already exists: {filename}");
                return;
            }

            using (var writer = new BinaryWriter((Stream)new FileStream(filename, FileMode.Create)))
            {
                writer.Write(content, 0, content.Length);
                writer.Close();
            }

            Console.WriteLine($"Downloaded: {filename}");
        }
    }
}
