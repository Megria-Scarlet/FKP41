using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;

namespace FKP41
{
    internal class DirectoryWatcher : IWatcher
    {
        private DirectoryInfo _directory;
        public string FilePath
        {
            [MethodImpl(MethodImplOptions.NoInlining)]
            get => _directory.FullName;
        }
        public DirectoryWatcher(string path)
        {
            _directory = new(path);
        }
    }
}
