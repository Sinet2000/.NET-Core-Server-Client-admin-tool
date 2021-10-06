using System;
using System.Collections.Generic;
using System.Text;

namespace ServerSide
{
    public static class Constants
    {
        public const string FolderPrefix = "-f";
        public const int WriteInFileCommandPartsCount = 4;
        public const int GetFileContentAndCreateFileCommandPartsCount = 2;
        public const int RemoveFolderCommandPartsCount = 3;
        public const int RemoveFileCommandPartsCount = 2;
        public const string RootFolderName = "Files";
        public const string NavigateBack = "..";
    }
}
