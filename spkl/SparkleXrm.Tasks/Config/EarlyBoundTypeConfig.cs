using Newtonsoft.Json;
using System.Collections.Generic;

namespace SparkleXrm.Tasks.Config
{
    public class EarlyBoundTypeConfig
    {
        public string profile;

        [JsonConverter(typeof(StringListConverter))]
        public List<string> entities;

        [JsonConverter(typeof(StringListConverter))]
        public List<string> actions;
        public bool generateOptionsetEnums;
        public bool generateGlobalOptionsets;
        public bool generateStateEnums;
        public string filename;
        public string classNamespace;
        public string serviceContextName;
        public bool oneTypePerFile;
    }
}