using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;

namespace ServerSide
{
    public static class ServerCommandsHelper
    {
        public static string getCommandsListAndDescription()
        {
            return "\tquit - exit program\n" +
                "\tls - get files list of server's folder\n" +
                "\ttouch [filename] - create txt file on server's folder with specified name\n" +
                "\tmkdir [folderName] - create folder in server's folder\n" +
                "\tcat [filename] - display content of file\n" +
                "\techo [\"text\"] >> [filename] - write content to file\n\t\tExample: echo \"asddas\" >> test.txt\n\n" +
                "\tcd [foldername] - go to folder\n" +
                "\tcd .. - navigate to parent folder\n" +
                "\trm [filename]- remove file\n" +
                $"\trm {Constants.FolderPrefix} [folder name]- remove folder\n" +
                "\twput [\"file path\"] upload file to the server folder";
        }

        public static string getInitialMessage()
        {
            return "\t Succesfully connected to the server!" +
                "\n\t To get list of commands, please type: help";
        }
    }

    public enum ServerCommands
    {
        [Description("quit")]
        Quit,

        [Description("ls")]
        GetFilesList,

        [Description("touch")]
        CreateFile,

        [Description("cat")]
        GetFileContent,

        [Description("help")]
        Help,

        [Description("echo")]
        WriteInFile,

        [Description("rm")]
        Remove,

        [Description("wput")]
        UploadFile, 

        [Description("cd")]
        Navigate,

        [Description("mkdir")]
        CreateFolder
    }
}
