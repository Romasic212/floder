using System.Collections.Generic;
using System.IO;
using floder.Models;

namespace floder.Core
{
    public class Indexer
    {
        public List<FileMeta> Scan(string folderPath)
        {
            var files = Directory.GetFiles(folderPath, "*", SearchOption.AllDirectories);
            var result = new List<FileMeta>();

            foreach (var file in files)
            {
                var info = new FileInfo(file);

                result.Add(new FileMeta
                {
                    Path = file,
                    Size = info.Length,
                    LastModified = info.LastWriteTimeUtc.Ticks
                });
            }

            return result;
        }
    }
}