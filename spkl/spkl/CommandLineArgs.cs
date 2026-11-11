using System;
using System.Linq;
using System.Text.RegularExpressions;

namespace SparkleXrmTask
{
    /// <summary>
    /// Arguments of the spkl command line: spkl task [path] [connection-string] [profile] [/p:profile] [/s:prefix] [/o] [/e] [/w] [/l] [/i]
    /// </summary>
    public class CommandLineArgs
    {
        public const string HelpText = @"
Usage: spkl <task> [path] [connection-string] [/p:<profile>] [switches]

Tasks:
  plugins                Deploy plugins and Custom API plugin types
  workflow               Deploy custom workflow activities
  webresources           Deploy and publish webresources
  instrument             Read registrations from Dataverse and add [CrmPluginRegistration] attributes to existing classes
  get-webresources       Match local webresource files to webresources in Dataverse and add them to spkl.json
  download-webresources  Download the webresources of a solution to local files
  earlybound             Create early-bound types for C# development
  unpack                 Export a solution from Dataverse and unpack it into xml files & folders
  unpacksolution         Unpack a solution zip from the filesystem into xml files & folders
  pack                   Pack an unpacked solution into a solution zip
  import                 Pack a solution as per 'pack', then import it into Dataverse and publish
  export                 Export a solution from Dataverse as zip file(s)
  whoami                 Show the organization and user of the connection

Arguments:
  path                   Folder that contains spkl.json, or the spkl.json file. Defaults to the current directory.
  connection-string      Connection string. Without it, spkl shows an interactive login. """" = no connection.

Switches:
  /p:<profile>           Only use spkl.json entries with this profile
  /s:<prefix>            get-webresources: publisher prefix, e.g. /s:new
  /o                     download-webresources: overwrite existing files
  /e                     plugins: register assemblies and types but no steps
  /w                     Wait for a key press before exiting
  /l                     On-premises login (AD / IFD) via the discovery service
  /i                     With /l: ignore the logged-in Windows user and ask for credentials
  /?                     Show this help
";

        private static readonly Regex SwitchPattern = new Regex(@"^[/-](?<name>[a-z?])(:(?<value>.*))?$", RegexOptions.IgnoreCase);

        public string Task { get; set; }
        public string Path { get; set; }
        public string Connection { get; set; }
        public string Profile { get; set; }
        public string Prefix { get; set; }
        public bool Overwrite { get; set; }
        public bool ExcludePluginSteps { get; set; }
        public bool WaitForKey { get; set; }
        public bool LegacyLogin { get; set; }
        public bool IgnoreLocalPrincipal { get; set; }
        public bool Help { get; set; }

        /// <summary>
        /// Parses the command line. Throws <see cref="ArgumentException"/> for unknown switches or too many positional arguments.
        /// </summary>
        public static CommandLineArgs Parse(string[] args)
        {
            var result = new CommandLineArgs();
            var positional = new System.Collections.Generic.List<string>();

            foreach (var arg in args)
            {
                var match = SwitchPattern.Match(arg);
                if (!match.Success)
                {
                    positional.Add(arg);
                    continue;
                }

                var value = match.Groups["value"].Success ? match.Groups["value"].Value : null;
                switch (match.Groups["name"].Value.ToLowerInvariant())
                {
                    case "p":
                        result.Profile = RequireValue("p", value);
                        break;
                    case "s":
                        result.Prefix = RequireValue("s", value);
                        break;
                    case "o":
                        result.Overwrite = true;
                        break;
                    case "e":
                        result.ExcludePluginSteps = true;
                        break;
                    case "w":
                        result.WaitForKey = true;
                        break;
                    case "l":
                        result.LegacyLogin = true;
                        break;
                    case "i":
                        result.IgnoreLocalPrincipal = true;
                        break;
                    case "?":
                    case "h":
                        result.Help = true;
                        break;
                    default:
                        throw new ArgumentException($"Unknown switch '{arg}'.");
                }
            }

            if (positional.Count > 4)
            {
                throw new ArgumentException($"Too many arguments: '{string.Join("' '", positional.Skip(4))}'.");
            }

            result.Task = positional.ElementAtOrDefault(0);
            result.Path = positional.ElementAtOrDefault(1);
            result.Connection = positional.ElementAtOrDefault(2);
            result.Profile = result.Profile ?? positional.ElementAtOrDefault(3);

            if (!result.Help && string.IsNullOrWhiteSpace(result.Task))
            {
                throw new ArgumentException("No task given.");
            }

            return result;
        }

        private static string RequireValue(string name, string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new ArgumentException($"Switch '/{name}' needs a value, e.g. /{name}:value.");
            }
            return value;
        }
    }
}
