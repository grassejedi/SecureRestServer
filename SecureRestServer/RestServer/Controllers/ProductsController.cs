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
    public class ProductsController : ApiController
    {
        /// <summary>
        /// GET all products
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        public HttpResponseMessage Get()
        {
            using (var db = new Models.DatabaseEntities())
            {
                var prods = (from p in db.products
                             select new { p.productName, p.productId, p.buyPrice, p.description, p.quantityInStock, p.productVendor }).ToList();

                return Request.CreateResponse(HttpStatusCode.OK, prods.AsEnumerable(), System.Net.Http.Formatting.JsonMediaTypeFormatter.DefaultMediaType);
            }
        }


        /// <summary>
        /// GET by product name
        /// </summary>
        /// <param name="productName"></param>
        /// <returns></returns>
        [HttpGet]
        public HttpResponseMessage Get(string productName)
        {

            using (var db = new Models.DatabaseEntities())
            {
                var prod = (from p in db.products
                            where p.productName == productName
                            select new { p.productName, p.buyPrice, p.description, p.quantityInStock, p.productVendor }).ToList();

                if (prod.Count() > 0)
                {
                    return Request.CreateResponse(HttpStatusCode.OK, prod, System.Net.Http.Formatting.JsonMediaTypeFormatter.DefaultMediaType);
                }
                else
                {
                    return Request.CreateResponse(HttpStatusCode.NotFound, "Product not found", System.Net.Http.Formatting.JsonMediaTypeFormatter.DefaultMediaType);
                }
            }
        }


        /// <summary>
        /// Get by productId
        /// </summary>
        /// <param name="productId"></param>
        /// <returns></returns>
        [HttpGet]
        public HttpResponseMessage Get(int productId)
        {

            using (var db = new Models.DatabaseEntities())
            {
                var prod = (from p in db.products
                            where p.productId == productId
                            select new { p.productName, p.buyPrice, p.description, p.quantityInStock, p.productVendor }).ToList();

                if (prod.Count() > 0)
                {
                    return Request.CreateResponse(HttpStatusCode.OK, prod, System.Net.Http.Formatting.JsonMediaTypeFormatter.DefaultMediaType);
                }
                else
                {
                    return Request.CreateResponse(HttpStatusCode.NotFound, "Product not found", System.Net.Http.Formatting.JsonMediaTypeFormatter.DefaultMediaType);
                }
            }
        }


        /// <summary>
        /// POST a new product by passing separate parameters.  Only admins can do this.
        /// </summary>
        /// <param name="token"></param>
        /// <param name="pcode"></param>
        /// <param name="pname"></param>
        /// <param name="pprice"></param>
        /// <param name="pquantity"></param>
        /// <param name="pvendor"></param>
        /// <param name="pdesc"></param>
        /// <param name="MSRP"></param>
        /// <returns></returns>
        [HttpPost]
        public HttpResponseMessage Post(string token, string pname, decimal pprice, short pquantity, string pvendor = "", string pdesc = "", decimal MSRP = 0)
        {
            if (validateUser.validUser(token))
            {
                if (validateUser.isAdmin(token))
                {
                    using (var db = new Models.DatabaseEntities())
                    {
                        var prod = new Models.product();
                        prod.productName = pname;
                        prod.buyPrice = pprice;
                        prod.description = pdesc;
                        prod.quantityInStock = pquantity;
                        prod.productVendor = pvendor;
                        prod.MSRP = MSRP;
                        db.products.Add(prod);

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

                        var msg = Request.CreateResponse(HttpStatusCode.Created, "Product " + prod.productName + " was created.", System.Net.Http.Formatting.JsonMediaTypeFormatter.DefaultMediaType);
                        msg.Headers.Location = new Uri(Request.RequestUri + "&productId=" + prod.productId.ToString());
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
        /// POST.  Create new product by passing Json object.  Only Admins can do this
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
                        Models.product prod = JsonConvert.DeserializeObject<Models.product>(json.ToString());

                        try
                        {
                            db.products.Add(prod);
                            db.SaveChanges();
                        }
                        catch (System.Data.Entity.Validation.DbEntityValidationException ex)
                        {
                            // A required field was most likely missing from the json object
                            return Request.CreateResponse(HttpStatusCode.BadRequest, ex.EntityValidationErrors.ToList(), System.Net.Http.Formatting.JsonMediaTypeFormatter.DefaultMediaType);
                        }
                        catch (Exception ex)
                        {
                            // Something like log4net could be implemented to write out the actual errors and info to a file
                            return Request.CreateResponse(HttpStatusCode.BadRequest, "An error occurred and product was not created. Error message: " + ex.InnerException.Message, System.Net.Http.Formatting.JsonMediaTypeFormatter.DefaultMediaType);
                        }


                        var msg = Request.CreateResponse(HttpStatusCode.Created, "Product " + prod.productName + " was created.", System.Net.Http.Formatting.JsonMediaTypeFormatter.DefaultMediaType);
                        msg.Headers.Location = new Uri(Request.RequestUri + "&productId=" + prod.productId.ToString());
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
        /// PUT.  Updates a product.  Only Admins can do this
        /// </summary>
        /// <param name="token"></param>
        /// <param name="json"></param>
        /// <returns></returns>
        [HttpPut]
        public HttpResponseMessage Put(string token, [FromBody]JArray json)
        {
            if (validateUser.validUser(token))
            {
                if (validateUser.isAdmin(token))
                {
                    using (var db = new Models.DatabaseEntities())
                    {
                        List<Models.product> prods = JsonConvert.DeserializeObject<List<Models.product>>(json.ToString());

                        foreach (Models.product prod in prods)
                        {
                            Models.product existingProduct = db.products.FirstOrDefault(product => product.productId == prod.productId);
                            if (existingProduct != null)
                            {
                                existingProduct.productName = prod.productName == null ? existingProduct.productName : prod.productName;
                                existingProduct.productVendor = prod.productVendor == null ? existingProduct.productVendor : prod.productVendor;
                                existingProduct.quantityInStock = prod.quantityInStock == null ? existingProduct.quantityInStock : prod.quantityInStock;
                                existingProduct.description = prod.description == null ? existingProduct.description : prod.description;
                                existingProduct.buyPrice = prod.buyPrice == null ? existingProduct.buyPrice : prod.buyPrice;
                            }
                            else
                            {
                                return Request.CreateResponse(HttpStatusCode.NotFound, "No product with productId " + prod.productId + " was found.  No order was updated.", System.Net.Http.Formatting.JsonMediaTypeFormatter.DefaultMediaType);
                            }
                        }
                        try
                        {
                            //db.products.Attach(prod);
                            db.SaveChanges();
                        }
                        catch (System.Data.Entity.Infrastructure.DbUpdateConcurrencyException ex)
                        {
                            // Something like log4net could be implemented to write out the actual errors and info to a file
                            return Request.CreateResponse(HttpStatusCode.BadRequest, "An error occurred and product was not updated.", System.Net.Http.Formatting.JsonMediaTypeFormatter.DefaultMediaType);
                        }
                        catch (System.Data.Entity.Validation.DbEntityValidationException ex)
                        {
                            // A required field was most likely missing from the json object
                            return Request.CreateResponse(HttpStatusCode.BadRequest, ex.EntityValidationErrors.ToList(), System.Net.Http.Formatting.JsonMediaTypeFormatter.DefaultMediaType);
                        }
                        catch (Exception ex)
                        {
                            // Something like log4net could be implemented to write out the actual errors and info to a file
                            return Request.CreateResponse(HttpStatusCode.BadRequest, "An error occurred and product was not updated.", System.Net.Http.Formatting.JsonMediaTypeFormatter.DefaultMediaType);
                        }

                        return Request.CreateResponse(HttpStatusCode.OK, "Products were updated.", System.Net.Http.Formatting.JsonMediaTypeFormatter.DefaultMediaType);
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
        /// DELETE. Only Admins can do this
        /// </summary>
        /// <param name="token"></param>
        /// <param name="pcode" description="Product Code for product"></param>
        /// <returns></returns>
        [HttpDelete]
        public HttpResponseMessage Delete(string token, decimal productId)
        {
            if (validateUser.validUser(token))
            {
                if (validateUser.isAdmin(token))
                {
                    using (var db = new Models.DatabaseEntities())
                    {
                        Models.product prod = db.products.FirstOrDefault(p => p.productId == productId);

                        if (prod != null)
                        {
                            db.products.Attach(prod);
                            db.products.Remove(prod);
                            try
                            {
                                db.SaveChanges();
                            }
                            catch (System.Data.Entity.Infrastructure.DbUpdateConcurrencyException ex)
                            {
                                // Something like log4net could be implemented to write out the actual errors and info to a file
                                return Request.CreateResponse(HttpStatusCode.InternalServerError, "An error occurred and product was not deleted.  Error Message " + ex.InnerException.Message, System.Net.Http.Formatting.JsonMediaTypeFormatter.DefaultMediaType);
                            }
                            catch (Exception ex)
                            {
                                // Something like log4net could be implemented to write out the actual errors and info to a file
                                return Request.CreateResponse(HttpStatusCode.InternalServerError, "An error occurred and product was not deleted.  Error Message " + ex.InnerException.Message, System.Net.Http.Formatting.JsonMediaTypeFormatter.DefaultMediaType);
                            }

                            return Request.CreateResponse(HttpStatusCode.OK, "Product " + prod.productName + " with Product Code " + productId + " was deleted.", System.Net.Http.Formatting.JsonMediaTypeFormatter.DefaultMediaType);
                        }
                        else
                        {
                            return Request.CreateResponse(HttpStatusCode.NotFound, "No product found with Product Code " + productId + ".", System.Net.Http.Formatting.JsonMediaTypeFormatter.DefaultMediaType);
                        }
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
