using SourceAFIS.Simple;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.IO.Pipes;
using System.Linq;
using System.Text;
using System.Threading;

namespace BiometricFinger
{
    static class Program
    {
        public static int numThreads = 2;
        
        static void Main()
        {
            //insertaHuellasDesdeCarpeta();
            Thread[] server = new Thread[numThreads];
            Console.WriteLine("\n*** Esperando la conexión de clientes...");

            for(int i = 0; i<numThreads; i++)
            {
                server[i] = new Thread(ServerThread);
                server[i].Start();
            }
            Thread.Sleep(250);
            //            Application.EnableVisualStyles();
            //          Application.SetCompatibleTextRenderingDefault(false);
            //        Application.Run(new Form1());
        }

        private static void ServerThread(object data)
        {
            while (true)
            {
                using (var context = new db_Entidades())
                {
                    NamedPipeServerStream pipeServer = new NamedPipeServerStream("testfinger", PipeDirection.InOut, numThreads);

                    int threadId = Thread.CurrentThread.ManagedThreadId;
                    // Wait for a client to connect
                    pipeServer.WaitForConnection();
                    Console.WriteLine("Cliente conectado a thread[{0}] .", threadId);
                    try
                    {
                        // Read the request from the client. Once the client has
                        // written to the pipe its security token will be available.

                        StreamTemplate sT = new StreamTemplate(pipeServer);

                        // Verify our identity to the connected client using a
                        // string that the client anticipates.

                        sT.WriteString("I am the one true server!");
                        Person person = sT.ReadPerson();

                        var usuarios = context.Usuario.ToList();
                        List<Person> persons = new List<Person>();
                        foreach (var u in usuarios)
                        {
                            Fingerprint fingerPrint = new Fingerprint();
                            fingerPrint.AsIsoTemplate = u.finger;
                            Person personAUX = new Person();
                            personAUX.Fingerprints.Add(fingerPrint);
                            persons.Add(personAUX);
                            //Console.WriteLine(u.id + " " + u.username + " " + u.finger);
                        }
                        AfisEngine Afis = new AfisEngine();

                        Afis.Threshold = 10;
                        Person encontrada = Afis.Identify(person, persons).FirstOrDefault() as Person;
                        if (encontrada == null)
                        {
                            Console.WriteLine("No se ha encontrado");
                        }
                        else
                        {
                            float score = Afis.Verify(person, encontrada);
                            Console.WriteLine("Encontrado con: {0:F3} ", score);
                        }
                    }
                    // Catch the IOException that is raised if the pipe is broken
                    // or disconnected.
                    catch (IOException e)
                    {
                        Console.WriteLine("ERROR: {0}", e.Message);
                    }
                    pipeServer.Close();
                }
            }
        }

        static void insertaHuellasDesdeCarpeta()
        {
            // Initialize path to images
            DirectoryInfo di = new DirectoryInfo(@"C:\Users\PC_STE_19\Documents\Visual Studio 2015\Projects\BiometricFinger\images");
            //DirectoryInfo di = new DirectoryInfo(@"C:\Users\aesca\OneDrive\Documentos\Visual Studio 2015\Projects\BiometricFinger\images");
            Console.WriteLine("No search pattern returns:");
            List<Usuario> usuarios = new List<Usuario>();
            using (var context = new db_Entidades())
            {
                foreach (var fi in di.GetFiles())
                {
                    AfisEngine Afis = new AfisEngine();
                    Bitmap image1 = (Bitmap)Image.FromFile(@"C:\Users\PC_STE_19\Documents\Visual Studio 2015\Projects\BiometricFinger\images\" + fi.Name, true);
                    //Bitmap image1 = (Bitmap)Image.FromFile(@"C:\Users\aesca\OneDrive\Documentos\Visual Studio 2015\Projects\BiometricFinger\images\" + fi.Name, true);
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

    public class StreamTemplate
    {
        private Stream ioStream;
        private UnicodeEncoding streamEncoding;

        public StreamTemplate(Stream ioStream)
        {
            this.ioStream = ioStream;
            streamEncoding = new UnicodeEncoding();
        }

        public Person ReadPerson()
        {
            int len = 0;

            len = ioStream.ReadByte() * 256;
            len += ioStream.ReadByte();
            byte[] inBuffer = new byte[len];
            ioStream.Read(inBuffer, 0, len);
            Bitmap bmp;
            using (var ms = new MemoryStream(inBuffer))
            {
                Image image = Image.FromStream(ms);
                bmp = (Bitmap)image;
            }
            
            Fingerprint f = new Fingerprint();
            f.AsBitmap = bmp;
            Person persona = new Person();
            persona.Fingerprints.Add(f);
            AfisEngine Afis = new AfisEngine();
            Afis.Extract(persona);

            return persona;
        }

        public int WriteString(string outString)
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
    }
}
