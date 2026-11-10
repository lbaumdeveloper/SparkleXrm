using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SparkleXrm.Tasks.Config
{
    public class ConfigFile
    {
        public List<WebresourceDeployConfig> webresources;
        public List<PluginDeployConfig> plugins;
        public List<EarlyBoundTypeConfig> earlyboundtypes;
        public List<SolutionPackageConfig> solutions;

        [JsonProperty("$schema")]
        public string schema;

        [JsonIgnore]
        public string filePath;

        /// <summary>
        /// Throws when a deployment entry has no solution, because deploying to the default solution is not supported.
        /// </summary>
        public void RequireSolution(string solution, string section)
        {
            if (string.IsNullOrWhiteSpace(solution))
            {
                throw new SparkleTaskException(SparkleTaskException.ExceptionTypes.SOLUTION_REQUIRED,
                    $"Every '{section}' entry in '{Path.Combine(filePath, "spkl.json")}' needs a 'solution'. Deploying to the default solution is not supported.");
            }
        }

        public virtual void Save()
        {
            var file = Path.Combine(filePath, "spkl.json");
            if (File.Exists(file))
            {
                File.Copy(file, file + DateTime.Now.ToString("yyyyMMddHHmmss") + ".bak");
            }
            File.WriteAllText(file, Newtonsoft.Json.JsonConvert.SerializeObject(this, Newtonsoft.Json.Formatting.Indented, new JsonSerializerSettings
            {
                NullValueHandling = NullValueHandling.Ignore
            }));
        }

        public virtual SolutionPackageConfig[] GetSolutionConfig(string profile)
        {
            if (solutions == null)
                return new SolutionPackageConfig[0];

            SolutionPackageConfig[] config = null;
            if (profile == "default")
            {
                profile = null;
            }
            if (profile != null)
            {
                config = solutions.Where(c => c.profile != null && c.profile.Replace(" ", "").Split(',').Contains(profile)).ToArray();
            }
            else
            {
                // Default profile or empty
                config = solutions.Where(c => c.profile == null || c.profile.Replace(" ", "").Split(',').Contains("default") || String.IsNullOrWhiteSpace(c.profile)).ToArray();
            }

            return config;
        }

        public virtual EarlyBoundTypeConfig[] GetEarlyBoundConfig(string profile)
        {
            if (earlyboundtypes == null)
                return new EarlyBoundTypeConfig[] { new EarlyBoundTypeConfig(){
                    filename ="Entities.cs",
                    entities = "account,contact",
                    classNamespace = "Xrm",
                    generateOptionsetEnums = true,
                    generateStateEnums = true
                } };

            EarlyBoundTypeConfig[] config = null;
            if (profile == "default")
            {
                profile = null;
            }
            if (profile != null)
            {
                config = earlyboundtypes.Where(c => c.profile != null && c.profile.Replace(" ", "").Split(',').Contains(profile)).ToArray();
            }
            else
            {
                // Default profile or empty
                config = earlyboundtypes.Where(c => c.profile == null || c.profile.Replace(" ", "").Split(',').Contains("default") || String.IsNullOrWhiteSpace(c.profile)).ToArray();
            }

            return config;
        }

        public virtual WebresourceDeployConfig[] GetWebresourceConfig(string profile)
        {
            if (webresources == null)
                return new WebresourceDeployConfig[0];

            WebresourceDeployConfig[] config = null;
            if (profile == "default")
            {
                profile = null;
            }
            if (profile != null)
            {
                config = webresources.Where(c => c.profile != null && c.profile.Replace(" ", "").Split(',').Contains(profile)).ToArray();
            }
            else
            {
                // Default profile or empty
                config = webresources.Where(c => c.profile == null || c.profile.Replace(" ", "").Split(',').Contains("default") || String.IsNullOrWhiteSpace(c.profile)).ToArray();
            }

            return config;
        }

        public virtual PluginDeployConfig[] GetPluginsConfig(string profile)
        {
            PluginDeployConfig[] config = null;
            if (plugins == null)
                return new PluginDeployConfig[0];

            if (profile == "default")
            {
                profile = null;
            }

            if (profile != null)
            {
                config = plugins.Where(c => c.profile != null && c.profile.Replace(" ", "").Split(',').Contains(profile)).ToArray();
            }
            else
            {
                // Default profile or empty
                config = plugins.Where(c => c.profile == null || c.profile.Replace(" ", "").Split(',').Contains("default") || String.IsNullOrWhiteSpace(c.profile)).ToArray();
            }

            return config;
        }

        public virtual List<string> GetAssemblies(PluginDeployConfig plugin)
        {
            var file = plugin.assemblypath;

            List<string> assemblies;
            var extension = Path.GetExtension(file);

            if (extension == "") file = Path.Combine(file, "*.dll");

            assemblies = ServiceLocator.DirectoryService.Glob(this.filePath, file);
            return assemblies;
        }
    }

    public class ConfigFileService : IConfigFileService
    {
        /// <summary>
        /// Loads the spkl.json given as a file path, or the spkl.json directly inside the given folder. Sub-folders are not searched.
        /// </summary>
        public List<ConfigFile> FindConfig(string folder, bool raiseErrorIfNotFound = true)
        {
            var configPath = folder.EndsWith("spkl.json", StringComparison.OrdinalIgnoreCase)
                ? folder
                : Path.Combine(folder, "spkl.json");

            if (!File.Exists(configPath))
            {
                if (raiseErrorIfNotFound)
                {
                    throw new SparkleTaskException(SparkleTaskException.ExceptionTypes.CONFIG_NOTFOUND,
                        $"Cannot find '{configPath}'. Pass the folder that contains spkl.json, or the path of the spkl.json file.");
                }

                return new List<ConfigFile> { new ConfigFile { filePath = folder } };
            }

            var config = Newtonsoft.Json.JsonConvert.DeserializeObject<ConfigFile>(File.ReadAllText(configPath));
            config.filePath = Path.GetDirectoryName(Path.GetFullPath(configPath));
            return new List<ConfigFile> { config };
        }
    }
}