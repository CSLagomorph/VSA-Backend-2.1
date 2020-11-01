using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Web.Http;
using System.Web.Http.Routing;

namespace VaaApi
{
    public static class WebApiConfig
    {
        public static void Register(HttpConfiguration config)
        {
            // Web API configuration and services
            config.EnableCors();
            
            // Web API routes
            config.MapHttpAttributeRoutes();

            // "routeTemplate":
            //      the structure of allowed HTTP Request strings that get concatenated to the host domain name;
            //      for example --> if the VAA software runs on "my.server.com", then an HTTP GET or POST request
            //      sent to the following sample paths (both valid) will be recognized by our [EnableCors]-enabled web API controllers:
            //          - "my.server.com/api/SomeController/9999"
            //          - "my.server.com/api/Another_controller"
            //
            // "defaults":
            //      you can specify default parameters, even ones that don't appear in the <routeTemplate>;
            //      parameters that DON'T appear in routeTemplate (as far as i know) can't be changed from their default value(s);
            //      for example --> this is valid:
            //          - defaults: new { invisible_param = "Spooky", id = RouteParameter.Optional }
            config.Routes.MapHttpRoute(
                name: "DefaultApi",
                routeTemplate: "api/{controller}/{id}",
                defaults: new { id = RouteParameter.Optional }
            );
            //config.Routes.MapHttpRoute("DefaultApiGet", "Api/{controller}", new { action = "Get" }, new { httpMethod = new HttpMethodConstraint(HttpMethod.Get) });
            //config.Routes.MapHttpRoute("DefaultApiWithId", "Api/{controller}/{id}", new { id = RouteParameter.Optional }, new { id = @"\d+" });
        }
    }
}
