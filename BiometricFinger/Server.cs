using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Collections.Concurrent;

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
        /// </summary>W
        public void Start()
        {
            //listener = new TcpListener(IPAddress.Parse("161.33.129.189"), port);
            listener = new TcpListener(IPAddress.Parse("192.168.1.137"), port);
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
                Console.WriteLine("New client connected");

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
            if (tcpClient == null)
            {
                Console.WriteLine("TCP cliente es nulo, detener el procesamiento de este cliente");
                DisconnectClient(tcpClient);
                return;
            }

            NetworkStream clientStream = tcpClient.GetStream();
            int bytesRead;
            string type = "INICIAL";

            while (started)
            {
                bytesRead = 0;

                try
                {
                    //bloquea hasta que un cliente envía un mensaje
                    int len = clientStream.ReadByte() * 256;
                    len += clientStream.ReadByte();
                    clientBuffers[tcpClient].ReadImage = new byte[len];
                    bytesRead = clientStream.Read(clientBuffers[tcpClient].ReadImage, 0, len);
                }
                catch
                {
                    //Se ha producido un error de socket
                    Console.WriteLine("A socket error has occurred with client: " + tcpClient.ToString());
                    break;
                }

                if (bytesRead == 0)
                {
                    //el cliente ha desconectado del servidor
                    break;
                }

                if (type == "INICIAL")
                {
                    ASCIIEncoding encoder = new ASCIIEncoding();
                    string message = encoder.GetString(clientBuffers[tcpClient].ReadBuffer, 0, bytesRead);
                    Console.WriteLine(message);
                    type = message;
                }
                if(type == "COMPRUEBA_HUELLAs")
                {
                    
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