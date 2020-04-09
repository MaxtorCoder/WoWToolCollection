using System;
using System.Threading;
using System.Net.Sockets;
using System.Net;
using System.IO;
using System.Text;

namespace RibbitServer
{
    class Program
    {
        private static TcpListener Listener;
        private static Socket Socket;
        private static byte[] buffer = new byte[4096];

        static void Main(string[] args)
        {
            Listener = new TcpListener(IPAddress.Parse("127.0.0.1"), 1119);
            Listener.Start();

            Console.WriteLine("Started Ribbit Server...");
            new Thread(ListenThread).Start();
        }

        static void ListenThread()
        {
            while (true)
            {
                while (Listener.Pending())
                {
                    Socket = Listener.AcceptSocket();
                    Console.WriteLine($"{Socket.RemoteEndPoint.ToString()} connected to Ribbit Server.");

                    Socket.BeginReceive(buffer, 0, buffer.Length, SocketFlags.None, ReceiveDataCallback, null);
                }
            }
        }

        private static void ReceiveDataCallback(IAsyncResult ar)
        {
            try
            {
                var length = Socket.EndReceive(ar);
                if (length == 0)
                    return;

                var data = new byte[length];
                Buffer.BlockCopy(buffer, 0, data, 0, length);

                var command = Encoding.UTF8.GetString(data);
                Console.WriteLine($"C -> S: {command.Replace("\r\n", "")} [{data.DeserializePacket()}]");
                
                switch (command)
                {
                    case "v1/summary\r\n":
                        HandleSummary();
                        break;
                }

                Socket.BeginReceive(buffer, 0, buffer.Length, SocketFlags.None, ReceiveDataCallback, null);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }

        static void Send(byte[] data, int offset, int size)
        {
            Socket.Send(data, offset, size, SocketFlags.None);
        }

        static void HandleSummary()
        {
            var bytes = Encoding.UTF8.GetBytes(File.ReadAllText("Responses/summary.txt") + "\r\n");
            var length = bytes.Length;
            var offset = 0;

            while (length > 0)
            {
                if (length < 1514)
                    length -= length;
                else
                    length -= 1514;

                var newData = new byte[length];
                Buffer.BlockCopy(bytes, offset, newData, 0, length - 1);
                Send(newData, offset, length);
                offset += length;
            }
        }
    }
}
