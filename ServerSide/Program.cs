using Newtonsoft.Json;
using System;
using System.Net;
using System.Net.Sockets;
using System.ComponentModel;
using System.Linq;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using ServerSide.Extensions;
using System.IO;
using System.Reflection;
using CSharpLib;

namespace ServerSide
{
    class Program
    {
        static void Main(string[] args)
        {
            var listeningSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            var localEndpoint = new IPEndPoint(IPAddress.Parse("127.0.0.1"), 1234);
            listeningSocket.Bind(localEndpoint);
            listeningSocket.Listen(1);

            Console.WriteLine(@"
            ===========================================================
                    Started listening requests at: {0}:{1}
            ============================================================",
            localEndpoint.Address, localEndpoint.Port);

            var connectedSocket = listeningSocket.Accept();
            listeningSocket.Close();
            listeningSocket.Dispose();

            Console.WriteLine("\nClient: " + connectedSocket.RemoteEndPoint.ToString());
            connectedSocket.Send(System.Text.Encoding.UTF8.GetBytes(ServerCommandsHelper.getInitialMessage()));

            const int bytesize = 1024 * 1024;
            byte[] buffer = new byte[bytesize];

            string rootFolderPath = Path.Combine(
                        Path.GetDirectoryName(
                            Assembly.GetExecutingAssembly().Location).Split("\\bin\\Debug\\netcoreapp3.1").First(),
                        Constants.RootFolderName);

            var currentFolderPath = rootFolderPath;

            while (true)
            {
                buffer = new byte[bytesize];
                int bytesReceived = connectedSocket.Receive(buffer);
                var commands = getClientTypedCommands(buffer, bytesReceived);

                if (commands.First() == ServerCommands.Quit.GetDescription())
                    break;

                if (commands.First() == ServerCommands.Help.GetDescription())
                {
                    connectedSocket.Send(System.Text.Encoding.UTF8.GetBytes(ServerCommandsHelper.getCommandsListAndDescription()));
                    continue;
                }

                if (commands.First() == ServerCommands.GetFilesList.GetDescription())
                {
                    sendFilesList(connectedSocket, currentFolderPath);
                    continue;
                }

                if (commands.First() == ServerCommands.CreateFile.GetDescription() && commands.Count == Constants.GetFileContentAndCreateFileCommandPartsCount)
                {
                    createFile(connectedSocket, commands[1], currentFolderPath);
                    continue;
                }

                if (commands.First() == ServerCommands.CreateFolder.GetDescription() && commands.Count == Constants.GetFileContentAndCreateFileCommandPartsCount)
                {
                    createFolder(connectedSocket, commands[1], currentFolderPath);
                    continue;
                }

                if (commands.First() == ServerCommands.GetFileContent.GetDescription() && commands.Count == Constants.GetFileContentAndCreateFileCommandPartsCount)
                {
                    displayFileContent(connectedSocket, commands[1], currentFolderPath);
                    continue;
                }

                if (commands.First() == ServerCommands.WriteInFile.GetDescription() && commands.Count == Constants.WriteInFileCommandPartsCount)
                {
                    writeContentToFile(connectedSocket, commands[1], commands.Last(), currentFolderPath);
                    continue;
                }

                if (commands.First() == ServerCommands.Remove.GetDescription() 
                    && commands.Count >= Constants.RemoveFileCommandPartsCount 
                    && commands.Count <= Constants.RemoveFolderCommandPartsCount)
                {
                    if (commands.Contains(Constants.FolderPrefix) && commands.Count == Constants.RemoveFolderCommandPartsCount)
                        removeFolder(connectedSocket, commands.Last(), currentFolderPath);
                    else
                        removeFile(connectedSocket, commands.Last(), currentFolderPath);

                    continue;
                }

                if (commands.First() == ServerCommands.Navigate.GetDescription())
                {
                    navigateToFolder(connectedSocket, ref currentFolderPath, rootFolderPath, commands.Last());
                    continue;
                }

                // if command wasn't correct
                connectedSocket.Send(System.Text.Encoding.UTF8.GetBytes("Incorrect command, please type -> help, to get commands list"));
            }

            var response = System.Text.Encoding.UTF8.GetBytes($"Bye!");
            connectedSocket.Send(response);
            connectedSocket.Close();
        }

        private static List<string> getClientTypedCommands(byte[] buffer, int bytesReceived)
        {
            var message = System.Text.Encoding.UTF8.GetString(buffer, 0, bytesReceived);
            //replace multiple spaces with one
            message = Regex.Replace(message, @"\s+", " ");

            return message.ToLower().Split(" ").ToList();
        }

        private static void sendFilesList(Socket connectedSocket, string filesPath)
        {
            var filesInfo = new List<string>();

            foreach (var file in Directory.GetFiles(filesPath))
            {
                var fileSize = new FileInfo(file).FormatBytes();
                var fileName = Path.GetFileName(file);
                filesInfo.Add($"{fileName}\t\t{fileSize}");
            }

            foreach(var folder in Directory.GetDirectories(filesPath))
            {
                filesInfo.Add($"{folder.Split(filesPath).Last()}\t\t[Folder]");
            }

            var response = string.Empty;

            if (filesInfo.Count == 0)
                response = "Folder is empty!";
            else
                response = string.Join('\n', filesInfo);

            connectedSocket.Send(System.Text.Encoding.UTF8.GetBytes(response));
        }

