using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MyApi.Models
{
    public class Table
    {
        public int Id { get; set; }
        public required string Name { get; set; }
    }
}