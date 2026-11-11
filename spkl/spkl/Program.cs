using Microsoft.Crm.Sdk.Messages;
using Microsoft.PowerPlatform.Dataverse.Client;
using Microsoft.Xrm.Sdk;
using SparkleXrm.Tasks;
using SparkleXrmTask.Connection;
using System;
using System.IO;
using System.Reflection;
using System.ServiceModel;
using System.Text.RegularExpressions;

namespace SparkleXrmTask
{
    internal class Program
    {
        private static int Main(string[] args)
        {
            Console.ForegroundColor = ConsoleColor.DarkYellow;
            Console.WriteLine(@"
                __   .__
  ____________ |  | _|  |
 /  ___/\____ \|  |/ /  |
 \___ \ |  |_> >    <|  |__
/____  >|   __/|__|_ \____/
     \/ |__|        \/     ");

            Console.WriteLine("spkl Task Runner v" + Assembly.GetEntryAssembly().GetName().Version + "\tTasks v" + typeof(BaseTask).Assembly.GetName().Version);
            Console.ForegroundColor = ConsoleColor.Gray;

            CommandLineArgs arguments = null;
            var exitCode = 0;
            try
            {
                arguments = CommandLineArgs.Parse(args);
                if (arguments.Help)
                {
                    Console.WriteLine(CommandLineArgs.HelpText);
                }
                else
                {
                    Run(arguments);
                }
            }
            catch (ArgumentException ex)
            {
                Console.WriteLine(ex.Message);
                Console.WriteLine(CommandLineArgs.HelpText);
                exitCode = 1;
            }
            catch (SparkleTaskException ex)
            {
                Console.WriteLine(ex.Message);
                exitCode = 1;
            }
            catch (Exception ex)
            {
                WriteError(ex);
                exitCode = 1;
            }

            if (arguments != null && arguments.WaitForKey)
            {
                Console.ForegroundColor = ConsoleColor.White;
                Console.WriteLine("Press any key...");
                Console.ReadKey();
            }
            Console.ForegroundColor = ConsoleColor.Gray;
            return exitCode;
        }

        private static void WriteError(Exception ex)
        {
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine("The application terminated with an error.");
            Console.WriteLine(ex.Message);
            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.WriteLine(ex.StackTrace);

            var fault = (ex as FaultException<OrganizationServiceFault>) ?? (ex.InnerException as FaultException<OrganizationServiceFault>);
            if (fault != null)
            {
                Console.WriteLine("Timestamp: {0}", fault.Detail.Timestamp);
                Console.WriteLine("Code: {0}", fault.Detail.ErrorCode);
                Console.WriteLine("Message: {0}", fault.Detail.Message);
                if (!string.IsNullOrEmpty(fault.Detail.TraceText))
                {
                    Console.WriteLine("Plugin Trace: {0}", fault.Detail.TraceText);
                }
            }
            else if (ex.InnerException != null)
            {
                Console.WriteLine(ex.InnerException.Message);
            }
            Console.ForegroundColor = ConsoleColor.White;
        }

        private static void Run(CommandLineArgs arguments)
        {
            var trace = new TraceLogger();

            if (arguments.Connection == null)
            {
                var interactive = new InteractiveConnection();
                using (var client = interactive.Connect())
                {
                    arguments.Connection = interactive.ConnectionString;
                    RunTask(arguments, client, trace);
                }
            }
            else if (arguments.Connection == "")
            {
                RunTask(arguments, null, trace);
            }
            else
            {
                var passwordMatch = Regex.Match(arguments.Connection, "Password=[*]+", RegexOptions.IgnoreCase);
                if (passwordMatch.Success)
                {
                    Console.Write("Password:");
                    var password = ConsoleEx.ReadPassword('*');
                    arguments.Connection = arguments.Connection.Replace(passwordMatch.Value, "Password=" + password);
                }

                ServiceClient.MaxConnectionTimeout = TimeSpan.FromHours(1);
                using (var client = new ServiceClient(arguments.Connection))
                {
                    if (!client.IsReady)
                    {
                        throw new SparkleTaskException(SparkleTaskException.ExceptionTypes.AUTH_ERROR,
                            $"Error connecting to Dataverse: {client.LastError}");
                    }
                    RunTask(arguments, client, trace);
                }
            }
        }

        private static void RunTask(CommandLineArgs arguments, IOrganizationService service, ITrace trace)
        {
            if (arguments.Path == null)
            {
                arguments.Path = Directory.GetCurrentDirectory();
            }
            else
            {
                // Strip trailing \
                arguments.Path = arguments.Path.TrimEnd('\\');
                arguments.Path = Path.Combine(Directory.GetCurrentDirectory(), arguments.Path);
            }

            BaseTask task = null;
            string command = arguments.Task.ToLower();
            switch (command)
            {
                case "whoami":
                    trace.WriteLine("Who Am I?");
                    var whoAmIResponse = service.Execute(new WhoAmIRequest()) as WhoAmIResponse;
                    Console.WriteLine($"OrgId:'{whoAmIResponse.OrganizationId}' UserId:'{whoAmIResponse.UserId}'");
                    return;

                case "plugins":
                    trace.WriteLine("Deploying Plugins");
                    task = new DeployPluginsTask(service, trace)
                    { ExcludePluginSteps = arguments.ExcludePluginSteps };
                    break;

                case "workflow":
                    trace.WriteLine("Deploying Custom Workflow Activities");
                    task = new DeployWorkflowActivitiesTask(service, trace);
                    break;

                case "webresources":
                    trace.WriteLine("Deploying WebResources");
                    task = new DeployWebResourcesTask(service, trace);
                    break;

                case "instrument":
                    trace.WriteLine("Downloading Plugin/Workflow Activity Metadata");
                    task = new DownloadPluginMetadataTask(service, trace);
                    break;

                case "download-webresources":
                    trace.WriteLine("Downloading Webresources");
                    task = new DownloadWebresourceFileTask(service, trace)
                    { Overwrite = arguments.Overwrite };
                    break;

                case "get-webresources":
                    trace.WriteLine("Downloading Webresources");
                    task = new DownloadWebresourceConfigTask(service, trace);
                    task.Prefix = arguments.Prefix;
                    break;

                case "earlybound":
                    trace.WriteLine("Generating early bound types");
                    var earlyBound = new EarlyBoundClassGeneratorTask(service, trace);
                    task = earlyBound;
                    earlyBound.ConectionString = arguments.Connection;
                    break;

                case "unpack":
                    trace.WriteLine("Unpacking solution");
                    var packager = new SolutionPackagerTask(service, trace);
                    packager.command = command;
                    task = packager;
                    break;

                case "unpacksolution":
                    trace.WriteLine("Unpacking solution Zip");
                    var unpackfromsolution = new SolutionPackagerTask(service, trace);
                    unpackfromsolution.command = command;
                    task = unpackfromsolution;
                    break;

                case "pack":
                    trace.WriteLine("Packing Solution");
                    var pack = new SolutionPackagerTask(service, trace);
                    pack.command = command;
                    task = pack;
                    break;

                case "import":
                    trace.WriteLine("Packing & Import Solution");
                    var import = new SolutionPackagerTask(service, trace);
                    import.command = command;
                    task = import;
                    break;

                case "export":
                    trace.WriteLine("Exporting Solution");
                    var export = new SolutionPackagerTask(service, trace);
                    export.command = command;
                    task = export;
                    break;

            }

            if (task != null)
            {
                if (arguments.Profile != null)
                    task.Profile = arguments.Profile;
                task.Execute(arguments.Path);
            }
            else
                throw new SparkleTaskException(SparkleTaskException.ExceptionTypes.NO_TASK_SUPPLIED, String.Format("Task '{0}' not recognised. Please consult help!", arguments.Task.ToLower()));
        }
    }
}
