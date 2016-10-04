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
                using (var context = new db_Entidades()){

                    NamedPipeServerStream pipeServer = new NamedPipeServerStream("testfinger", PipeDirection.InOut, numThreads);

                    int threadId = Thread.CurrentThread.ManagedThreadId;
                    // Esperar a que un cliente se conecte
                    pipeServer.WaitForConnection();
                    Console.WriteLine("Cliente conectado a thread[{0}] .", threadId);
                    try{
                        // Leer la solicitud del cliente. Una vez que el cliente ha escrito a la tubería estará disponible.
                        ComunicacionStream cS = new ComunicacionStream(pipeServer);
                        cS.enviaCadena("Conectado al servidor");

                        Fingerprint fingerPrint = cS.leeImage();
                        UsuarioAFIS usuarioABuscar = new UsuarioAFIS();
                        usuarioABuscar.Fingerprints.Add(fingerPrint);

                        //Creamos Objeto AfisEngine el cual realiza la identificación de usuarios 
                        AfisEngine Afis = new AfisEngine();
                        // Marcamos límite para verificar una huella como encontrada
                        Afis.Threshold = 100;
                        Afis.Extract(usuarioABuscar);

                        //Obtenemos los usuarios registrados en la base de datos
                        var usuariosBBDD = context.Usuario.ToList();
                        //Lista de tipo UsuarioAFIS, los cuales rellenamos con plantillas de huellas dactilares e id de usuario de la base de datos
                        List<UsuarioAFIS> listaUsuariosAFIS = new List<UsuarioAFIS>();

                        foreach (var usuario in usuariosBBDD){
                            Fingerprint fingerPrintAUX = new Fingerprint();
                            fingerPrintAUX.AsIsoTemplate = usuario.finger;
                            UsuarioAFIS usuarioAFIS_AUX = new UsuarioAFIS();
                            usuarioAFIS_AUX.id = usuario.id;
                            usuarioAFIS_AUX.Fingerprints.Add(fingerPrintAUX);
                            listaUsuariosAFIS.Add(usuarioAFIS_AUX);
                        }
                        //Realiza la busqueda 
                        UsuarioAFIS usuarioEncontrado = Afis.Identify(usuarioABuscar, listaUsuariosAFIS).FirstOrDefault() as UsuarioAFIS;
                        if (usuarioEncontrado == null){
                            Console.WriteLine("No se ha encontrado");
                            cS.enviaCadena("NO IDENTIFICADO");
                        }
                        else{
                            //Obtenemos la puntuación de los usuarios identificados
                            float puntuacion = Afis.Verify(usuarioABuscar, usuarioEncontrado);
                            Usuario usuarioCompleto = usuariosBBDD.Find(x => x.id == usuarioEncontrado.id);
                            cS.enviaCadena("IDENTIFICADO");
                            cS.enviaCadena(usuarioCompleto.username);
                            Console.WriteLine("Encontrado con: {0:F3}, Nombre: {1}", puntuacion, usuarioCompleto.username);
                        }
                    }
                    //Captura IOException si la tubería se rompe o desconecta.
                    catch (IOException e){
                        Console.WriteLine("ERROR: {0}", e.Message);
                    }
                    //Cerramos la tubería
                    pipeServer.Close();
                }
            }
        }

        static void insertaHuellasDesdeCarpeta()
        {
            // Inicializa la carpeta de imagenes
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
}
