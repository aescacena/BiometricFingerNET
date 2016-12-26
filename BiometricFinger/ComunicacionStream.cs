using BiometricFinger;
using SourceAFIS.Simple;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace BiometricFinger
{
    public class ComunicacionStream
    {
        private Stream ioStream;
        private UnicodeEncoding streamEncoding;

        public ComunicacionStream(Stream ioStream)
        {
            this.ioStream = ioStream;
            streamEncoding = new UnicodeEncoding();
        }

        /// <summary>
        /// Cadena String en Stream
        /// </summary>
        /// <remarks>
        /// Realiza la escritura en Stream de la cadena String recibida por parámetros
        /// </remarks>
        /// <param name="outString"></param>
        /// <returns>int</returns>
        public int enviaCadena(string outString)
        {
            byte[] outBuffer = streamEncoding.GetBytes(outString);
            int len = outBuffer.Length;
            if (len > UInt16.MaxValue)
            {
                len = (int)UInt16.MaxValue;
            }
            ioStream.WriteByte((byte)(len / 256));
            ioStream.WriteByte((byte)(len & 255));
            ioStream.Write(outBuffer, 0, len);
            ioStream.Flush();

            return outBuffer.Length + 2;
        }
        public string leeCadena()
        {
            int len;
            ioStream.Flush();
            len = ioStream.ReadByte() * 256;
            len += ioStream.ReadByte();
            byte[] inBuffer = new byte[len];
            ioStream.Read(inBuffer, 0, len);
            ioStream.Flush();
            Console.WriteLine("-------------------> "+ Encoding.UTF8.GetString(inBuffer));
            return Encoding.UTF8.GetString(inBuffer);
        }

        /// <summary>
        /// Leer un tipo imagen
        /// </summary>
        /// <remarks>
        /// Lee un tipo Image enviado desde el cliente mediante una conexión TCP,
        /// parar crear FingerPrint con dicha Image.
        /// </remarks>
        /// <returns>Fingerprint</returns>
        public Fingerprint leeImage()
        {
            byte[] inBuffer = new byte[4096];   //Creamos array de bytes con tamaño inicial que vamos a leer en el buffer
            int aux = 0;    //variable auxiliar para contar el número de bytes leídos
            int condicional = 0;    //Variable condicional para salir del do while

            do{
                condicional = ioStream.Read(inBuffer, aux, 4096);   //Lee del buffer como máximo 4096 y almacena en condicional el total de bytes leídos
                aux += condicional; //suma la cantidad de bytes que acabamos de leer con los bytes leídos en vueltas anteriores
                Array.Resize(ref inBuffer, inBuffer.Length + condicional);  //redireccionamos el array de bytes en la medida justa leídos
                Thread.Sleep(50);   //Dormimos el hilo 0.5seg para que no pierda información en la lectura
            }
            while (condicional >= 4096);    //mientras la lectura de bytes sea menor que el condicional
            
            Bitmap bmp;
            using (var ms = new MemoryStream(inBuffer))
            {
                Image image = Image.FromStream(ms); //Guardamos el buffer en una variable tipo Image
                bmp = (Bitmap)image;    //Le aplicamos un "Cast" PARA ALMACENAR en bitmap
                image.Save("c:\\imagenREMOTO.jpg"); //Guardamos en disco
            }

            Fingerprint fingerPrint = new Fingerprint();
            fingerPrint.AsBitmap = bmp;

            return fingerPrint;
        }

        public int enviaImagen(Image image)
        {
            byte[] imageData;
            using (var ms = new MemoryStream())
            {
                image.Save(ms, ImageFormat.Jpeg);
                imageData = ms.ToArray();
            }

            //byte[] outBuffer = streamEncoding.GetBytes(outString);
            int len = imageData.Length;
            if (len > UInt16.MaxValue)
            {
                len = (int)UInt16.MaxValue;
            }

            ioStream.WriteByte((byte)(len / 256));
            ioStream.WriteByte((byte)(len & 255));
            ioStream.Write(imageData, 0, len);
            ioStream.Flush();

            return 1;
        }

        public int enviaUsuario(Persona usuarioVerificado)
        {
            BinaryFormatter formatter = new BinaryFormatter();
            try
            {
                formatter.Serialize(ioStream, usuarioVerificado);
                ioStream.Flush();
            }
            catch (SerializationException e)
            {
                Console.WriteLine("Error al realizar la Serialización de Usuario");
            }

            return 1;
        }

        public void limpiar()
        {
            ioStream.Flush();
        }
    }
}
