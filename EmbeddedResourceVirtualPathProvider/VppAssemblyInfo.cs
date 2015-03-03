using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace EmbeddedResourceVirtualPathProvider
{
    /// <summary>
    /// Stores information about the assembly registered in the VirtualPathProvider
    /// </summary>
    public class VppAssemblyInfo
    {
        /// <summary>
        /// Assembly
        /// </summary>
        public Assembly Assembly { get; set; }

        /// <summary>
        /// Path of the assembly's sources
        /// </summary>
        public string ProjectSourcePath { get; set; }

        /// <summary>
        /// List of folders in the source directory
        /// </summary>
        public List<string> ScannedSources { get; set; }
    }
}
