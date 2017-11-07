using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;

namespace ChatServer
{
    class Server
    {
        TcpListener network;
        EstablishedConnections clients;

        public Server()
        {
            clients = new EstablishedConnections();
            int i = 27010;
            IPAddress ip = FindLocalIP();
            back:
            try
            {
                network = new TcpListener(ip, i);
            }
            catch
            {
                i++;
                goto back;
            }
            network = new TcpListener(ip, i);
            
            Console.WriteLine("Binded to IP: [{0}:{1}]\r", ip, i);
            
            network.Start();
            CatchNewClients();
        }
        public void CatchNewClients()
        {
            try
            {
                string waiting = "Waiting for a connection...";
                Console.Write(waiting);
                    
                network.BeginAcceptTcpClient(new AsyncCallback(TcpClientAccepted), network);

            }
            catch (SocketException e)
            {
                Console.WriteLine("SocketException: {0}", e);
            }
        }
        private void TcpClientAccepted(IAsyncResult ar)
        {
            try
            {
                TcpListener listener = (TcpListener)ar.AsyncState;
                TcpClient client = listener.EndAcceptTcpClient(ar);
                NetworkStream stream = client.GetStream();
                clients.AddEstablishedConnection(client.Client, stream);
                Console.WriteLine("[Connection!]    Established a new connection with: {0}\r", client.Client.RemoteEndPoint.ToString());

                if (clients.AllPartiesReady)
                {
                    //Both Parties have connected
                    JsonMessage msg = new JsonMessage((short)JsonMessage.ConnectionState.Established, "");
                    WriteAsync(clients.First, msg);
                    WriteAsync(clients.Second, msg);
                }
                else
                {
                    //Waiting for the other party to connect.
                    WriteAsync(stream, (short)JsonMessage.ConnectionState.Null, "");
                    CatchNewClients();
                }
            }
            catch (SocketException e)
            { Console.WriteLine("SocketException: {0}", e); }
        }
        private void WriteAsync(NetworkStream stream, JsonMessage msg)
        {         WriteAsync(stream, msg.State, msg.Message);        }
        private void WriteAsync(NetworkStream stream, short state, string text)
        {
            try { 

                byte[] buf;
                buf = JsonMessage.GetBytes(state, text);
                stream.WriteAsync(buf, 0, buf.Length);
            } catch (SocketException e)
            { Console.WriteLine("SocketException: {0}", e); }
        }
        /*
        private void WritingAsync(IAsyncResult ar)
        {
            try {

                stream.EndWrite(ar);
                Console.WriteLine("--------Writing succeded?---------");

            } catch (SocketException e)
            { Console.WriteLine("SocketException: {0}", e); }
        }
        */
        private static IPAddress FindLocalIP()
        {
            IPHostEntry host;
            host = Dns.GetHostEntry(Dns.GetHostName());
            foreach (IPAddress ip in host.AddressList)
            {
                if (ip.AddressFamily == AddressFamily.InterNetwork)
                    return ip;
            }
            return IPAddress.Parse("127.0.0.1");
        }
    }
}