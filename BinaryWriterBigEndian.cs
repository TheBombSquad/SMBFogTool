using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SMBFogTool
{
    public class BinaryWriterBigEndian : BinaryWriter
    {
        public BinaryWriterBigEndian(Stream stream) : base(stream) { }

        public override void Write(byte[] buffer)
        {
            Array.Reverse(buffer);
            base.Write(buffer);
        }

        public override void Write(uint value)
        {
            Write(BitConverter.GetBytes(value));
        }

        public override void Write(float value)
        {
            Write(BitConverter.GetBytes(value));
        }

    }
}
