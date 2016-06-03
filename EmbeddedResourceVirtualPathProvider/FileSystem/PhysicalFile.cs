using System;
using System.IO;
using System.Web.Hosting;

namespace EmbeddedResourceVirtualPathProvider.FileSystem
{
    internal class PhysicalFile : VirtualFile
    {
        private readonly FileInfo _fileInfo;

        /// <summary>
        /// Initializes a new instance of the <see cref="PhysicalFile" /> class.
        /// </summary>
        /// <param name="virtualPath">The virtual path to the resource represented by this instance.</param>
        /// <param name="fileInfo">The file info.</param>
        public PhysicalFile(string virtualPath, FileInfo fileInfo)
            : base(virtualPath.StartsWith("~", StringComparison.OrdinalIgnoreCase) ? virtualPath : "~" + virtualPath)
        {
            this._fileInfo = fileInfo;
        }

        public override System.IO.Stream Open()
        {
            return File.Open(this._fileInfo.FullName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        }
    }
}