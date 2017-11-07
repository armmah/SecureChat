using System;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace ChatServer
{
    class EstablishedConnections
    {
        //List<NetworkStream> streamList;
        public bool AllPartiesReady { get { return (user1 != null && user2 != null); } }
        NetworkStream user1, user2;
        Socket socket1, socket2;

        public NetworkStream First { get { return user1; } }
        public NetworkStream Second { get { return user2; } }

        public EstablishedConnections()
        {
            //streamList = new List<NetworkStream>();
        }
        public void AddEstablishedConnection(Socket socket, NetworkStream stream)
        {
            //streamList.Add(stream);
            if (user1 == null)
            {
                socket1 = socket;
                user1 = stream;
                Console.WriteLine("[Connection!]  First user connected.\r");

                StartReadingFromFirst();
                return;
            }
            if (user2 == null)
            {
                socket2 = socket;
                user2 = stream;
                Console.WriteLine("[Connection!]  Second user connected.\r");

                StartReadingFromSecond();
                return;
            }

            FlushAll();
            AddEstablishedConnection(socket, stream);
        }
        private void FlushAll()
        {
            FlushFirst();
            FlushSecond();
        }
        private void FlushFirst()
        {
            if (user1 != null)
            {
                if(socket2 != null && socket2.Connected)
                    socket2.Shutdown(SocketShutdown.Both);
                user1.Close();
                user1.Dispose();
                user1 = null;
            }
        }
        private void FlushSecond()
        {
            if (user2 != null)
            {
                if (socket1 != null && socket1.Connected)
                    socket1.Shutdown(SocketShutdown.Both);
                user2.Close();
                user2.Dispose();
                user2 = null;
            }
        }
        byte[] buf1;
        private void StartReadingFromFirst()
        {
            try
            {
                buf1 = new byte[1500];

                if(user1.CanRead)
                    user1.BeginRead(buf1, 0, buf1.Length, new AsyncCallback(ReceivedFromFirst), user1);
            }
            catch (SocketException e)
            { Console.WriteLine(e.ToString()); }
        }
        private void ReceivedFromFirst(IAsyncResult ar)
        {
            try
            {
                if (user1 == null || user2 == null)
                    return;

                Console.Write("Msg 1,...");
                user1.EndRead(ar);
                byte[] data = buf1;

                JsonMessage msg = JsonMessage.GetJsonMessage(data);
                if (msg == null || msg.State ==
                    (short)JsonMessage.ConnectionState.Closed)
                {
                    Console.WriteLine("[Disconnect] First user have disconnected. Closing the channel.");
                    user2.Write(data, 0, data.Length);

                    FlushFirst();
                    FlushSecond();
                    return;
                }

                user2.Write(data, 0, data.Length);
                Console.WriteLine(" -> 2.\r");
                StartReadingFromFirst();
            }
            catch (SocketException e)
            { Console.WriteLine(e.ToString()); }
        }

        byte[] buf2;
        private void StartReadingFromSecond()
        {
            try
            {
                buf2 = new byte[1500];

                if (user2.CanRead)
                    user2.BeginRead(buf2, 0, buf2.Length, new AsyncCallback(ReceivedFromSecond), user2);
            }
            catch (SocketException e)
            { Console.WriteLine(e.ToString()); }
        }
        private void ReceivedFromSecond(IAsyncResult ar)
        {
            try
            {
                if (user1 == null || user2 == null)
                    return;

                Console.Write("Msg 2,...");
                user2.EndRead(ar);
                byte[] data = buf2;

                JsonMessage msg = JsonMessage.GetJsonMessage(data);
                if (msg == null || msg.State ==
                    (short)JsonMessage.ConnectionState.Closed)
                {
                    Console.WriteLine("[Disconnect] Second user have disconnected. Closing the channel.");
                    user1.Write(data, 0, data.Length);

                    FlushSecond();
                    FlushFirst();
                    return;
                }

                user1.Write(data, 0, data.Length);
                Console.WriteLine(" -> 1.\r");
                StartReadingFromSecond();
            }
            catch (SocketException e)
            { Console.WriteLine(e.ToString()); }
        }
        ~EstablishedConnections()
        { FlushAll(); }
    }
}
