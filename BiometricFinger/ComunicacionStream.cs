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

            return streamEncoding.GetString(inBuffer);
        }

        /// <summary>
        /// Leer un tipo imagen
        /// </summary>
        /// <remarks>
        /// Lee un tipo Image enviado desde el cliente mediante una conexión Pipe,
        /// parar crear FingerPrint con dicha Image.
        /// </remarks>
        /// <returns>Fingerprint</returns>
        public Fingerprint leeImage()
        {
            int len = 0;
            len = ioStream.ReadByte() * 256;
            len += ioStream.ReadByte();
            byte[] inBuffer = new byte[len];           
            int aux = 0;

            for (int i = len ; i > 0; i = i - 1024)
            {
                if(i < 1024)
                {
                    ioStream.Read(inBuffer, aux, i);
                }
                else
                {
                    ioStream.Read(inBuffer, aux, 1024);
                    aux += 1024;
                }
            }

            ioStream.Flush();
            Bitmap bmp;
            using (var ms = new MemoryStream(inBuffer))
            {
                Image image = Image.FromStream(ms);
                bmp = (Bitmap)image;
                image.Save("c:\\imagenREMOTO.jpg");
            }

            Fingerprint fingerPrint = new Fingerprint();
            fingerPrint.AsBitmap = bmp;

            return fingerPrint;

            /*Image image = Image.FromStream(ioStream);
            Fingerprint fingerPrint = new Fingerprint();
            fingerPrint.AsBitmap = (Bitmap)image;

            return fingerPrint;*/
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

        public int enviaUsuario(Usuario usuarioVerificado)
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
