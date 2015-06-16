using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Data.Entity.Infrastructure;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web;
using System.Web.Http;
using RestServer.Models;
using System.Security.Cryptography;
using System.Text;

namespace RestServer.Controllers
{
    public class SignupController : ApiController
    {
        private DatabaseEntities db = new DatabaseEntities();

        // POST api/Signup
        [HttpPost]
        public HttpResponseMessage Postlogin(string userName, string pass, string userLastName, string userFirstName, decimal phone, string addressLine1, string city, string state, string postalCode, string country, string email)
        {
            if (ModelState.IsValid)
            {
                Models.user newUser = new Models.user();
                newUser.userFirstName = userFirstName;
                newUser.userLastName = userLastName;
                newUser.name = newUser.userFirstName + " " + newUser.userLastName;
                newUser.phone = phone;
                newUser.addressLine1 = addressLine1;
                newUser.city = city;
                newUser.state = state;
                newUser.postalCode = postalCode;
                newUser.country = country;
                newUser.email = email;

                try
                {
                    db.users.Add(newUser);
                    db.SaveChanges();
                }
                catch (Exception ex)
                {
                    return Request.CreateResponse(HttpStatusCode.BadRequest, ex.InnerException.Message, System.Net.Http.Formatting.JsonMediaTypeFormatter.DefaultMediaType);
                }

                List<string> hashAndSalt = ComputeHash(pass);
                string hash = hashAndSalt[0];
                string salt = hashAndSalt[1];

                Models.login newlogin = new Models.login();
                newlogin.userNumber = newUser.userNumber;
                newlogin.userName = userName;
                newlogin.pass = hash;
                newlogin.salt = salt;

                try
                {
                    db.logins.Add(newlogin);
                    db.SaveChanges();
                }
                catch (Exception ex)
                {
                    return Request.CreateResponse(HttpStatusCode.BadRequest, ex.InnerException.Message, System.Net.Http.Formatting.JsonMediaTypeFormatter.DefaultMediaType);
                }


                HttpResponseMessage response = Request.CreateResponse(HttpStatusCode.Created, "User Created", System.Net.Http.Formatting.JsonMediaTypeFormatter.DefaultMediaType);
                response.Headers.Location = new Uri(Request.RequestUri + "&userId=" + newUser.userNumber.ToString());
                return response;
            }
            else
            {
                return Request.CreateErrorResponse(HttpStatusCode.BadRequest, ModelState);
            }
        }


        protected override void Dispose(bool disposing)
        {
            db.Dispose();
            base.Dispose(disposing);
        }


        /// <summary>
        /// http://www.obviex.com/samples/hash.aspx
        /// </summary>
        /// <param name="plainText"></param>
        /// <param name="saltBytes"></param>
        /// <returns></returns>
        public static List<string> ComputeHash(string plainText, byte[] saltBytes = null)
        {
            List<string> HashAndSalt = new List<string>();

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
            string salt = Convert.ToBase64String(saltBytes);

            HashAndSalt.Add(hashValue);
            HashAndSalt.Add(salt);

            return HashAndSalt;
        }


    }
}