        private static void createFile(Socket connectedSocket, string fileName, string filesPath)
        {
            var filePath = Path.Combine(filesPath, fileName);
            var response = System.Text.Encoding.UTF8.GetBytes($"");

            if (!File.Exists(filePath))
            {
                File.CreateText(filePath).Dispose();
                response = System.Text.Encoding.UTF8.GetBytes($"\n\t File with name {fileName} is created!");
            }
            else
                response = System.Text.Encoding.UTF8.GetBytes($"\n\t File with name {fileName} already exists");

            connectedSocket.Send(response);
        }

        private static void createFolder(Socket connectedSocket, string folderName, string currentFolderPath)
        {
            var newFolderPath = Path.Combine(currentFolderPath, folderName);
            var response = System.Text.Encoding.UTF8.GetBytes($"");

            if (!Directory.Exists(newFolderPath))
            {
                Directory.CreateDirectory(newFolderPath);
                response = System.Text.Encoding.UTF8.GetBytes($"\n\t Folder with name {folderName} is created!");
            }
            else
                response = System.Text.Encoding.UTF8.GetBytes($"\n\t Directory with name {folderName} already exists");

            connectedSocket.Send(response);
        }

        private static void displayFileContent(Socket connectedSocket, string fileName, string filesPath)
        {
            var filePath = Path.Combine(filesPath, fileName);
            var response = System.Text.Encoding.UTF8.GetBytes($"");

            if (File.Exists(filePath))
            {
                var content = string.Join('\n', File.ReadAllLines(filePath));
                if (content == string.Empty)
                    content = "File is empty!";

                response = System.Text.Encoding.UTF8.GetBytes($"\t{content}");
            }
            else
                response = System.Text.Encoding.UTF8.GetBytes($"\n\t File with name {fileName} doesn't exists");

            connectedSocket.Send(response);
        }

        private static void writeContentToFile(Socket connectedSocket, string content, string fileName, string filesPath)
        {
            var filePath = Path.Combine(filesPath, fileName);
            var response = System.Text.Encoding.UTF8.GetBytes($"");

            if (File.Exists(filePath))
            {
                using (StreamWriter stream = File.AppendText(filePath))
                {
                    stream.WriteLine(content);
                }

                response = System.Text.Encoding.UTF8.GetBytes($"\tContent is appended to the file {fileName}");
            }
            else
                response = System.Text.Encoding.UTF8.GetBytes($"\n\t File with name {fileName} doesn't exists");

            connectedSocket.Send(response);
        }

        private static void removeFolder(Socket connectedSocket, string folderName, string parentFolderPath)
        {
            var folderPath = Path.Combine(parentFolderPath, folderName);
            var response = System.Text.Encoding.UTF8.GetBytes($"");

            if (Directory.Exists(folderPath))
            {
                Directory.Delete(folderPath, true);

                response = System.Text.Encoding.UTF8.GetBytes($"\tDirectory {folderPath} is deleted!");
            }
            else
                response = System.Text.Encoding.UTF8.GetBytes($"\n\t Directory with name {folderName} doesn't exists");

            connectedSocket.Send(response);
        }

        private static void removeFile(Socket connectedSocket, string fileName, string parentFolderPath)
        {
            var filePath = Path.Combine(parentFolderPath, fileName);
            var response = System.Text.Encoding.UTF8.GetBytes($"");

            if (File.Exists(filePath))
            {
                File.Delete(filePath);

                response = System.Text.Encoding.UTF8.GetBytes($"\nFile {fileName} is deleted!");
            }
            else
                response = System.Text.Encoding.UTF8.GetBytes($"\n\t File with name {filePath} doesn't exists");

            connectedSocket.Send(response);
        }

        private static void navigateToFolder(Socket connectedSocket, ref string currentFolderPath, string rootFolderPath, string navigateToFolderName = null)
        {
            var response = System.Text.Encoding.UTF8.GetBytes($"");

            if (navigateToFolderName == Constants.NavigateBack)
            {
                if (currentFolderPath == rootFolderPath)
                    response = System.Text.Encoding.UTF8.GetBytes($"\nCan't navigate to top level from root folder!");
                else
                {
                    currentFolderPath = Path.GetDirectoryName(currentFolderPath);
                    response = System.Text.Encoding.UTF8.GetBytes($"\n{Constants.RootFolder + currentFolderPath.Split(rootFolderPath).Last()}");
                }
            } 
            else
            {
                var folderToNavigatePath= Path.Combine(currentFolderPath, navigateToFolderName);

                if (Directory.Exists(folderToNavigatePath))
                {
                    currentFolderPath = folderToNavigatePath;
                    response = System.Text.Encoding.UTF8.GetBytes($"\n{Constants.RootFolder + folderToNavigatePath.Split(rootFolderPath).Last()}");
                }
                else
                    response = System.Text.Encoding.UTF8.GetBytes($"\nThe folder {navigateToFolderName} doesn't exist!");
            }

            connectedSocket.Send(response);

        }
    }
}
