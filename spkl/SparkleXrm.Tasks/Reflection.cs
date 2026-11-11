using SparkleXrm.Tasks.Config;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace SparkleXrm.Tasks
{
    public class Reflection
    {
        public static string[] IgnoredAssemblies = new string[] {
            "Microsoft.Crm.Sdk.Proxy.dll",
            "Microsoft.IdentityModel.dll",
            "Microsoft.Xrm.Sdk.dll",
            "Microsoft.Xrm.Sdk.Workflow.dll",
            "Microsoft.IdentityModel.Clients.ActiveDirectory.dll",
            "Microsoft.Extensions.FileSystemGlobbing.dll",
            "Microsoft.IdentityModel.Clients.ActiveDirectory.WindowsForms.dll",
            "Microsoft.Xrm.Sdk.Deployment.dll",
            "Microsoft.Xrm.Tooling.Connector.dll",
            "Newtonsoft.Json.dll",
            "SparkleXrm.Tasks.dll",
            "System.Net.Http.dll",
            "Microsoft.Rest.ClientRuntime.dll"
        };

        /// <summary>
        /// Folders with the .NET Framework assemblies that plugin and workflow assemblies are compiled against.
        /// </summary>
        public static List<string> FrameworkDirectories { get; set; } = DefaultFrameworkDirectories();

        /// <summary>
        /// Creates a metadata-only context for inspecting a .NET Framework plugin assembly without loading it into spkl.
        /// Dependencies are resolved from the assembly's folder, the .NET Framework folder and spkl's own Dataverse SDK assemblies.
        /// </summary>
        public static MetadataLoadContext CreateContext(string assemblyPath)
        {
            var paths = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            AddAssemblies(paths, AppContext.BaseDirectory, "Microsoft.Xrm.Sdk.dll", "Microsoft.Crm.Sdk.Proxy.dll");
            foreach (var directory in FrameworkDirectories.Where(Directory.Exists))
            {
                AddAssemblies(paths, directory, "*.dll");
            }
            AddAssemblies(paths, Path.GetDirectoryName(Path.GetFullPath(assemblyPath)), "*.dll");
            paths[Path.GetFileName(assemblyPath)] = Path.GetFullPath(assemblyPath);

            return new MetadataLoadContext(new PathAssemblyResolver(paths.Values), "mscorlib");
        }

        public static IEnumerable<Type> GetTypesImplementingInterface(Assembly assembly, string interfaceFullName)
        {
            return assembly.GetExportedTypes()
                .Where(t => t.IsClass && TryGetInterfaces(t).Any(i => i.FullName == interfaceFullName))
                .ToList();
        }

        public static IEnumerable<Type> GetTypesInheritingFrom(Assembly assembly, string baseTypeFullName)
        {
            return assembly.GetTypes()
                .Where(t => t.IsClass && GetBaseTypeNames(t).Contains(baseTypeFullName))
                .ToList();
        }

        private static IEnumerable<Type> TryGetInterfaces(Type type)
        {
            try
            {
                return type.GetInterfaces();
            }
            catch (FileNotFoundException)
            {
                return Enumerable.Empty<Type>();
            }
        }

        private static IEnumerable<string> GetBaseTypeNames(Type type)
        {
            var names = new List<string>();
            try
            {
                for (var baseType = type.BaseType; baseType != null; baseType = baseType.BaseType)
                {
                    names.Add(baseType.FullName);
                }
            }
            catch (FileNotFoundException)
            {
            }
            return names;
        }

        private static void AddAssemblies(Dictionary<string, string> paths, string directory, params string[] patterns)
        {
            foreach (var pattern in patterns)
            {
                foreach (var file in Directory.GetFiles(directory, pattern))
                {
                    paths[Path.GetFileName(file)] = file;
                }
            }
        }

        private static List<string> DefaultFrameworkDirectories()
        {
            var windows = Environment.GetFolderPath(Environment.SpecialFolder.Windows);
            return new List<string>
            {
                Path.Combine(windows, @"Microsoft.NET\Framework64\v4.0.30319"),
                Path.Combine(windows, @"Microsoft.NET\Framework\v4.0.30319")
            }.Where(d => !string.IsNullOrEmpty(windows)).ToList();
        }

        public static IEnumerable<CustomAttributeData> GetAttributes(IEnumerable<Type> types, string attributeName)
        {
            List<CustomAttributeData> attributes = new List<CustomAttributeData>();
            foreach (Type type in types)
            {
                var data = type.GetCustomAttributesData().Where(a => a.AttributeType.Name == attributeName);
                // Don't allow multiple steps with the same name per type
                var duplicateNames = data.Select(a => a.CreateFromData()).GroupBy(s => s.Name??"").SelectMany(grp => grp.Skip(1));
                if (duplicateNames.Count() > 0)
                {
                    var names = string.Join(", ", duplicateNames.Select(a => a.Name).ToArray());
                    throw new SparkleTaskException(SparkleTaskException.ExceptionTypes.DUPLICATE_STEP, string.Format("Found types with duplicate attributes of the same name(s) {0}", names));
                }
                attributes.AddRange(data);
            }

            return attributes;
        }
    }
}