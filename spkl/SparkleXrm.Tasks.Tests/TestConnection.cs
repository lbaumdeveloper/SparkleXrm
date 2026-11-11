using System;
using Microsoft.Crm.Sdk.Messages;
using Microsoft.PowerPlatform.Dataverse.Client;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace SparkleXrm.Tasks.Tests
{
    /// <summary>
    /// Creates the Dataverse connection for integration tests from the SPKL_TEST_CONNECTION environment variable.
    /// </summary>
    public static class TestConnection
    {
        public const string VariableName = "SPKL_TEST_CONNECTION";

        public static string ConnectionString
        {
            get
            {
                var connectionString = Environment.GetEnvironmentVariable(VariableName);
                if (string.IsNullOrWhiteSpace(connectionString))
                {
                    Assert.Inconclusive($"Set {VariableName} to a Dataverse connection string to run integration tests.");
                }
                return connectionString;
            }
        }

        public static ServiceClient Create()
        {
            var client = new ServiceClient(ConnectionString);
            if (!client.IsReady)
            {
                throw new InvalidOperationException(client.LastError);
            }
            return client;
        }

        public static Guid GetMyUserId(this ServiceClient client)
        {
            return ((WhoAmIResponse)client.Execute(new WhoAmIRequest())).UserId;
        }
    }
}
