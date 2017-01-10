using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Collections.Concurrent;
using SourceAFIS.Simple;
using static BiometricFinger.Program;
using System.Data.Entity;

namespace BiometricFinger
{
    public delegate void ServerHandlePacketData(byte[] data, int bytesRead, TcpClient client, int type);

    /// <summary>
    /// Implementa un servidor TCP simple que utiliza un hilo por cliente
    /// </summary>
    public class Server
    {
        public event ServerHandlePacketData OnDataReceived;

        private TcpListener listener;
        private ConcurrentDictionary<TcpClient, NetworkBuffer> clientBuffers;
        private List<TcpClient> clients;
        private int sendBufferSize = 1024;
        private int readBufferSize = 1024;
        private int port;
        private bool started = false;

        /// <summary>
        /// Lista de clientes conectados actualmente
        /// </summary>
        public List<TcpClient> Clients
        {
            get
            {
                return clients;
            }
        }

        /// <summary>
        /// Número de clientes conectados actualmente
        /// </summary>
        public int NumClients
        {
            get
            {
                return clients.Count;
            }
        }

        /// <summary>
        /// Construye un nuevo servidor TCP que se escucha en un puerto dado
        /// </summary>
        /// <param name="port"></param>
        public Server(int port)
        {

            this.port = port;
            clientBuffers = new ConcurrentDictionary<TcpClient, NetworkBuffer>();
            clients = new List<TcpClient>();
        }

        /// <summary>
        /// Comienza a escuchar en el puerto proporcionado al constructor
        /// </summary>
        public void Start()
        {
            listener = new TcpListener(IPAddress.Parse("161.33.129.202"), port);
            //listener = new TcpListener(IPAddress.Parse("192.168.1.137"), port);
            Console.WriteLine("Started server on port " + port);

            Thread thread = new Thread(new ThreadStart(ListenForClients));
            thread.Start();
            started = true;
        }

        /// <summary>
        /// Se ejecuta en su propio hilo. Responsable de aceptar nuevos clientes
        /// </summary>
        private void ListenForClients()
        {
            listener.Start();

            while (started)
            {
                TcpClient client = listener.AcceptTcpClient();
                Thread clientThread = new Thread(new ParameterizedThreadStart(WorkWithClient));
                Console.WriteLine("Nuevo cliente conectado");

                NetworkBuffer newBuff = new NetworkBuffer();
                newBuff.WriteBuffer = new byte[sendBufferSize];
                newBuff.ReadBuffer = new byte[readBufferSize];
                newBuff.CurrentWriteByteCount = 0;
                clientBuffers.GetOrAdd(client, newBuff);
                clients.Add(client);

                clientThread.Start(client);
                Thread.Sleep(15);
            }
        }

        /// <summary>
        /// Detiene el servidor a aceptar nuevos clientes
        /// </summary>
        public void Stop()
        {
            if (!listener.Pending())
            {
                listener.Stop();
                started = false;
            }
        }

        /// <summary>
        /// Este método se ejecuta en un hilo, uno por cliente. Responsable de la lectura de datos desde el
        /// cliente y enviando los datos al cliente.
        /// </summary>
        /// <param name="client"></param>
        private void WorkWithClient(object client)
        {
            TcpClient tcpClient = client as TcpClient;
            bool started = true;
            if (tcpClient == null)
            {
                Console.WriteLine("TCP cliente es nulo, detener el procesamiento de este cliente");
                DisconnectClient(tcpClient);
                return;
            }
            ComunicacionStream cS = new ComunicacionStream(tcpClient.GetStream());
            Boolean clienteConectado = true;
            string estado = "INICIAL";  //Estado en el cual comienza la maquina de estado (de momento, caso es el estado por defecto)

            while (started)
            {
                try
                {
                    switch (estado)
                    {
                        case "VERIFICA_HUELLA":
                            verificaPersona(cS);
                            estado = "FIN";
                            break;
                        case "INSERTA_HUELLA":
                            insertaHuella(cS);
                            estado = "FIN";
                            break;
                        case "FIN":
                            //cS.enviaCadena("Cierre de conexión");
                            started = false;
                            clienteConectado = false;
                            break;
                        default:
                            //Console.WriteLine("Default case");
                            cS.limpiar();
                            estado = cS.leeCadena();
                            break;
                    }
                }catch(Exception e)
                {
                    //Se ha producido un error de socket
                    Console.WriteLine("Un error de socket ha ocurrido con el cliente: " + tcpClient.ToString());
                    break;
                }

                if (!clienteConectado)
                {
                    //el cliente ha desconectado del servidor o se fuerza la desconexión
                    break;
                }

               /* if (OnDataReceived != null)
                {
                    //Send off the data for other classes to handle
                    OnDataReceived(clientBuffers[tcpClient].ReadBuffer, bytesRead, tcpClient, type);
                }*/

                Thread.Sleep(15);
            }

            DisconnectClient(tcpClient);
        }

