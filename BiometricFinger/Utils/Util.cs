using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BiometricFinger
{
    public class Util
    {
        public static void guardaImagen(string nombre, Bitmap bmp)
        {
            bmp.Save(@"C:\Huellas\" + nombre + ".jpg");
            //image.Save(@"C:\Huellas\imagenREMOTO.jpg"); //Guardamos en disco
        }
    }
}
