using System;
using System.Web.Http;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using Swashbuckle.Swagger.Annotations;
using Scheduler;


namespace VaaApi.Controllers
{
    using System.Web.Http.Cors;
    using System.Web.Http.Results;
    using ApiCore;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;

    public class AssociatesController : ApiController
    {
        // POST Api to post the preferences of the user to the db and store it
        [SwaggerOperation("Post")]
        [EnableCors(origins: "*", headers: "*", methods: "*")]
        public string Post(CourseObject content)
        {
            try
            {
                int id = PreferenceHelper.ProcessAssociatesPreference(content);
                return id.ToString();
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }

        }
    }
}
