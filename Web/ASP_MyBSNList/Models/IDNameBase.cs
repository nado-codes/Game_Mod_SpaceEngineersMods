using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace ASP_MyBSNList.Models
{
    public class IDNameBase
    {
        byte Id { get; set; }

        [Required]
        string Name { get; set; }
    }
}