using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SMBFogTool
{
    public class BinaryReaderBigEndian : BinaryReader
    {
        public BinaryReaderBigEndian(Stream stream) : base(stream) { }
        public override uint ReadUInt32()
        {
            var data = ReadBytes(4);
            return BitConverter.ToUInt32(data, 0);
        }

        public override float ReadSingle()
        {
            var data = ReadBytes(4);
            return BitConverter.ToSingle(data, 0);
        }

        public override byte[] ReadBytes(int count)
        {
            byte[] data = new byte[count];
            for (int i = 0; i < count; i++)
            {
                data[i] = base.ReadByte();
            }
            Array.Reverse(data);
            return data;

        }
    }
}
