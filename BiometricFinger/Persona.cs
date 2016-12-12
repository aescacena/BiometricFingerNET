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
    [Table("personal")]
    public class Persona
    {
        [Key]
        [Column("id_personal")]
        public int id_personal { get; set; }
        public string nombre { get; set; }
        public string comentario { get; set; }
        public string isAdmin { get; set; } //Se ha puesto tipo String por que en base de datos el tipo bit permite NULOS pero en C# la no (HABRÁ QUE CAMBIAR LA BD)
        public byte[] huella1 { get; set; }
        public byte[] huella2 { get; set; }
    }
}
