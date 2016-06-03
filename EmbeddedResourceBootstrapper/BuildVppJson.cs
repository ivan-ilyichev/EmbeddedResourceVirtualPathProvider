using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.Build.Framework;

namespace EmbeddedResourceBootstrapper
{
    public class BuildVppJson : Microsoft.Build.Utilities.AppDomainIsolatedTask
    {
        [Required]
        public string ProjectFile { get; set; }

        public override bool Execute()
        {
            if (!File.Exists(ProjectFile))
                throw new Exception($"File {ProjectFile} doesn't exist");

            File.WriteAllText("vpp.json", ProcessTask(ProjectFile));

            return true;
        }

        internal static string ProcessTask(string projectFilename)
        {
            var contents = File.ReadAllText(projectFilename);

            var embeddeddResourcesRegex = new Regex("<EmbeddedResource Include=\"(?<fileName>[^\"]+)\"[\\s]*/>", RegexOptions.IgnoreCase);
            var files = embeddeddResourcesRegex.Matches(contents).Cast<Match>().Select(m => m.Groups["fileName"]).ToList();

            return string.Join("\r\n", files); 
        }

        public static void Test()
        {
            var result = ProcessTask(@"d:\BoxFusion\OpenSource\EmbeddedResourceVirtualPathProvider-my\TestResourceLibrary\TestResourceLibrary.csproj");
        }
    }
}
