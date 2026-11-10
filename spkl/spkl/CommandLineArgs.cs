using CmdLine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SparkleXrmTask
{
    [CommandLineArguments(Program = "spkl", Title = "SparkleXrm Deployment Tasks", Description = "Let SparkleXrm do the work for you!")]
    public class CommandLineArgs
    {
        [CommandLineParameter(Command = "?", Default = false, Description = "Show Help", Name = "Help", IsHelp = true)]
        public bool Help { get; set; }

        [CommandLineParameter(Name = "task", ParameterIndex = 1, Required = true, Description = @"
plugins = Deploy plugins and Custom API plugin types
workflow = Deploy custom workflow activities
webresources = Deploy and publish webresources
instrument = Read registrations from Dynamics 365 and add [CrmPluginRegistration] attributes to existing classes
get-webresources = Match local webresource files to webresources in Dynamics 365 and add them to spkl.json
download-webresources = Download the webresources of a solution to local files
earlybound = Create earlybound types for C# development
unpack = Export a solution from Dynamics 365 and unpack it into xml files & folders
unpacksolution = Unpack a solution zip from the filesystem into xml files & folders
pack = Pack an unpacked solution into a solution zip, taking assemblies and webresources from their mapped locations
import = Pack a solution as per the 'pack' task, then import it into Dynamics 365 and publish
export = Export a solution from Dynamics 365 as zip file(s)
whoami = Show the organization and user of the connection
")]
        public string Task { get; set; }

        [CommandLineParameter(Name = "Overwrite", Command = "o", Required = false, Description = "Optional overwrite webresource files when downloading webresources")]
        public bool Overwrite { get; set; }

        [CommandLineParameter(Name = "path", ParameterIndex = 2, Required = false, Description = "Optional path to spkl.json file")]
        public string Path { get; set; }

        [CommandLineParameter(Name = "connection", ParameterIndex = 3, Required = false, Description = "Optional Connection String")]
        public string Connection { get; set; }

        [CommandLineParameter(Name = "profile", Command = "p", ParameterIndex = 4, Required = false, Description = "Optional profile name. If ommitted, default will be used")]
        public string Profile { get; set; }

        [CommandLineParameter(Name = "solution prefix", Command = "s", Required = false, Description = "Optional Prefix to filter webresources when downloading the config.")]
        public string Prefix { get; set; }

        [CommandLineParameter(Name = "Wait for keypress", Command = "w", Required = false, Description = "Optional wait for a key press at the end of task run")]
        public bool WaitForKey { get; set; }

        [CommandLineParameter(Name = "Ignore Windows login", Command = "i", Required = false, Description = "Optional flag to ignore logged in windows credentials during discovery and always ask for username/password.")]
        public bool IgnoreLocalPrincipal { get; set; }

        [CommandLineParameter(Name = "Exclude Plugin Steps", Command = "e", Required = false, Description = "Exclude plugin steps when deploying plugins")]
        public bool ExcludePluginSteps { get; set; }

        [CommandLineParameter(Name = "Legacy Login Mode", Command = "l", Required = false, Description = "Use the legacy command line login mode")]
        public bool LegacyLogin { get; set; }
    }
}