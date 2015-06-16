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
    public class AccountController : ApiController
    {

        /// <summary>
        /// Basic Get request.  We are not supporting it for this object
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        public HttpResponseMessage Get()
        {
            return Request.CreateResponse(HttpStatusCode.Unauthorized, "User must be logged in and use a valid token.", System.Net.Http.Formatting.JsonMediaTypeFormatter.DefaultMediaType);
        }


        /// <summary>
        /// Will return json array of all users.  Only Admin have access to this
        /// </summary>
        /// <param name="token"></param>
        /// <returns></returns>
        /// 
        [HttpGet]
        public HttpResponseMessage Get(string token)
        {

            if (validateUser.validUser(token))
            {
                using (var db = new Models.DatabaseEntities())
                {
                    if (validateUser.isAdmin(token))
                    {
                        var allUsers = (from c in db.users
                                        join us in db.logins
                                        on c.userNumber equals us.userNumber
                                        select new { c.userNumber, c.userFirstName, c.userLastName, c.name, c.phone, c.addressLine1, c.city, c.state, c.postalCode, c.country, c.email, userName = us.userName }).ToList();

                        return Request.CreateResponse(HttpStatusCode.OK, allUsers.AsEnumerable(), System.Net.Http.Formatting.JsonMediaTypeFormatter.DefaultMediaType);

                    }
                    else
                    {
                        return Request.CreateResponse(HttpStatusCode.Unauthorized, "Not Authorized", System.Net.Http.Formatting.JsonMediaTypeFormatter.DefaultMediaType);
                    }
                }
            }
            else
            {
                return Request.CreateResponse(HttpStatusCode.Unauthorized, "Not Authorized or token has expired.", System.Net.Http.Formatting.JsonMediaTypeFormatter.DefaultMediaType);
            }

        }


        /// <summary>
        /// Get request to ask for information about a specific user (user)
        /// </summary>
        /// <param name="token"></param>
        /// <param name="user"></param>
        /// <returns></returns>
        /// 
        [HttpGet]
        public HttpResponseMessage Get(string token, string userName)
        {
            if (validateUser.validUser(token))
            {
                using (var db = new Models.DatabaseEntities())
                {
                    var userInfo = (from c in db.users
                                    where (from u in db.logins where userName == u.userName select u.userNumber).Contains(c.userNumber)
                                    join us in db.logins
                                    on c.userNumber equals us.userNumber
                                    select new { c.userNumber, c.userFirstName, c.userLastName, c.name, c.phone, c.addressLine1, c.city, c.state, c.postalCode, c.country, c.email, userName = us.userName }).ToList();


                    if (userInfo.Count() > 0)
                    {
                        return Request.CreateResponse(HttpStatusCode.OK, userInfo, System.Net.Http.Formatting.JsonMediaTypeFormatter.DefaultMediaType);
                    }
                    else
                    {
                        return Request.CreateErrorResponse(HttpStatusCode.BadRequest, "No User Info found");
                    }
                }
            }
            else
            {
                return Request.CreateResponse(HttpStatusCode.Unauthorized, "Not Authorized or token has expired.", System.Net.Http.Formatting.JsonMediaTypeFormatter.DefaultMediaType);
            }
        }


        /// <summary>
        /// POST.  User passes individual parameters.  Only Admin can do this.
        /// </summary>
        /// <param name="token"></param>
        /// <param name="userFirstName"></param>
        /// <param name="userLastName"></param>
        /// <param name="phone"></param>
        /// <param name="city"></param>
        /// <param name="postalCode"></param>
        /// <param name="country"></param>
        /// <param name="email"></param>
        /// <param name="username"></param>
        /// <param name="pass"></param>
        /// <returns></returns>
        [HttpPost]
        public HttpResponseMessage Post(string token, string userFirstName, string userLastName, decimal phone, string addressLine1, string city, string state, string postalCode, string country, string email, string username, string pass)
        {
            if (validateUser.validUser(token))
            {
                if (validateUser.isAdmin(token))
                {
                    using (var db = new Models.DatabaseEntities())
                    {

                        Models.user newUser = new Models.user();
                        newUser.userFirstName = userFirstName;
                        newUser.userLastName = userLastName;
                        newUser.name = userFirstName + userLastName;
                        newUser.phone = phone;
                        newUser.addressLine1 = addressLine1;
                        newUser.city = city;
                        newUser.state = state;
                        newUser.postalCode = postalCode;
                        newUser.country = country;
                        newUser.email = email;

                        try
                        {
                            db.SaveChanges();
                        }
                        catch (System.Data.Entity.Validation.DbEntityValidationException ex)
                        {
                            return Request.CreateResponse(HttpStatusCode.BadRequest, ex.EntityValidationErrors.ToList(), System.Net.Http.Formatting.JsonMediaTypeFormatter.DefaultMediaType);
                        }
                        catch (Exception ex)
                        {
                            return Request.CreateResponse(HttpStatusCode.BadRequest, ex.InnerException.Message, System.Net.Http.Formatting.JsonMediaTypeFormatter.DefaultMediaType);
                        }

                        Models.login newLogin = new Models.login();
                        newLogin.userName = username;

                        List<string> passSalt = SignupController.ComputeHash(pass);
                        newLogin.pass = passSalt[0];
                        newLogin.salt = passSalt[1];

                        newLogin.userNumber = newUser.userNumber;
                        db.logins.Add(newLogin);

                        try
                        {
                            db.SaveChanges();
                        }
                        catch (Exception ex)
                        {
                            return Request.CreateResponse(HttpStatusCode.BadRequest, ex.InnerException.Message, System.Net.Http.Formatting.JsonMediaTypeFormatter.DefaultMediaType);
                        }

                        var msg = Request.CreateResponse(HttpStatusCode.Created, "User " + newUser.name + " with id " + newUser.userNumber + " was created.", System.Net.Http.Formatting.JsonMediaTypeFormatter.DefaultMediaType);
                        msg.Headers.Location = new Uri(Request.RequestUri + "&userId=" + newUser.userNumber.ToString());
                        return msg;
                    }
                }
                else
                {
                    return Request.CreateResponse(HttpStatusCode.Unauthorized, "Not Authorized.", System.Net.Http.Formatting.JsonMediaTypeFormatter.DefaultMediaType);
                }
            }
            else
            {
                return Request.CreateResponse(HttpStatusCode.Unauthorized, "Not Authorized or token has expired.", System.Net.Http.Formatting.JsonMediaTypeFormatter.DefaultMediaType);
            }
        }


        /// <summary>
        /// POST.  This will create a new user.  Does not support multiple Accounts at once.  Only Admin can do this.  
        /// </summary>
        /// <param name="token"></param>
        /// <param name="json"></param>
        /// <returns></returns>
        [HttpPost]
        public HttpResponseMessage Post(string token, [FromBody]JObject json)
        {
            if (validateUser.validUser(token))
            {
                if (validateUser.isAdmin(token))
                {
                    using (var db = new Models.DatabaseEntities())
                    {
                        Models.user newUser = JsonConvert.DeserializeObject<Models.user>(json.ToString());

                        newUser.name = newUser.userFirstName + " " + newUser.userLastName;
                        db.users.Add(newUser);

                        try
                        {
                            db.SaveChanges();
                        }
                        catch (System.Data.Entity.Validation.DbEntityValidationException ex)
                        {
                            return Request.CreateResponse(HttpStatusCode.BadRequest, ex.EntityValidationErrors.ToList(), System.Net.Http.Formatting.JsonMediaTypeFormatter.DefaultMediaType);
                        }
                        catch (Exception ex)
                        {
                            return Request.CreateResponse(HttpStatusCode.BadRequest, ex.InnerException.Message, System.Net.Http.Formatting.JsonMediaTypeFormatter.DefaultMediaType);
                        }

                        Models.login newLogin = new Models.login();
                        newLogin.userName = json["username"].ToString();

                        List<string> passSalt = SignupController.ComputeHash(json["pass"].ToString());
                        newLogin.pass = passSalt[0];
                        newLogin.salt = passSalt[1];

                        newLogin.userNumber = newUser.userNumber;
                        db.logins.Add(newLogin);

                        try
                        {
                            db.SaveChanges();
                        }
                        catch (Exception ex)
                        {
                            return Request.CreateResponse(HttpStatusCode.BadRequest, ex.InnerException.Message, System.Net.Http.Formatting.JsonMediaTypeFormatter.DefaultMediaType);
                        }

                        var msg = Request.CreateResponse(HttpStatusCode.Created, "User " + newUser.name + " with id " + newUser.userNumber + " was created.", System.Net.Http.Formatting.JsonMediaTypeFormatter.DefaultMediaType);
                        msg.Headers.Location = new Uri(Request.RequestUri + "&userId=" + newUser.userNumber.ToString());
                        return msg;
                    }
                }
                else
                {
                    return Request.CreateResponse(HttpStatusCode.Unauthorized, "Not Authorized.", System.Net.Http.Formatting.JsonMediaTypeFormatter.DefaultMediaType);
                }
            }
            else
            {
                return Request.CreateResponse(HttpStatusCode.Unauthorized, "Not Authorized or token has expired.", System.Net.Http.Formatting.JsonMediaTypeFormatter.DefaultMediaType);
            }
        }


        /// <summary>
        /// Put.  This is to update a user
        /// </summary>
        /// <param name="token"></param>
        /// <param name="userNumber"></param>
        /// <returns></returns>
        [HttpPut]
        public HttpResponseMessage Put(string token, [FromBody]JObject json)
        {
            if (validateUser.validUser(token))
            {
                using (var db = new Models.DatabaseEntities())
                {
                    Models.user user = JsonConvert.DeserializeObject<Models.user>(json.ToString());
                    Models.user existingUser = db.users.FirstOrDefault(u => u.userNumber == user.userNumber);  // See if the usernumber exists.  If it doesn't it is to add a new user.  If it does, it is to update a user
                    Models.ActiveSession session = db.ActiveSessions.FirstOrDefault(s => s.token == token);
                    bool isAdmin = validateUser.isAdmin(token);

                    if (isAdmin)
                    {
                        Models.login login = db.logins.FirstOrDefault(l => l.userNumber == user.userNumber);  // Getting this info so that we can update the username/password if we need to

                        // could use a comparer here
                        existingUser.userFirstName = user.userFirstName == null ? existingUser.userFirstName : user.userFirstName;
                        existingUser.userLastName = user.userLastName == null ? existingUser.userLastName : user.userLastName;
                        existingUser.phone = user.phone == null ? existingUser.phone : user.phone;
                        existingUser.addressLine1 = user.addressLine1 == null ? existingUser.addressLine1 : user.addressLine1;
                        existingUser.city = user.city == null ? existingUser.city : user.city;
                        existingUser.state = user.state == null ? existingUser.state : user.state;
                        existingUser.postalCode = user.postalCode == null ? existingUser.postalCode : user.postalCode;
                        existingUser.country = user.country == null ? existingUser.country : user.country;
                        existingUser.email = user.email == null ? existingUser.email : user.email;

                        // If the admin is changing their own info we will leave them as admin
                        if (session.login1.userNumber == user.userNumber)
                        {
                            existingUser.isAdmin = true;
                        }
                        else
                        {
                            existingUser.isAdmin = user.isAdmin;
                        }

                        login.userName = json["username"].ToString() == "" ? login.userName : json["username"].ToString();

                        if (json["pass"].ToString() != "")
                        {
                            List<string> passSalt = SignupController.ComputeHash(json["pass"].ToString());
                            login.pass = passSalt[0];
                            login.salt = passSalt[1];
                        }
                        //login.pass = json["pass"].ToString() == "" ? login.pass : json["pass"].ToString();


                        // TODO See what the other exceptions would be
                        try
                        {
                            db.SaveChanges();
                        }
                        catch (Exception ex)
                        {
                            return Request.CreateResponse(HttpStatusCode.Conflict, "User Information was not updated." + " Error Message:" + ex.InnerException.Message, System.Net.Http.Formatting.JsonMediaTypeFormatter.DefaultMediaType);
                        }

                        return Request.CreateResponse(HttpStatusCode.OK, "User Information was updated.", System.Net.Http.Formatting.JsonMediaTypeFormatter.DefaultMediaType);
                    }
                    else // Not an admin and they are wanting to update info
                    {
                        Models.login login = db.logins.Single(l => l.userNumber == user.userNumber);

                        if (session.login1.userNumber == user.userNumber)  // Check to make sure that they user is trying to update their own records
                        {
                            existingUser.userFirstName = user.userFirstName == null ? existingUser.userFirstName : user.userFirstName;
                            existingUser.userLastName = user.userLastName == null ? existingUser.userLastName : user.userLastName;
                            existingUser.phone = user.phone == null ? existingUser.phone : user.phone;
                            existingUser.addressLine1 = user.addressLine1 == null ? existingUser.addressLine1 : user.addressLine1;
                            existingUser.city = user.city == null ? existingUser.city : user.city;
                            existingUser.state = user.state == null ? existingUser.state : user.state;
                            existingUser.postalCode = user.postalCode == null ? existingUser.postalCode : user.postalCode;
                            existingUser.country = user.country == null ? existingUser.country : user.country;
                            existingUser.email = user.email == null ? existingUser.email : user.email;

                            login.userName = json["username"].ToString();  // There are other things I will need to do to update the username
                            login.pass = json["pass"].ToString();

                            try
                            {
                                db.SaveChanges();
                            }
                            catch (Exception ex)
                            {
                                return Request.CreateResponse(HttpStatusCode.Conflict, "User Information was not updated." + " Error Message:" + ex.InnerException.Message, System.Net.Http.Formatting.JsonMediaTypeFormatter.DefaultMediaType);
                            }

                            return Request.CreateResponse(HttpStatusCode.OK, "User Information was updated.", System.Net.Http.Formatting.JsonMediaTypeFormatter.DefaultMediaType);
                        }
                        else
                        {
                            // User is not admin and is trying to update user info that is not their own
                            return Request.CreateResponse(HttpStatusCode.Unauthorized, "Not Authorized.", System.Net.Http.Formatting.JsonMediaTypeFormatter.DefaultMediaType);
                        }
                    }
                }
            }
            else
            {
                return Request.CreateResponse(HttpStatusCode.Unauthorized, "Not Authorized or token has expired.", System.Net.Http.Formatting.JsonMediaTypeFormatter.DefaultMediaType);
            }
        }



        [HttpPut]
        public HttpResponseMessage Put(string token, bool batch, [FromBody]JArray acctJson)
        {
            if (validateUser.validUser(token) && batch)
            {
                using (var db = new Models.DatabaseEntities())
                {
                    //List<Models.user> users = JsonConvert.DeserializeObject<List<Models.user>>(acctJson.ToString());

                    bool isAdmin = validateUser.isAdmin(token);

                    if (isAdmin)
                    {

                        foreach (var user in acctJson)  // Doing it this way instead of using the users above becuase the username and password that get passed with it aren't part of the user class
                        {
                            decimal userNumber = Convert.ToDecimal(user["userNumber"].ToString());
                            Models.user existingUser = db.users.FirstOrDefault(u => u.userNumber == userNumber);  // See if the usernumber exists.  If it doesn't it is to add a new user.  If it does, it is to update a user
                            Models.ActiveSession session = db.ActiveSessions.FirstOrDefault(s => s.token == token);
                            Models.login login = db.logins.FirstOrDefault(l => l.userNumber == userNumber);  // Getting this info so that we can update the username/password if we need to

                            // could use a comparer here
                            existingUser.userFirstName = user["userFirstName"].ToString() == null ? existingUser.userFirstName : user["userFirstName"].ToString();
                            existingUser.userLastName = user["userLastName"].ToString() == null ? existingUser.userLastName : user["userLastName"].ToString();
                            existingUser.phone = Convert.ToDecimal(user["phone"].ToString()) == null ? existingUser.phone : Convert.ToDecimal(user["phone"].ToString());
                            existingUser.addressLine1 = user["addressLine1"].ToString() == null ? existingUser.addressLine1 : user["addressLine1"].ToString();
                            existingUser.city = user["city"].ToString() == null ? existingUser.city : user["city"].ToString();
                            existingUser.state = user["state"].ToString() == null ? existingUser.state : user["state"].ToString();
                            existingUser.postalCode = user["postalCode"].ToString() == null ? existingUser.postalCode : user["postalCode"].ToString();
                            existingUser.country = user["country"].ToString() == null ? existingUser.country : user["country"].ToString();
                            existingUser.email = user["email"].ToString() == null ? existingUser.email : user["email"].ToString();

                            //// If the admin is changing their own info we will leave them as admin
                            //if (session.login1.userNumber == userNumber)
                            //{
                            //    existingUser.isAdmin = true;
                            //}
                            //else
                            //{
                            //    existingUser.isAdmin = Convert.ToBoolean(user["isAdmin"].ToString());
                            //}

                            login.userName = user["username"].ToString() == "" ? login.userName : user["username"].ToString();  // There are other things I will need to do to update the username

                            if (user["pass"].ToString() != "")
                            {
                                List<string> passSalt = SignupController.ComputeHash(user["pass"].ToString());
                                login.pass = passSalt[0];
                                login.salt = passSalt[1];
                            }
                            //login.pass = user["pass"].ToString() == "" ? login.pass : user["pass"].ToString();
                        }

                        // TODO See what the other exceptions would be
                        try
                        {
                            db.SaveChanges();
                        }
                        catch (Exception ex)
                        {
                            return Request.CreateResponse(HttpStatusCode.Conflict, "User Information was not updated." + " Error Message:" + ex.InnerException.Message, System.Net.Http.Formatting.JsonMediaTypeFormatter.DefaultMediaType);
                        }

                        return Request.CreateResponse(HttpStatusCode.OK, "User Information was updated.", System.Net.Http.Formatting.JsonMediaTypeFormatter.DefaultMediaType);

                    }
                    else // Not an admin and they are wanting to update info
                    {
                        // User is not admin and is trying to update user info that is not their own
                        return Request.CreateResponse(HttpStatusCode.Unauthorized, "Not Authorized.", System.Net.Http.Formatting.JsonMediaTypeFormatter.DefaultMediaType);
                    }
                }
            }
            else
            {
                return Request.CreateResponse(HttpStatusCode.Unauthorized, "Not Authorized or token has expired.", System.Net.Http.Formatting.JsonMediaTypeFormatter.DefaultMediaType);
            }
        }



        /// <summary>
        /// Delete.  This will delete a user.  The username is unique so it can be used to find and delete an existing user.  Only Admin can do this.
        /// </summary>
        /// <param name="token"></param>
        /// <param name="userName"></param>
        /// <returns></returns>
        [HttpDelete]
        public HttpResponseMessage Delete(string token, string userName)
        {
            using (var db = new Models.DatabaseEntities())
            {
                if (validateUser.validUser(token))
                {
                    if (validateUser.isAdmin(token))
                    {
                        try
                        {
                            var custNum = (from l in db.logins
                                           where userName == l.userName
                                           select l.userNumber).FirstOrDefault();

                            Models.login login = db.logins.First(l => l.userNumber == custNum);
                            Models.ActiveSession session = db.ActiveSessions.FirstOrDefault(s => s.login == login.Id);
                            // Could put in a stored procedure to delete the ActiveSession, login when user is being deleted but this shows more clearly what is being done
                            if (session != null)
                            {
                                //var sessionToDelete = new Models.ActiveSession { Id = session.Id };
                                db.ActiveSessions.Attach(session);
                                db.ActiveSessions.Remove(session);
                                //db.SaveChanges();
                            }

                            //var loginToDelete = new Models.login { Id = login.Id };
                            db.logins.Attach(login);
                            db.logins.Remove(login);
                            //db.SaveChanges();

                            var user = new Models.user { userNumber = custNum };
                            db.users.Attach(user);
                            db.users.Remove(user);
                            db.SaveChanges();

                            return Request.CreateResponse(HttpStatusCode.OK, "User " + user + " was deleted.", System.Net.Http.Formatting.JsonMediaTypeFormatter.DefaultMediaType);
                        }
                        catch (Exception ex)
                        {
                            return Request.CreateResponse(HttpStatusCode.InternalServerError, "An error has occurred (user not found or other error) and the request was not completed." + " Error Message: " + ex.InnerException.Message, System.Net.Http.Formatting.JsonMediaTypeFormatter.DefaultMediaType);
                        }
                    }
                    else
                    {
                        return Request.CreateResponse(HttpStatusCode.Unauthorized, "Not Authorized.", System.Net.Http.Formatting.JsonMediaTypeFormatter.DefaultMediaType);
                    }
                }
                else
                {
                    return Request.CreateResponse(HttpStatusCode.Unauthorized, "Not Authorized or token has expired.", System.Net.Http.Formatting.JsonMediaTypeFormatter.DefaultMediaType);
                }
            }
        }
    }
}
