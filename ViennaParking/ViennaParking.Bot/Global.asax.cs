using Autofac;
using Microsoft.Bot.Builder.Dialogs.Internals;
using Microsoft.Bot.Builder.Internals.Fibers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Http;
using System.Web.Routing;

namespace ViennaParking.Bot
{
    public class WebApiApplication : System.Web.HttpApplication
    {
        static ContainerBuilder builder = new ContainerBuilder();

        protected void Application_Start()
        {
            var builder = new ContainerBuilder();
            builder.RegisterModule(new DialogModule());
            builder.RegisterModule(new ReflectionSurrogateModule());
            builder.Build();


            SqlServerTypes.Utilities.LoadNativeAssemblies(Server.MapPath("~/bin"));

            GlobalConfiguration.Configure(WebApiConfig.Register);
        }
    }
}
