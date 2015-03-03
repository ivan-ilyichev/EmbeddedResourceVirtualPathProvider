using System.Web.Hosting;
using EmbeddedResourceVirtualPathProvider;
using TestResourceLibrary;

namespace TestWebProject.App_Code
{
    public class RegisterVirtualPathProvider
    {
        public static void AppInitialize()
        {
            var vpp = new EmbeddedResourceVirtualPathProvider.Vpp()
            {
                { typeof (Marker).Assembly, @"..\TestResourceLibrary" },
            };
            vpp.UseLocalIfAvailable = r => true;

            HostingEnvironment.RegisterVirtualPathProvider(vpp);
        }
    }
}