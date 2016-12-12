using SourceAFIS.Simple;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.IO.Pipes;
using System.Linq;
using System.Text;
using System.Threading;
using System.Net.Sockets;

namespace BiometricFinger
{
    static class Program
    {

        private static Server server;

        // Hereda de Person(Libreria SourceAFIS) para añadir campo id del Usuario de BBDD a esta instancia
        [Serializable]
        public class UsuarioAFIS : Person
        {
            public int id;
        }

        public static int numThreads = 2;
        
        static void Main()
        {
            //insertaHuellasDesdeCarpeta();

            //            Application.EnableVisualStyles();
            //          Application.SetCompatibleTextRenderingDefault(false);
            //        Application.Run(new Form1());

            server = new Server(8888);
            server.Start();

            Console.WriteLine("To exit, type 'exit'");
            while (true)
            {
                String s = Console.ReadLine();
                if ("exit".Equals(s))
                {
                    break;
                }
                //If the user types "count", print out the number of connected clients
                else if ("count".Equals(s))
                {
                    Console.WriteLine(server.NumClients);
                }
            }

            Environment.Exit(0);

        }

        static void insertaHuellasDesdeCarpeta()
        {
            // Inicializa la carpeta de imagenes
            //DirectoryInfo di = new DirectoryInfo(@"C:\Users\PC_STE_19\Documents\Visual Studio 2015\Projects\BiometricFinger\images");
            DirectoryInfo di = new DirectoryInfo(@"C:\Users\PC_STE_19\Desktop\DEDOS_ESCACENA");
            Console.WriteLine("No search pattern returns:");
            List<Usuario> usuarios = new List<Usuario>();
            using (var context = new db_Entidades())
            {
                foreach (var fi in di.GetFiles())
                {
                    AfisEngine Afis = new AfisEngine();
                    //Bitmap image1 = (Bitmap)Image.FromFile(@"C:\Users\PC_STE_19\Documents\Visual Studio 2015\Projects\BiometricFinger\images\" + fi.Name, true);
                    Bitmap image1 = (Bitmap)Image.FromFile(@"C:\Users\PC_STE_19\Desktop\DEDOS_ESCACENA\" + fi.Name, true);
                    Fingerprint f = new Fingerprint();
                    f.AsBitmap = image1;
                    Usuario usu = new Usuario();
                    Person persona = new Person();
                    persona.Fingerprints.Add(f);
                    Afis.Extract(persona);
                    usu.username = fi.Name;
                    usu.finger = f.AsIsoTemplate;
                    usuarios.Add(usu);
                    Console.WriteLine(fi.Name);
                    context.Usuario.Add(usu);
                }
                context.SaveChanges();
            }
        }
    }
}
