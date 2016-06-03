using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using Microsoft.Build.Framework;
using Newtonsoft.Json;

namespace EmbeddedResourceVirtualPathProvider.Tasks
{
    public class BuildResourceMetadata : Microsoft.Build.Utilities.AppDomainIsolatedTask
    {
        [Required]
        public string RelativePath { get; set; }

        [Required]
        public ITaskItem[] Resources { get; set; }

        public string Keys { get; set; }

        public string Values { get; set; }

        public override bool Execute()
        {
            File.WriteAllText("rpmetadata.json", ProcessTask());

            return true;
        }

        internal string ProcessTask()
        {
            if (!RelativePath.EndsWith(@"\")) RelativePath += @"\";

            var keysArray = (Keys ?? String.Empty).Replace("/", "\\").Split(';');
            var valuesArray = (Values ?? String.Empty).Replace("/", "\\").Split(';');

            var transposePaths = keysArray.Select((x, i) => new { Key = x.TrimStart('\\'), Value = valuesArray[i].TrimStart('\\') })
                .Where(x => !String.IsNullOrWhiteSpace(x.Key))
                .ToDictionary(x => x.Key, x => x.Value);

            var items = new Dictionary<string, string[]>();
            foreach (var item in Resources)
            {
                var path = item.GetMetadata("Fullpath").Substring(RelativePath.Length);
                var paths = new List<string>();
                if (transposePaths.Any())
                    paths.AddRange(GetRelativeResourcePaths(path, transposePaths));

                paths.Add(path);
                items.Add(path, paths.ToArray());
            }

            var data = new { Files = items, ProjectPath = RelativePath + @"..\", };

            return JsonConvert.SerializeObject(data);
        }

        private IEnumerable<string> GetRelativeResourcePaths(string fullpath, IEnumerable<KeyValuePair<string, string>> transposePaths)
        {
            var newFullpath = fullpath;
            foreach (var transpose in transposePaths)
            {
                var index = newFullpath.IndexOf(transpose.Key, StringComparison.OrdinalIgnoreCase);
                if (index > -1)
                {
                    yield return String.Format(CultureInfo.InvariantCulture, "{0}{1}{2}",
                        newFullpath.Substring(0, index),
                        transpose.Value,
                        newFullpath.Substring(index + transpose.Key.Length));
                }
            }
        }
    }

}
