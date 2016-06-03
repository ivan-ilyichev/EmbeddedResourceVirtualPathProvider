using Microsoft.Owin;
using Owin;

[assembly: OwinStartupAttribute(typeof(TestMvc.Startup))]
namespace TestMvc
{
    public partial class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            ConfigureAuth(app);
        }
    }
}
