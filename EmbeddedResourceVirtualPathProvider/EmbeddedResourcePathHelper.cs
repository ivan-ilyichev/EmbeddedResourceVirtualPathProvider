using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;

namespace EmbeddedResourceVirtualPathProvider
{
    /// <summary>
    /// Searchs file paths in the debug mode
    /// </summary>
    public static class EmbeddedResourcePathHelper
    {
        private static Dictionary<string, string> _pathMappings = new Dictionary<string, string>();

        public static string GetPath(string projectSourcePath, string resourceName)
        {
            if (_pathMappings.ContainsKey(resourceName))
                return _pathMappings[resourceName];

            string path = null;

            if (!Directory.Exists(projectSourcePath))
                return null;

            var fileName = Path.Combine(projectSourcePath, resourceName);

            // search all subdirectories with dashes
            var subDirectories = Directory.GetDirectories(projectSourcePath, "*", SearchOption.AllDirectories);
            var dottedDirectories = subDirectories
                .Select(d => d.Substring(projectSourcePath.Length + 1))
                .Where(d => d.Contains("."))
                .ToList();

            foreach (var dir in dottedDirectories)
            {
                var slashedDir = dir.Replace('.', '\\');
                // replace dashed path 
                if (fileName.StartsWith(slashedDir, true, CultureInfo.InvariantCulture))
                {
                    fileName = dir + fileName.Substring(slashedDir.Length);
                    break;
                }
            }

            fileName = GetFileName(fileName);

            return fileName;
        }

        private static string GetFileName(string possibleFileName)
        {
            var indexOfLastSlash = possibleFileName.LastIndexOf('\\');
            while (indexOfLastSlash > -1)
            {
                if (File.Exists(possibleFileName)) 
                    return possibleFileName;
                possibleFileName = ReplaceChar(possibleFileName, indexOfLastSlash, '.');
                indexOfLastSlash = possibleFileName.LastIndexOf('\\');
            }
            return null;
        }

        private static string ReplaceChar(string text, int index, char charToUse)
        {
            char[] buffer = text.ToCharArray();
            buffer[index] = charToUse;
            return new string(buffer);
        }
    }
}