        private bool verificaPersona(ComunicacionStream cS)
        {
            bool result = false;
            bool started = true;   //COLOCAR A TRUE, SI NO NO ENTRA EN EL WHILE Y NO REALIZA NINGUNA COMPROBACIÓN U OPERACIÓN
            string estado = "VERIFICA_HUELLA";  //Estado en el cual comienza la maquina de estado (de momento, caso es el estado por defecto)
            Fingerprint fingerPrint = null;
            Persona usuarioVerificado = null;

            while (started)
            {
                try
                {
                    switch (estado)
                    {
                        case "VERIFICA_HUELLA":
                            //bloquea hasta que un cliente envía imagen de dedo
                            Console.WriteLine("Recibe imagen de huella dactilar");
                            fingerPrint = cS.leeImage();    //Recoge la imagen enviada por el cliente
                            Console.WriteLine("Imagen recibida, comprueba si huella corresponde con alguna en BBBDD");
                            usuarioVerificado = verificaHuella(fingerPrint);    //Llama a la función para realizar la verificación de la huella
                            Console.WriteLine("-------> ENVIA CADENA A CLIENTE");
                            if (usuarioVerificado.nombre != null)
                                //cS.enviaUsuario(usuarioVerificado);
                                cS.enviaCadena(usuarioVerificado.id_personal.ToString());
                            else
                                cS.enviaCadena("NO VERIFICADO");
                            estado = "FIN";
                            break;
                        case "ENVIA_PERSONA":
                            if (usuarioVerificado != null)  //Huella verificada
                                cS.enviaCadena(usuarioVerificado.id_personal + "");  //Se envía al cliente id_personal correspondiente a la huella verificada
                            else   //Huella no verificada
                                cS.enviaCadena("NO VERIFICADO");    //Se envía al usuario que la huella no ha sido verificada

                            estado = "FIN";
                            break;
                        case "FIN":
                            started = false;
                            result = true;
                            break;
                        default:
                            //Console.WriteLine("Default case");
                            cS.limpiar();
                            //estado = cS.leeCadena();
                            break;
                    }
                }
                catch (Exception e)
                {
                    //Se ha producido un error de socket
                    Console.WriteLine("Un error de socket ha ocurrido con el cliente");
                    break;
                }
                Thread.Sleep(15);
            }
            return result;
        }

