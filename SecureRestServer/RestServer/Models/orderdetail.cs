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
    
    public partial class orderdetail
    {
        public int Id { get; set; }
        public int orderNumber { get; set; }
        public decimal productId { get; set; }
        public int quantityOrdered { get; set; }
        public decimal total { get; set; }
    
        public virtual order order { get; set; }
        public virtual product product { get; set; }
    }
}
