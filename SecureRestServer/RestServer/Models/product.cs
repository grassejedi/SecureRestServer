//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated from a template.
//
//     Manual changes to this file may cause unexpected behavior in your application.
//     Manual changes to this file will be overwritten if the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace RestServer.Models
{
    using System;
    using System.Collections.Generic;
    
    public partial class product
    {
        public product()
        {
            this.orderdetails = new HashSet<orderdetail>();
        }
    
        public decimal productId { get; set; }
        public string productName { get; set; }
        public decimal buyPrice { get; set; }
        public Nullable<decimal> MSRP { get; set; }
        public string description { get; set; }
        public short quantityInStock { get; set; }
        public string productVendor { get; set; }
    
        public virtual ICollection<orderdetail> orderdetails { get; set; }
    }
}
