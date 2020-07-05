using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace ASP_MyBSNList.Models.Components_Person
{
    public class Status
    {
        [Required]
        public string Name { get; set; }
    }
}