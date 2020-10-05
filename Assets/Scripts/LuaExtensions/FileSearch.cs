using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using MoonSharp.Interpreter;

namespace PathOfBuilding
{
    [MoonSharpUserData(AccessMode = InteropAccessMode.Preoptimized)]
    class FileSearch
    {
        public static DynValue NewFileSearch(params DynValue[] values)
        {
            if (values.Length < 1)
            {
                Debug.LogError("Usage: NewFileSearch(spec[, findDirectories])");
                return DynValue.Nil;
            }

            if (values[0].Type != DataType.String)
            {
                Debug.LogError($"NewFileSearch() argument 1: expected string, got {values[0].Type}");
                return DynValue.Nil;
            }

            string searchPath = values[0].String;
            bool findDirectories = values.Length >= 2 && values[1].Boolean;

            string searchPattern = Path.GetFileName(searchPath);
            string directory = Path.GetDirectoryName(searchPath) + '\\';
            IEnumerator<string> entries = findDirectories ?
                Directory.EnumerateDirectories(directory, searchPattern).GetEnumerator() :
                Directory.EnumerateFiles(directory, searchPattern).GetEnumerator();
            if (entries.MoveNext())
            {
                return UserData.Create(new FileSearch(entries));
            }
            return DynValue.Nil;
        }

        private FileSearch(IEnumerator<string> entries)
        {
            this.entries = entries;
        }

        public bool NextFile()
        {
            return entries.MoveNext();
        }

        public string GetFileName()
        {
            return Path.GetFileName(entries.Current);
        }

        public int GetFileSize()
        {
            FileInfo fileInfo = new FileInfo(entries.Current);
            return (int)(fileInfo.Attributes.HasFlag(FileAttributes.Directory) ? 0 : fileInfo.Length);
        }

        public DynValue GetFileModifiedTime()
        {
            FileInfo fileInfo = new FileInfo(entries.Current);
            double modified = (double)(fileInfo.LastWriteTime.ToFileTimeUtc() / 10000000l);
            string modifiedDate = fileInfo.LastWriteTime.ToShortDateString();
            string modifiedTime = fileInfo.LastWriteTime.ToShortTimeString();
            return DynValue.NewTuple(
                DynValue.NewNumber(modified),
                DynValue.NewString(modifiedDate),
                DynValue.NewString(modifiedTime));
        }

        private readonly IEnumerator<string> entries;
    }
}
