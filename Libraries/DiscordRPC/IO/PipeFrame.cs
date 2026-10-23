using System;
using System.IO;
using System.Text;
using Valve.Newtonsoft.Json;

namespace Qubit.Libraries.DiscordRPC.IO
{
    public struct PipeFrame : IEquatable<PipeFrame>
    {
        public static readonly int MAX_SIZE = 16 * 1024;

        public Opcode Opcode { get; set; }

        public uint Length { get { return (uint)Data.Length; } }

        public byte[] Data { get; set; }

        public string Message
        {
            get { return GetMessage(); }
            set { SetMessage(value); }
        }

        public PipeFrame(Opcode opcode, object data)
        {
            Opcode = opcode;
            Data = null;

            SetObject(data);
        }

        public Encoding MessageEncoding { get { return Encoding.UTF8; } }

        private void SetMessage(string str) { Data = MessageEncoding.GetBytes(str); }

        private string GetMessage() { return MessageEncoding.GetString(Data); }

        public void SetObject(object obj)
        {
            string json = JsonConvert.SerializeObject(obj);
            SetMessage(json);
        }

        public void SetObject(Opcode opcode, object obj)
        {
            Opcode = opcode;
            SetObject(obj);
        }

        public T GetObject<T>()
        {
            string json = GetMessage();
            return JsonConvert.DeserializeObject<T>(json);
        }

        public bool ReadStream(Stream stream)
        {
            uint op;
            if (!TryReadUInt32(stream, out op))
                return false;

            uint len;
            if (!TryReadUInt32(stream, out len))
                return false;

            uint readsRemaining = len;

            using (var mem = new MemoryStream())
            {
                uint chunkSize = (uint)Min(2048, len);
                byte[] buffer = new byte[chunkSize];
                int bytesRead;
                while ((bytesRead = stream.Read(buffer, 0, Min(buffer.Length, readsRemaining))) > 0)
                {
                    readsRemaining -= chunkSize;
                    mem.Write(buffer, 0, bytesRead);
                }

                byte[] result = mem.ToArray();
                if (result.LongLength != len)
                    return false;

                Opcode = (Opcode)op;
                Data = result;
                return true;
            }
        }

        private int Min(int a, uint b)
        {
            return b >= a ? a : (int)b;
        }

        private bool TryReadUInt32(Stream stream, out uint value)
        {
            byte[] bytes = new byte[4];
            int cnt = stream.Read(bytes, 0, bytes.Length);

            if (cnt != 4)
            {
                value = default(uint);
                return false;
            }

            value = BitConverter.ToUInt32(bytes, 0);
            return true;
        }

        public void WriteStream(Stream stream)
        {
            byte[] op = BitConverter.GetBytes((uint)Opcode);
            byte[] len = BitConverter.GetBytes(Length);

            byte[] buff = new byte[op.Length + len.Length + Data.Length];
            op.CopyTo(buff, 0);
            len.CopyTo(buff, op.Length);
            Data.CopyTo(buff, op.Length + len.Length);

            stream.Write(buff, 0, buff.Length);
        }

        public bool Equals(PipeFrame other)
        {
            return Opcode == other.Opcode &&
                    Length == other.Length &&
                    Data == other.Data;
        }
    }
}
