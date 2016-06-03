using System;
using System.Web.Hosting;

namespace EmbeddedResourceVirtualPathProvider.FileSystem
{
    internal class EmbeddedFile : VirtualFile
    {
        private readonly IResourceFile _resourcesFile;

        /// <summary>
        /// Initializes a new instance of the <see cref="EmbeddedFile" /> class.
        /// </summary>
        /// <param name="virtualPath">The virtual path to the resource represented by this instance.</param>
        /// <param name="resourcesFile">The embedded resource file.</param>
        public EmbeddedFile(string virtualPath, IResourceFile resourcesFile)
            : base(virtualPath.StartsWith("~", StringComparison.OrdinalIgnoreCase) ? virtualPath : "~" + virtualPath)
        {
            this._resourcesFile = resourcesFile;
        }

        public override System.IO.Stream Open()
        {
            return this._resourcesFile.GetResourceStream();
        }
    }
}