//Written for Islet Online. https://store.steampowered.com/app/428180/
using System.IO;
using System.IO.Compression;

namespace Islet_Online_Unpacker
{
    static class Program
    {
        static void Main(string[] args)
        {
            using FileStream input = File.OpenRead(args[0]);
            BinaryReader br = new(input);
            Directory.CreateDirectory(Path.GetDirectoryName(input.Name) + "//" + Path.GetFileNameWithoutExtension(input.Name));

            while (br.BaseStream.Position < br.BaseStream.Length)
            {
                int size = br.ReadInt32();
                if (size == 0x100)
                {
                    br.BaseStream.Position -= 4;
                    break;
                };
                br.BaseStream.Position += size;
            }

            System.Collections.Generic.List<Subfile> subfiles = new();
            while (br.BaseStream.Position < br.BaseStream.Length - 0x28)
            {
                Subfile subfile = new()
                {
                    isCompressed = br.ReadInt32(),
                    sizeUncompressed = br.ReadInt32(),
                    sizeCompressed = br.ReadInt32(),
                    start = br.ReadInt32()
                };
                br.ReadBytes(20);
                subfile.name = new(br.ReadChars(br.ReadInt16()));
                subfiles.Add(subfile);
            }

            foreach (Subfile sub in subfiles)
            {
                br.BaseStream.Position = sub.start;
                int size = br.ReadInt32();
                if ((sub.isCompressed == 0 && size != sub.sizeUncompressed) || (sub.isCompressed == 0x100 && size != sub.sizeCompressed))
                    throw new System.Exception("Fuck.");

                if (sub.name.Contains(@"\"))
                    Directory.CreateDirectory(Path.GetDirectoryName(input.Name) + "//" + Path.GetDirectoryName(sub.name));

                using FileStream FS = File.Create(Path.GetDirectoryName(input.Name) + "//" + sub.name);
                BinaryWriter bw = new(FS);
                if (sub.isCompressed == 0x100)
                {
                    MemoryStream ms = new();
                    br.ReadInt16();
                    using (var ds = new DeflateStream(new MemoryStream(br.ReadBytes(size)), CompressionMode.Decompress))
                        ds.CopyTo(FS);
                    continue;
                }

                bw.Write(br.ReadBytes(size));
                bw.Close();
            }
        }

        struct Subfile
        {
            public int isCompressed;
            public int sizeUncompressed;
            public int sizeCompressed;
            public int start;
            public string name;
        }
    }
}
