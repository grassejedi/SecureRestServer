using System;
using System.Web;
using System.Web.Mvc;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Security.Cryptography;
using System.Text;

namespace RestServer.Controllers
{
    public class LoginController : ApiController
    {
        /// <summary>
        /// GET
        /// </summary>
        /// <returns></returns>
        public HttpResponseMessage Get()
        {
            return Request.CreateResponse(HttpStatusCode.Unauthorized, "Not Authorized.  User must be logged in", System.Net.Http.Formatting.JsonMediaTypeFormatter.DefaultMediaType);
        }


        /// <summary>
        /// Get.  This will allow the user to login.  A token will be returned that the user will need to use with other requests.  Token is only good for
        /// </summary>
        /// <param name="user"></param>
        /// <param name="pass"></param>
        /// <returns></returns>
        public HttpResponseMessage Get(string user, string pass)
        {
            using (
                var db = new Models.DatabaseEntities())
            {


                Models.login existingLogin = db.logins.FirstOrDefault(l => l.userName == user);

                if (existingLogin != null)
                {
                    if (VerifyHash(pass, existingLogin.pass, existingLogin.salt))
                    {

                        var authUser = (from u in db.logins
                                        where u.userName == user
                                        join c in db.users
                                        on u.userNumber equals c.userNumber
                                        select new { u.user, u.userNumber, u.Id, admin = c.isAdmin }).ToList();

                        string token = createToken.uniqueCode();
                        JObject o = new JObject
                        {
                            {"status", "loggedin"},
                            {"token", token }
                                                
                        };

                        if (authUser[0].admin)
                        {
                            o.Add("admin", "true");
                        }

                        try
                        {
                            var session = new Models.ActiveSession();
                            session.token = token.Trim();
                            session.login = authUser[0].Id;
                            session.InvalidDateTime = DateTime.Now;
                            //int id = db.ActiveSessions.Any() ? db.ActiveSessions.Select(a => a.Id).Max() + 1 : 1;
                            // session.Id = id;
                            db.ActiveSessions.Add(session);
                            db.SaveChanges();
                        }
                        catch (System.Data.Entity.Infrastructure.DbUpdateException ex)
                        {
                            // We should could set something up so that it returns the user information like above in this case
                            return Request.CreateResponse(HttpStatusCode.Conflict, "User currently logged in", System.Net.Http.Formatting.JsonMediaTypeFormatter.DefaultMediaType);
                        }
                        catch (System.Exception dbEx)
                        {
                            return Request.CreateResponse(HttpStatusCode.InternalServerError, "Error occurred.  Please attempt to login again.", System.Net.Http.Formatting.JsonMediaTypeFormatter.DefaultMediaType);
                        }

                        return Request.CreateResponse(HttpStatusCode.OK, o, System.Net.Http.Formatting.JsonMediaTypeFormatter.DefaultMediaType);
                    }
                    else
                    {
                        return Request.CreateErrorResponse(HttpStatusCode.Unauthorized, "User not authenicated.");
                    }
                }
                else
                {
                    return Request.CreateErrorResponse(HttpStatusCode.Unauthorized, "User not authenicated.");
                }

            }
        }

        // POST api/login
        public HttpResponseMessage Post([FromBody]string value)
        {
            return Request.CreateResponse(HttpStatusCode.NotImplemented, "Not Supported", System.Net.Http.Formatting.JsonMediaTypeFormatter.DefaultMediaType);
        }






        /// <summary>
        /// VerifyHash Method
        /// Purpose: Compare incoming password with stored hash.  Got code from http://www.obviex.com/samples/hash.aspx.  I made a few changes to go along with what I was doing
        /// </summary>
        /// <param name="plainText"></param>
        /// <param name="hashValue"></param>
        /// <returns></returns>
        public static bool VerifyHash(string plainText, string hashValue, string salt)
        {
            // Convert base64-encoded hash value into a byte array.
            byte[] hashWithSaltBytes = Encoding.ASCII.GetBytes(hashValue);

            // We must know size of hash (without salt).
            int hashSizeInBits, hashSizeInBytes;

            // Size of hash is based on the specified algorithm.
            hashSizeInBits = 512;

            // Convert size of hash from bits to bytes.
            hashSizeInBytes = hashSizeInBits / 8;

            // Make sure that the specified hash value is long enough.
            if (hashWithSaltBytes.Length < hashSizeInBytes)
                return false;

            //// Allocate array to hold original salt bytes retrieved from hash.
            //byte[] saltBytes = new byte[hashWithSaltBytes.Length - hashSizeInBytes];

            //// Copy salt from the end of the hash to the new array.
            //for (int i = 0; i < saltBytes.Length; i++)
            //{
            //    saltBytes[i] = hashWithSaltBytes[hashSizeInBytes + i];
            //}

            byte[] saltBytes = Convert.FromBase64String(salt);

            // Compute a new hash string.
            string expectedHashString = ComputeHash(plainText, saltBytes);

            // If the computed hash matches the specified hash,
            // the plain text value must be correct.
            return (hashValue == expectedHashString);
        }



        /// <summary>
        /// http://www.obviex.com/samples/hash.aspx
        /// </summary>
        /// <param name="plainText"></param>
        /// <param name="saltBytes"></param>
        /// <returns></returns>
        public static string ComputeHash(string plainText, byte[] saltBytes)
        {
            // If salt is not specified, generate it on the fly.
            if (saltBytes == null)
            {
                // Define min and max salt sizes.
                int minSaltSize = 4;
                int maxSaltSize = 8;

                // Generate a random number for the size of the salt.
                Random random = new Random();
                int saltSize = random.Next(minSaltSize, maxSaltSize);

                // Allocate a byte array, which will hold the salt.
                saltBytes = new byte[saltSize];

                // Initialize a random number generator.
                RNGCryptoServiceProvider rng = new RNGCryptoServiceProvider();

                // Fill the salt with cryptographically strong byte values.
                rng.GetNonZeroBytes(saltBytes);
            }

            // Convert plain text into a byte array.
            byte[] plainTextBytes = Encoding.UTF8.GetBytes(plainText);

            // Allocate array, which will hold plain text and salt.
            byte[] plainTextWithSaltBytes =
                    new byte[plainTextBytes.Length + saltBytes.Length];

            // Copy plain text bytes into resulting array.
            for (int i = 0; i < plainTextBytes.Length; i++)
                plainTextWithSaltBytes[i] = plainTextBytes[i];

            // Append salt bytes to the resulting array.
            for (int i = 0; i < saltBytes.Length; i++)
                plainTextWithSaltBytes[plainTextBytes.Length + i] = saltBytes[i];

            HashAlgorithm hash = new SHA512Managed();

            // Compute hash value of our plain text with appended salt.
            byte[] hashBytes = hash.ComputeHash(plainTextWithSaltBytes);

            // Create array which will hold hash and original salt bytes.
            byte[] hashWithSaltBytes = new byte[hashBytes.Length +
                                                saltBytes.Length];

            // Copy hash bytes into resulting array.
            for (int i = 0; i < hashBytes.Length; i++)
                hashWithSaltBytes[i] = hashBytes[i];

            // Append salt bytes to the result.
            for (int i = 0; i < saltBytes.Length; i++)
                hashWithSaltBytes[hashBytes.Length + i] = saltBytes[i];

            // Convert result into a base64-encoded string.
            string hashValue = Convert.ToBase64String(hashWithSaltBytes);

            // Return the result.
            return hashValue;
        }

    }
}