        /// <summary>
        /// MAQUINA DE ESTADOS: Inserta la huella recibida a el usuario indicado por el cliente.
        /// </summary>
        /// <param name="cS">Comunicación entre Servidor/Cliente</param>
        private bool insertaHuella(ComunicacionStream cS)
        {
            bool result = false;
            string estado = "RECIBE_PERSONA";  //Estado en el cual comienza la maquina de estado (de momento, caso es el estado por defecto)
            bool started = true;
            Fingerprint fingerPrint = null; //Huella recibida a insertar/actualizar
            Persona persona = null; //Persona a insertar/actualizar huella
            DAO DAO = new DAO();    //Data Access Objet, objeto de acceso a base de datos

            while (started)
            {
                try
                {
                    switch (estado)
                    {
                        case "RECIBE_PERSONA":
                            //bloquea hasta que un cliente envía cadena
                            Console.WriteLine("Recibe identificador de la persona");
                            cS.limpiar();
                            string cadenaRecibida = cS.leeCadena(); //Debe recibir identificador y huella a insertar/actualizar (AÚN NO SE SI TIPO JSON)
                            Console.WriteLine("Identificador recibido, comprueba si el identificador corresponde en BBBDD");
                            Console.WriteLine("La cadena recibida es:"+ cadenaRecibida);
                            persona = DAO.getPersona(Int32.Parse(cadenaRecibida));    //Llama a la función para realizar la búsqueda de la persona

                            if (persona != null)
                            {  //Huella verificada
                                cS.enviaCadena("ID: " + persona.id_personal + ", Nombre: " + persona.nombre);  //Se envía al cliente la persona encontrada con ese id
                                estado = "RECIBE_HUELLA";    //Al verificar la persona correctamente, la maquina pasa al estado RECIBE_HUELLA
                            }
                            else
                            {   //Huella no verificada
                                cS.enviaCadena("NO ENCONTRADO");    //Se envía al usuario que la persona no ha sido encontrada
                                estado = "";    //Pasamos al estado por defecto
                                started = false;    //Colocamos a false el started para que termine la maquina de estado
                            }
                            break;

                        case "RECIBE_HUELLA":
                            //bloquea hasta que un cliente envía imagen de dedo
                            Console.WriteLine("Recibe imagen de huella dactilar");
                            fingerPrint = cS.leeImage();    //Recoge la imagen enviada por el cliente
                            Console.WriteLine("Imagen recibida");
                            estado = "INSERTA_HUELLA_PERSONA";                 
                            break;

                        case "INSERTA_HUELLA_PERSONA":
                            bool operacionOK = DAO.insertaHuella(persona.id_personal, fingerPrint.AsImageData, 1); //Realizamos la actualización de la huella en BBDD (HAY QUE INDICAR QUE HUELLA ACTUALIZAR)

                            if (!operacionOK)   //Huella no verificada
                            {
                                cS.enviaCadena("ERROR");
                                result = false;
                            }
                            else //Huella insertada con exito
                            {
                                cS.enviaCadena("OK");
                            }

                            estado = "FIN";
                            break;

                        case "FIN":
                            //cS.enviaCadena("FIN INSERTA HUELLA");
                            started = false;    //Colocamos started a false para finalizar la maquina de estados
                            //result = true;
                            break;

                        default:
                            Console.WriteLine("ESTADO POR DEFECTO");
                            cS.limpiar();
                            estado = cS.leeCadena();
                            break;
                    }
                }
                catch (Exception e)
                {
                    //Se ha producido un error de socket
                    Console.WriteLine("Un error de socket ha ocurrido con el cliente");
                    break;
                }

                Thread.Sleep(15);
            }

            return result;
        }

        private Persona verificaHuella(Fingerprint fingerPrint)
        {
            Persona usuarioVerificado = null;

            using (var context = new db_Entidades())
            {
                UsuarioAFIS usuarioABuscar = new UsuarioAFIS();
                usuarioABuscar.Fingerprints.Add(fingerPrint);

                //Creamos Objeto AfisEngine el cual realiza la identificación de usuarios 
                AfisEngine Afis = new AfisEngine();
                // Marcamos límite para verificar una huella como encontrada
                Afis.Threshold = 50;
                Afis.Extract(usuarioABuscar);

                //Obtenemos los usuarios registrados en la base de datos
                var usuariosBBDD = context.Personal.ToList();
                //Lista de tipo UsuarioAFIS, los cuales rellenamos con plantillas de huellas dactilares e id de usuario de la base de datos
                List<UsuarioAFIS> listaUsuariosAFIS = new List<UsuarioAFIS>();

                foreach (var usuario in usuariosBBDD)
                {
                    Fingerprint fingerPrintAUX = new Fingerprint();
                    if (usuario.huella1 != null)
                    {
                        fingerPrintAUX.AsImageData = usuario.huella1;
                        UsuarioAFIS usuarioAFIS_AUX = new UsuarioAFIS();
                        usuarioAFIS_AUX.id = usuario.id_personal;
                        usuarioAFIS_AUX.Fingerprints.Add(fingerPrintAUX);
                        Afis.Extract(usuarioAFIS_AUX);
                        listaUsuariosAFIS.Add(usuarioAFIS_AUX);
                    }
                }
                //Realiza la busqueda 
                UsuarioAFIS usuarioEncontrado = Afis.Identify(usuarioABuscar, listaUsuariosAFIS).FirstOrDefault() as UsuarioAFIS;
                if (usuarioEncontrado == null)
                {
                    Console.WriteLine("No se ha encontrado");
                    //cS.enviaCadena("NO IDENTIFICADO");
                    usuarioVerificado = null;
                }
                else
                {
                    //Obtenemos la puntuación de los usuarios identificados
                    float puntuacion = Afis.Verify(usuarioABuscar, usuarioEncontrado);
                    usuarioVerificado = usuariosBBDD.Find(x => x.id_personal == usuarioEncontrado.id);
                    //cS.enviaCadena("IDENTIFICADO");
                    //cS.enviaCadena(usuarioCompleto.username);
                    Console.WriteLine("Encontrado con: {0:F3}, Nombre: {1}", puntuacion, usuarioVerificado.nombre);
                }
            }
            return usuarioVerificado;
        }

