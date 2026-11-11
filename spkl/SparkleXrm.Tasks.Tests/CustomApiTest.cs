using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using System;
using System.IO;

namespace SparkleXrm.Tasks.Tests
{
    [TestClass]
    public class CustomApiTest
    {
        [TestMethod]
        [TestCategory("Integration Tests")]
        public void DeployCustomApi()
        {
            var crmSvc = TestConnection.Create();
            var userId = crmSvc.GetMyUserId();
            var trace = new TraceLogger();
           

            // Check if the API exists
            var fetchQuery = @"<fetch top='50' >
                              <entity name='customapi' >
                                <filter>
                                  <condition attribute='uniquename' operator='eq' value='spkl_CustomApiTest' />
                                </filter>
                              </entity>
                            </fetch>";

            var existing = crmSvc.RetrieveMultiple(new FetchExpression(fetchQuery));
            if (existing.Entities.Count == 0)
            {
                // Create custom API
                var customApi = new Entity("customapi")
                {
                    Attributes = {
                { "allowedcustomprocessingsteptype", new OptionSetValue(0)}, //None
                { "bindingtype", new OptionSetValue(0)}, //Global
                { "description","Integration test for spkl" },
                { "displayname","Custom API Example" },
                { "executeprivilegename", null },
                { "isfunction", false},
                { "isprivate", false},
                { "name","spkl_CustomApiTest"},
                { "uniquename","spkl_CustomApiTest"}
            }
                };

                Guid customApiId = crmSvc.Create(customApi);
            }
        }
    }
}