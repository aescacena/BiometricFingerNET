using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BiometricFinger
{
    public partial class db_Entidades : DbContext
    {
        public db_Entidades() : base(nameOrConnectionString: "MonkeyFist") { }
        public DbSet<Persona> Personal { get; set; }
    }
}
