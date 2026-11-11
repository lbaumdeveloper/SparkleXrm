using Microsoft.PowerPlatform.Dataverse.Client;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;

namespace SparkleXrmTask.Connection
{
    /// <summary>
    /// Console login for Dataverse with saved environments. Only environment URL, user name and display name are stored;
    /// tokens are cached by the Dataverse client in the token cache folder.
    /// </summary>
    public class InteractiveConnection
    {
        private const string AppId = "51f81489-12ee-4a9e-aaae-a2591f45987d";
        private const string RedirectUri = "http://localhost";

        private static readonly string SettingsFolder = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "spkl");

        public static readonly string ConnectionsFilePath = Path.Combine(SettingsFolder, "Connections.json");
        public static readonly string TokenPath = Path.Combine(SettingsFolder, "TokenCache");

        private List<SavedConnection> _savedConnections = new List<SavedConnection>();

        public string ConnectionString { get; private set; }

        public InteractiveConnection()
        {
            LoadSettings();
        }

        public ServiceClient Connect()
        {
            Console.ForegroundColor = ConsoleColor.Blue;
            Console.WriteLine("Dataverse Login");
            Console.ForegroundColor = ConsoleColor.Gray;

            var connectionStringRoot = $"AuthType=OAuth;AppId={AppId};RedirectUri={RedirectUri};TokenCacheStorePath={TokenPath}";

            var configNumber = SelectSavedConnection();

            SavedConnection selectedConnection;
            var loginPrompt = "Auto";
            var newConnection = configNumber <= 0;
            if (newConnection)
            {
                selectedConnection = new SavedConnection
                {
                    EnvironmentUrl = ReadLine("Environment Url (e.g. org123.crm4.dynamics.com)", @"^(?<!http)([^\s:\/]+)(\.crm[0-9]*\.dynamics\.com[\/]?)$").TrimEnd('/')
                };
                loginPrompt = "Always";
            }
            else
            {
                selectedConnection = _savedConnections[configNumber - 1];
                _savedConnections.Remove(selectedConnection);
                _savedConnections.Add(selectedConnection);
                SaveSettings();
                connectionStringRoot += $";Username={selectedConnection.UserName}";
            }

            var connectionString = $"{connectionStringRoot};Url=https://{selectedConnection.EnvironmentUrl};LoginPrompt={loginPrompt}";
            var client = new ServiceClient(connectionString);
            if (!client.IsReady)
            {
                throw new Exception($"Cannot connect: {client.LastError}", client.LastException);
            }

            if (newConnection)
            {
                selectedConnection.EnvironmentUrl = client.ConnectedOrgUriActual.Host;
                selectedConnection.UserName = client.OAuthUserId;
                selectedConnection.DisplayName = client.ConnectedOrgFriendlyName;
                connectionString += $";Username={selectedConnection.UserName}";
                _savedConnections.RemoveAll(c =>
                    c.EnvironmentUrl.Equals(selectedConnection.EnvironmentUrl, StringComparison.InvariantCultureIgnoreCase) &&
                    string.Equals(c.UserName, selectedConnection.UserName, StringComparison.InvariantCultureIgnoreCase));
                _savedConnections.Add(selectedConnection);
                SaveSettings();
            }

            ConnectionString = connectionString;
            return client;
        }

        private int SelectSavedConnection()
        {
            if (_savedConnections.Count == 0)
            {
                return -1;
            }

            Console.WriteLine("(0) Add New Server Configuration");
            for (var i = 0; i < _savedConnections.Count; i++)
            {
                var connection = _savedConnections[i];
                Console.WriteLine($"({i + 1}) {connection.EnvironmentUrl},  {connection.DisplayName},  {connection.UserName}");
            }

            Console.Write($"\nSpecify the saved server configuration number (0-{_savedConnections.Count}) [{_savedConnections.Count}] : ");
            var input = Console.ReadLine();
            Console.WriteLine();

            if (string.IsNullOrEmpty(input))
            {
                return _savedConnections.Count;
            }

            return int.TryParse(input, out var number) && number <= _savedConnections.Count ? number : -1;
        }

        private void LoadSettings()
        {
            if (File.Exists(ConnectionsFilePath))
            {
                _savedConnections = JsonConvert.DeserializeObject<List<SavedConnection>>(File.ReadAllText(ConnectionsFilePath)) ?? new List<SavedConnection>();
            }
        }

        private void SaveSettings()
        {
            Directory.CreateDirectory(SettingsFolder);
            File.WriteAllText(ConnectionsFilePath, JsonConvert.SerializeObject(_savedConnections));
        }

        private static string ReadLine(string prompt, string regex)
        {
            while (true)
            {
                Console.Write(prompt + ": ");
                var input = Console.ReadLine() ?? string.Empty;
                Console.WriteLine();

                if (input.Length == 0)
                {
                    Console.WriteLine("Input Required");
                }
                else if (!Regex.IsMatch(input, regex))
                {
                    Console.WriteLine("Input invalid");
                }
                else
                {
                    return input;
                }
            }
        }
    }
}
