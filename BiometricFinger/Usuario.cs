using SourceAFIS.Simple;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BiometricFinger
{
    [Serializable]
    [Table("user")]
    public class Usuario
    {
        [Key]
        [Column("user_id")]
        public int id { get; set; }
        public string username { get; set; }
        public byte[] finger { get; set; }
    }
}