        /// <summary>
        /// Elimina un cliente determinado de nuestra lista de clientes
        /// </summary>
        /// <param name="client"></param>
        private void DisconnectClient(TcpClient client)
        {
            if (client == null)
            {
                return;
            }

            Console.WriteLine("Disconnected client: " + client.ToString());

            client.Close();

            clients.Remove(client);
            NetworkBuffer buffer;
            clientBuffers.TryRemove(client, out buffer);
        }

        /// <summary>
        /// Agrega datos para que el paquete sea enviado, pero no lo envía a través de la red
        /// </summary>
        /// <param name="data">Los datos para enviart</param>
        /// <param name="client">Cliente al que se le envía</param>
        public void AddToPacket(byte[] data, TcpClient client)
        {
            if (clientBuffers[client].CurrentWriteByteCount + data.Length > clientBuffers[client].WriteBuffer.Length)
            {
                FlushData(client);
            }

            Array.ConstrainedCopy(data, 0, clientBuffers[client].WriteBuffer, clientBuffers[client].CurrentWriteByteCount, data.Length);

            clientBuffers[client].CurrentWriteByteCount += data.Length;
        }

        /// <summary>
        /// Agrega datos para que el paquete sea enviado. 
        /// Estos datos se envía a cada cliente conectado
        /// </summary>
        /// <param name="data">Los datos que se envían</param>
        public void AddToPacketToAll(byte[] data)
        {
            lock (clients)
            {
                foreach (TcpClient client in clients)
                {
                    if (clientBuffers[client].CurrentWriteByteCount + data.Length > clientBuffers[client].WriteBuffer.Length)
                    {
                        FlushData(client);
                    }

                    Array.ConstrainedCopy(data, 0, clientBuffers[client].WriteBuffer, clientBuffers[client].CurrentWriteByteCount, data.Length);

                    clientBuffers[client].CurrentWriteByteCount += data.Length;
                }
            }
        }

        /// <summary>
        /// Envía y vacía todos los datos salientes al cliente identificado
        /// </summary>
        /// <param name="client"></param>
        private void FlushData(TcpClient client)
        {
            client.GetStream().Write(clientBuffers[client].WriteBuffer, 0, clientBuffers[client].CurrentWriteByteCount);
            client.GetStream().Flush();
            clientBuffers[client].CurrentWriteByteCount = 0;
        }

        /// <summary>
        /// Envía y vacía todos los datos salientes a cada cliente
        /// </summary>
        private void FlushDataToAll()
        {
            lock (clients)
            {
                foreach (TcpClient client in clients)
                {
                    client.GetStream().Write(clientBuffers[client].WriteBuffer, 0, clientBuffers[client].CurrentWriteByteCount);
                    client.GetStream().Flush();
                    clientBuffers[client].CurrentWriteByteCount = 0;
                }
            }
        }

        /// <summary>
        /// Envía los datos inmediatamente al cliente especificado
        /// </summary>
        /// <param name="data">Los datos que se envían</param>
        /// <param name="client">Cliente a enviar los datos</param>
        public void SendImmediate(byte[] data, TcpClient client)
        {
            AddToPacket(data, client);
            FlushData(client);
        }

        /// <summary>
        /// Envía los datos de inmediato a todos los clientes
        /// </summary>
        /// <param name="data">Los datos que se envían</param>
        public void SendImmediateToAll(byte[] data)
        {
            lock (clients)
            {
                foreach (TcpClient client in clients)
                {
                    AddToPacket(data, client);
                    FlushData(client);
                }
            }
        }
    }
}