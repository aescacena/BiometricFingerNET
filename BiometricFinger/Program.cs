using SourceAFIS.Simple;
using System;
//using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

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
                insertaHuellasDesdeCarpeta();
                var usuarios = context.Usuario.ToList();
                foreach(var usu in usuarios)
                {
                    byte[] finger = usu.finger;
                    MemoryStream stream = new MemoryStream(finger, 0, finger.Length);
                    stream.Position = 0;
                    Image image = Image.FromStream(stream);
                    Console.WriteLine(usu.id + " " + usu.username + " " + usu.finger);
                }
            }
//            Application.EnableVisualStyles();
  //          Application.SetCompatibleTextRenderingDefault(false);
    //        Application.Run(new Form1());
        }
        static void insertaHuellasDesdeCarpeta()
        {
            // Initialize path to images
           DirectoryInfo di = new DirectoryInfo(@"C:\Users\PC_STE_19\Documents\visual studio 2015\Projects\BiometricFinger\images");
           Console.WriteLine("No search pattern returns:");
            using (var context = new db_Entidades())
            {
                int count = 0;
                foreach (var fi in di.GetFiles())
                {
                    count++;
                    Bitmap image1 = (Bitmap)Image.FromFile(@"C:\Users\PC_STE_19\Documents\Visual Studio 2015\Projects\BiometricFinger\images\" + fi.Name, true);
                    Fingerprint f = new Fingerprint();
                    f.AsBitmap = image1;
                    Usuario usu = new Usuario();
                    usu.id = count;
                    usu.username = fi.Name;
                    usu.finger = new byte[100];
                    context.Usuario.Add(usu);
                    Console.WriteLine(fi.Name);
                }
            }
        }
    }
}
