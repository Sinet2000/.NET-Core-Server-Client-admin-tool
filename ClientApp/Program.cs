using Newtonsoft.Json;
using ServerSide;
using ServerSide.Extensions;
using System;
using System.Net;
using System.Net.Sockets;

namespace ClientApp
{
    class Program
    {
        static void Main(string[] args)
        {
            var socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            var ownEndpoint = new IPEndPoint(IPAddress.Parse("127.0.0.1"), 4321);
            socket.Bind(ownEndpoint);

            Console.WriteLine("\n Trying to connect to server ...");
            socket.Connect(IPAddress.Parse("127.0.0.1"), 1234);

            const int bytesize = 1024 * 1024;

            while (true)
            {
                byte[] buffer = new byte[bytesize];
                int bytesReceived = socket.Receive(buffer);
                var response = System.Text.Encoding.UTF8.GetString(buffer, 0, bytesReceived);
                Console.WriteLine(response);
                // send command
                var command = Console.ReadLine();

                var data = System.Text.Encoding.UTF8.GetBytes(command);
                socket.Send(data);

                if (command == ServerCommands.Quit.GetDescription())
                    break;
            }


            socket.Close(); 
        }

        private static byte[] sendMessage(byte[] messageBytes)
        {
            const int bytesize = 1024 * 1024;

            try
            {
                TcpClient tcpClient = new TcpClient("127.0.0.1", 1234);
                NetworkStream stream = tcpClient.GetStream();

                stream.Write(messageBytes, 0, messageBytes.Length);
                Console.WriteLine("================================");
                Console.WriteLine("=   Connected to the server    =");
                Console.WriteLine("================================");
                Console.WriteLine("Waiting for response...");

                messageBytes = new byte[bytesize];

                // receive the stream of bytes
                stream.Read(messageBytes, 0, messageBytes.Length);

                // Calean up
                stream.Dispose();
                tcpClient.Close();
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }

            return messageBytes;
        }
    }
}
