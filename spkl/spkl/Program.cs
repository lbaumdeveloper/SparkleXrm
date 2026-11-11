using Microsoft.Crm.Sdk.Messages;
using Microsoft.Xrm.Sdk;
using SparkleXrm.Tasks;
using Microsoft.Xrm.Sdk.Client;
using Microsoft.Crm.Sdk.Samples;
using Microsoft.Crm.Services.Utility;
using Microsoft.Xrm.Tooling.Connector;
using SparkleXrmTask.XrmToolingCmdLine;
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

            if (arguments.Connection == null && arguments.LegacyLogin)
            {
                RunWithLegacyLogin(arguments, trace);
            }
            else if (arguments.Connection == null)
            {
                var toolingConnector = new XrmToolingConnection();
                var client = toolingConnector.Connect();
                arguments.Connection = toolingConnector.ConnectionString;
                RunTask(arguments, client, trace);
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

                CrmServiceClient.MaxConnectionTimeout = TimeSpan.FromHours(1);
                using (var client = new CrmServiceClient(arguments.Connection))
                {
                    if (!client.IsReady)
                    {
                        throw new SparkleTaskException(SparkleTaskException.ExceptionTypes.AUTH_ERROR,
                            $"Error connecting to the Organization Service: {client.LastCrmError}");
                    }
                    RunTask(arguments, client, trace);
                }
            }
        }

        /// <summary>
        /// Console login for on-premises (AD / IFD) via discovery service and OrganizationServiceProxy.
        /// </summary>
        private static void RunWithLegacyLogin(CommandLineArgs arguments, ITrace trace)
        {
            Console.ForegroundColor = ConsoleColor.Blue;
            Console.WriteLine("On-premises Login");
            Console.ForegroundColor = ConsoleColor.Gray;

            var config = new ServerConnection().GetServerConfiguration(arguments.IgnoreLocalPrincipal);
            arguments.Connection = BuildConnectionString(config);

            using (var serviceProxy = new OrganizationServiceProxy(config.OrganizationUri, config.HomeRealmUri, config.Credentials, config.DeviceCredentials))
            {
                serviceProxy.EnableProxyTypes();
                serviceProxy.Timeout = TimeSpan.FromHours(1);
                RunTask(arguments, serviceProxy, trace);
            }
        }

        private static string BuildConnectionString(ServerConnection.Configuration config)
        {
            //string onlineRegion, organisationName;
            //bool isOnPrem;
            //Utilities.GetOrgnameAndOnlineRegionFromServiceUri(config.OrganizationUri, out onlineRegion, out organisationName, out isOnPrem);
            string connectionString;

            // On prem connection
            // AuthType = AD; Url = http://contoso:8080/Test;

            // AuthType=AD;Url=http://contoso:8080/Test; Domain=CONTOSO; Username=jsmith; Password=passcode

            // Office 365
            // AuthType = Office365; Username = jsmith@contoso.onmicrosoft.com; Password = passcode; Url = https://contoso.crm.dynamics.com

            // IFD
            // AuthType=IFD;Url=http://contoso:8080/Test; HomeRealmUri=https://server-1.server.com/adfs/services/trust/mex/;Domain=CONTOSO; Username=jsmith; Password=passcode

            switch (config.EndpointType)
            {
                case AuthenticationProviderType.ActiveDirectory:
                    connectionString = String.Format("AuthType=AD;Url={0}", config.OrganizationUri);

                    break;

                case AuthenticationProviderType.Federation:
                    connectionString = String.Format("AuthType=IFD;Url={0}", config.OrganizationUri);
                    break;

                case AuthenticationProviderType.OnlineFederation:
                    connectionString = String.Format("AuthType=Office365;Url={0}", config.OrganizationUri);
                    break;

                default:
                    throw new SparkleTaskException(SparkleTaskException.ExceptionTypes.AUTH_ERROR, String.Format("Unsupported Endpoint Type {0}", config.EndpointType.ToString()));
            }

            if (config.Credentials != null && config.Credentials.Windows != null)
            {
                if (!String.IsNullOrEmpty(config.Credentials.Windows.ClientCredential.Domain))
                {
                    connectionString += ";DOMAIN=" + config.Credentials.Windows.ClientCredential.Domain;
                }

                if (!String.IsNullOrEmpty(config.Credentials.Windows.ClientCredential.UserName))
                {
                    connectionString += ";Username=" + config.Credentials.Windows.ClientCredential.UserName;
                }

                if (!String.IsNullOrEmpty(config.Credentials.Windows.ClientCredential.Password))
                {
                    connectionString += ";Password=" + config.Credentials.Windows.ClientCredential.Password;
                }
                else if (config.Credentials.Windows.ClientCredential.SecurePassword != null && config.Credentials.Windows.ClientCredential.SecurePassword.Length > 0)
                {
                    var password = new System.Net.NetworkCredential(string.Empty, config.Credentials.Windows.ClientCredential.SecurePassword).Password;
                    connectionString += ";Password=" + password;
                }
            }

            if (config.Credentials != null)
            {
                if (!String.IsNullOrEmpty(config.Credentials.UserName.UserName))
                {
                    connectionString += ";Username=" + config.Credentials.UserName.UserName;
                }
                if (!String.IsNullOrEmpty(config.Credentials.UserName.Password))
                {
                    connectionString += ";Password=" + config.Credentials.UserName.Password;
                }
            }

            if (config.HomeRealmUri != null)
            {
                connectionString += ";HomeRealmUri=" + config.HomeRealmUri.ToString();
            }

            return connectionString;
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

            DryRunOrganizationService dryRunService = null;
            if (arguments.DryRun)
            {
                trace.WriteLine("Dry run: reading from the server, nothing will be written.");
                dryRunService = new DryRunOrganizationService(service, trace);
                service = dryRunService;
            }
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
                dryRunService?.WriteSummary();
            }
            else
                throw new SparkleTaskException(SparkleTaskException.ExceptionTypes.NO_TASK_SUPPLIED, String.Format("Task '{0}' not recognised. Please consult help!", arguments.Task.ToLower()));
        }
    }
}
