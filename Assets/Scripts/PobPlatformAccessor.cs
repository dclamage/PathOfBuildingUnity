using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using MoonSharp.Interpreter;
using MoonSharp.Interpreter.Platforms;

namespace PathOfBuilding
{
    public class PobPlatformAccessor : StandardPlatformAccessor
    {
        public PobPlatformAccessor(string basePath)
        {
            if (Path.DirectorySeparatorChar != '/')
            {
                basePath = basePath.Replace('/', Path.DirectorySeparatorChar);
            }
            if (Path.DirectorySeparatorChar != '\\')
            {
                basePath = basePath.Replace('\\', Path.DirectorySeparatorChar);
            }
            if (!basePath.EndsWith(Path.DirectorySeparatorChar.ToString()))
            {
                basePath += Path.DirectorySeparatorChar;
            }
            this.basePath = basePath;
        }

        public override Stream IO_OpenFile(Script script, string filename, Encoding encoding, string mode)
        {
            filename = basePath + filename;
            return new FileStream(filename, ParseFileMode(mode), ParseFileAccess(mode), FileShare.ReadWrite | FileShare.Delete);
        }

        private string basePath;
    }
}
