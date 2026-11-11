using FakeItEasy;
using Microsoft.Crm.Sdk.Messages;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Client;
using Microsoft.Xrm.Sdk.Messages;
using Microsoft.Xrm.Sdk.Query;
using SparkleXrm.Tasks.Config;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace SparkleXrm.Tasks.Tests
{
    [TestClass]
    public class DryRunTests
    {
        private class ListTrace : ITrace
        {
            public List<string> Lines { get; } = new List<string>();

            public void WriteLine(string format, params object[] args)
            {
                if (format != null)
                {
                    Lines.Add(string.Format(format, args));
                }
            }

            public void Write(string format, params object[] args)
            {
                WriteLine(format, args);
            }
        }

        [TestMethod]
        [TestCategory("Unit Tests")]
        public void WritesAreNotForwarded()
        {
            var inner = A.Fake<IOrganizationService>(o => o.Strict());
            var trace = new ListTrace();
            var service = new DryRunOrganizationService(inner, trace);

            var id = service.Create(new Entity("webresource") { ["name"] = "new_/script.js" });
            service.Update(new Entity("plugintype", Guid.NewGuid()) { ["name"] = "Plugins.Account" });
            service.Delete("sdkmessageprocessingstep", Guid.NewGuid());
            service.Execute(new AddSolutionComponentRequest { ComponentId = id, ComponentType = 61, SolutionUniqueName = "Test" });
            service.Execute(new PublishXmlRequest { ParameterXml = "<importexportxml/>" });
            var created = (CreateResponse)service.Execute(new CreateRequest { Target = new Entity("pluginassembly") { ["name"] = "Plugins" } });

            Assert.AreNotEqual(Guid.Empty, id);
            Assert.AreNotEqual(Guid.Empty, created.id);
            Assert.AreEqual(2, service.Creates);
            Assert.AreEqual(1, service.Updates);
            Assert.AreEqual(1, service.Deletes);
            Assert.AreEqual(2, service.OtherRequests);
            Assert.IsTrue(trace.Lines.Any(l => l.Contains("Would create webresource 'new_/script.js'")));
            Assert.IsTrue(trace.Lines.Any(l => l.Contains("Would execute AddSolutionComponent") && l.Contains("SolutionUniqueName=Test")));
        }

        [TestMethod]
        [TestCategory("Unit Tests")]
        public void CreateKeepsPresetId()
        {
            var service = new DryRunOrganizationService(A.Fake<IOrganizationService>(o => o.Strict()), new ListTrace());
            var id = Guid.NewGuid();

            Assert.AreEqual(id, service.Create(new Entity("sdkmessageprocessingstep", id)));
        }

        [TestMethod]
        [TestCategory("Unit Tests")]
        public void ReadsAreForwarded()
        {
            var inner = A.Fake<IOrganizationService>(o => o.Strict());
            var expected = new RetrieveMultipleResponse();
            A.CallTo(() => inner.Execute(A<OrganizationRequest>.That.Matches(r => r is RetrieveMultipleRequest))).Returns(expected);
            A.CallTo(() => inner.RetrieveMultiple(A<QueryBase>.Ignored)).Returns(new EntityCollection());
            A.CallTo(() => inner.Retrieve(A<string>.Ignored, A<Guid>.Ignored, A<ColumnSet>.Ignored)).Returns(new Entity("webresource"));
            var service = new DryRunOrganizationService(inner, new ListTrace());

            Assert.AreSame(expected, service.Execute(new RetrieveMultipleRequest { Query = new QueryExpression("webresource") }));
            service.RetrieveMultiple(new QueryExpression("webresource"));
            service.Retrieve("webresource", Guid.NewGuid(), new ColumnSet());

            Assert.AreEqual(0, service.Creates + service.Updates + service.Deletes + service.OtherRequests);
        }

        [TestMethod]
        [TestCategory("Unit Tests")]
        public void WebresourceDeployWritesNothing()
        {
            ServiceLocator.Init();
            var inner = A.Fake<IOrganizationService>(o => o.Strict());
            A.CallTo(() => inner.Execute(A<OrganizationRequest>.That.Matches(r => r is RetrieveMultipleRequest)))
                .ReturnsLazily(() =>
                {
                    var response = new RetrieveMultipleResponse();
                    response.Results["EntityCollection"] = new EntityCollection();
                    return response;
                });
            var trace = new ListTrace();
            var service = new DryRunOrganizationService(inner, trace);

            var folder = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            Directory.CreateDirectory(Path.Combine(folder, "new_"));
            File.WriteAllText(Path.Combine(folder, "new_", "script.js"), "var a = 1;");
            var config = new ConfigFile
            {
                filePath = folder,
                webresources = new List<WebresourceDeployConfig>
                {
                    new WebresourceDeployConfig
                    {
                        solution = "Test",
                        files = new List<WebResourceFile> { new WebResourceFile { file = @"new_\script.js" } }
                    }
                }
            };

            try
            {
                using (var ctx = new OrganizationServiceContext(service))
                {
                    var task = new DeployWebResourcesTask(service, trace);
                    var ids = task.DeployWebresources(ctx, config, config.webresources[0]);
                    task.PublishWebresources(ids);
                }
            }
            finally
            {
                Directory.Delete(folder, true);
            }

            Assert.AreEqual(1, service.Creates);
            Assert.AreEqual(2, service.OtherRequests);
            A.CallTo(() => inner.Create(A<Entity>.Ignored)).MustNotHaveHappened();
        }
    }
}
