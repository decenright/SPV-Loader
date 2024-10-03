using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace SPV_Loader.Models
{
    [NotMapped]
    public class AsasaraViewModel
    {
        public IEnumerable<AsasaraJob> AsasaraList { get; set; }

        public IEnumerable<Order> OrdersList { get; set; }

        public ExportAsasara ExportAsasara { get; set; }

        public AsasaraJob AsasaraDetails { get; set; }
    }
}