using SourceAFIS.Simple;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;

namespace BiometricFinger
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            using (var context = new db_Entidades())
            {
                //insertaHuellasDesdeCarpeta();
                var usuarios = context.Usuario.ToList();
                List<Person> personas = new List<Person>();
                foreach (var u in usuarios)
                {
                    Fingerprint fingerPrint = new Fingerprint();
                    fingerPrint.AsIsoTemplate = u.finger;
                    Person personaAUX = new Person();
                    personaAUX.Fingerprints.Add(fingerPrint);
                    personas.Add(personaAUX);
                    Console.WriteLine(u.id + " " + u.username + " " + u.finger);
                }
                DirectoryInfo di = new DirectoryInfo(@"C:\Users\aesca\OneDrive\Documentos\Visual Studio 2015\Projects\BiometricFinger\images");
                AfisEngine Afis = new AfisEngine();

                Bitmap image1 = (Bitmap)Image.FromFile(@"C:\Users\aesca\OneDrive\Documentos\Visual Studio 2015\Projects\BiometricFinger\alterImages\020_2_2_muchas_lineas.jpg", true);
                Fingerprint f = new Fingerprint();
                f.AsBitmap = image1;
                Usuario usu = new Usuario();
                Person persona = new Person();
                persona.Fingerprints.Add(f);
                Afis.Extract(persona);

                Afis.Threshold = 10;
                Person encontrada = Afis.Identify(persona, personas).FirstOrDefault() as Person;
                if(encontrada == null)
                {
                    Console.WriteLine("No se ha encontrado");
                }
                float score = Afis.Verify(persona, encontrada);
                Console.WriteLine("Encontrado con: {0:F3} ", score);
            }
            //            Application.EnableVisualStyles();
            //          Application.SetCompatibleTextRenderingDefault(false);
            //        Application.Run(new Form1());
        }
        static void insertaHuellasDesdeCarpeta()
        {
            // Initialize path to images
            DirectoryInfo di = new DirectoryInfo(@"C:\Users\aesca\OneDrive\Documentos\Visual Studio 2015\Projects\BiometricFinger\images");
            Console.WriteLine("No search pattern returns:");
            List<Usuario> usuarios = new List<Usuario>();
            using (var context = new db_Entidades())
            {
                foreach (var fi in di.GetFiles())
                {
                    AfisEngine Afis = new AfisEngine();
                    //Bitmap image1 = (Bitmap)Image.FromFile(@"C:\Users\PC_STE_19\Documents\Visual Studio 2015\Projects\BiometricFinger\images\" + fi.Name, true);
                    Bitmap image1 = (Bitmap)Image.FromFile(@"C:\Users\aesca\OneDrive\Documentos\Visual Studio 2015\Projects\BiometricFinger\images\" + fi.Name, true);
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
