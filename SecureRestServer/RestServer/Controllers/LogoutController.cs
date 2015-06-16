using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace RestServer.Controllers
{
    public class LogoutController : ApiController
    {
        public HttpResponseMessage Get(string token)
        {
            using (var db = new Models.DatabaseEntities())
            {
                // Models.ActiveSession session = new Models.ActiveSession();
                //session.token = token;
                Models.ActiveSession session = db.ActiveSessions.FirstOrDefault(t => t.token == token);
                //db.ActiveSessions.Attach(session);              
                try
                {
                    db.ActiveSessions.Remove(session);
                    db.SaveChanges();

                }
                catch (System.Data.Entity.Infrastructure.DbUpdateConcurrencyException ex)
                {

                    JObject response = new JObject
                    {
                        {"status", "Invalid Token"}
                    };

                    // Something like log4net could be implemented to write out the actual errors and info to a file
                    return Request.CreateResponse(HttpStatusCode.NotFound, response, System.Net.Http.Formatting.JsonMediaTypeFormatter.DefaultMediaType);
                }
                catch (Exception ex)
                {
                    JObject response = new JObject
                    {
                        {"status", "Invalid Token"}
                    };

                    // Something like log4net could be implemented to write out the actual errors and info to a file
                    return Request.CreateResponse(HttpStatusCode.InternalServerError, response, System.Net.Http.Formatting.JsonMediaTypeFormatter.DefaultMediaType);
                }

                JObject response2 = new JObject
                    {
                        {"status", "loggedout"}
                    };

                return Request.CreateResponse(HttpStatusCode.OK, response2, System.Net.Http.Formatting.JsonMediaTypeFormatter.DefaultMediaType);
            }
        }
    }
}
