using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BiometricFinger
{
    [Serializable]
    [Table("estadocomparacion")]
    public class EstadoComparacion
    {
        [Key]
        [Column("id")]
        public int id { get; set; }
        public int id_personal { get; set; }
        public  float puntuacion { get; set; }
    }
}
