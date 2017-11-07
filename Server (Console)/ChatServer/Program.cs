using System;
using System.Net.Sockets;

namespace ChatServer
{
    class Program
    {
        static void Main(string[] args)
        {
            try
            {
                Server network = new Server();
                while (true)
                    Console.ReadLine();
            }
            catch (SocketException e)
            { Console.WriteLine("SocketException: {0}", e); }
           // finally { Main(args); }
        }
    }
}