using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BiometricFinger
{
    public partial class DAO
    {
        db_Entidades db_Entidades = null;
        public DAO()
        {
            db_Entidades = new db_Entidades();
        }

        /// <summary>
        /// Realiza una consulta a la Tabla Personal con el id_persona recibido por parámetros
        /// </summary>
        /// <param name="id_persona"></param>
        public Persona getPersona(int id_persona)
        {
            Persona persona = null;
            //Obtenemos la persona de base de datos con el id_persona recibido como parámetro
            var personaBBDD = db_Entidades.Personal.Where(s => s.id_personal == id_persona);//COMPROBAR SU FUNCIONAMIENTO
            persona = personaBBDD.FirstOrDefault<Persona>();

            return persona;
        }

        /// <summary>
        /// Realiza la actualización del campo huella de la persona identificada por id_persona
        /// </summary>
        /// <param name="id_persona">identificador de la persona</param>
        /// <param name="huella">huella de la persona</param>
        /// <param name="tipoHuella">Si es 1 actualiza la huella1 si es 2 actualiza huella 2</param>
        public bool insertaHuella(int id_persona, byte[] huella, int tipoHuella)
        {
            Persona persona = getPersona(id_persona);
            Persona personaEditado = persona;

            if (tipoHuella == 1)
                personaEditado.huella1 = huella;
            else if (tipoHuella == 2)
                persona.huella2 = huella;
            else
                return false;

            db_Entidades.Entry(persona).CurrentValues.SetValues(personaEditado);
            return db_Entidades.SaveChanges() > 0;
        }

        /// <summary>
        /// Realiza una insercción en BBDD con el estado de la comparación de la huella.
        /// </summary>
        /// <param name="id_persona">identificador de la persona</param>
        /// <param name="puntuacion">valor de la puntuación</param>
        /// <param name="mac">Dirección mac del dispositivo usado para la captura de la huella</param>
        public bool insertaEstadoComparacion(int id_persona, float puntuacion)
        {
            EstadoComparacion estado = new EstadoComparacion();

            estado.id_personal = id_persona;
            estado.puntuacion = puntuacion;

            db_Entidades.DbSetEstadoComparacion.Add(estado);
            return db_Entidades.SaveChanges() > 0;
        }
    }
}
