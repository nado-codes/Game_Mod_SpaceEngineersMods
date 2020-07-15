using Microsoft.Owin;
using Owin;

[assembly: OwinStartupAttribute(typeof(ASP_MyBSNList.Startup))]
namespace ASP_MyBSNList
{
    public partial class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            ConfigureAuth(app);
        }
    }
}
