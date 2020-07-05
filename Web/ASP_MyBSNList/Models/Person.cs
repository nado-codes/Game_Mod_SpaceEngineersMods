using ASP_MyBSNList.Models.Person_Components;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace ASP_MyBSNList.Models
{
    public class Person
    {
        [Required]
        public Status Status { get; set; }

        [Required]
        public int StatusId { get; set; }

        [Required]
        public Reason Reason { get; set; }

        [Required]
        public int ReasonId { get; set; }
    }
}