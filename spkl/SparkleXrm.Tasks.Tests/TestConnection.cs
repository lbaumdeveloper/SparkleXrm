using System;
using Microsoft.Crm.Sdk.Messages;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Xrm.Tooling.Connector;

namespace SparkleXrm.Tasks.Tests
{
    /// <summary>
    /// Creates the connection for integration tests from the SPKL_TEST_CONNECTION environment variable.
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
                    Assert.Inconclusive($"Set {VariableName} to a connection string to run integration tests.");
                }
                return connectionString;
            }
        }

        public static CrmServiceClient Create()
        {
            var client = new CrmServiceClient(ConnectionString);
            if (!client.IsReady)
            {
                throw new InvalidOperationException(client.LastCrmError);
            }
            return client;
        }

        public static Guid GetMyUserId(this CrmServiceClient client)
        {
            return ((WhoAmIResponse)client.Execute(new WhoAmIRequest())).UserId;
        }
    }
}
