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
    
    public partial class user
    {
        public user()
        {
            this.orders = new HashSet<order>();
            this.logins = new HashSet<login>();
        }
    
        public decimal userNumber { get; set; }
        public string userLastName { get; set; }
        public string userFirstName { get; set; }
        public string name { get; set; }
        public decimal phone { get; set; }
        public string addressLine1 { get; set; }
        public string addressLine2 { get; set; }
        public string city { get; set; }
        public string state { get; set; }
        public string postalCode { get; set; }
        public string country { get; set; }
        public string email { get; set; }
        public bool isAdmin { get; set; }
    
        public virtual ICollection<order> orders { get; set; }
        public virtual ICollection<login> logins { get; set; }
    }
}