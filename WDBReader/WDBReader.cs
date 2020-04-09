using System;
using System.Collections.Generic;
using System.Text;
using System.IO;

namespace WDBReader
{
    public class WDBReader : BinaryReader
    {
        #region Bit Variables
        byte bitPosition = 8;
        byte bitValue = 0;
        #endregion

        public WDBReader(Stream stream) : base(stream) { }

        public byte ReadBit()
        {
            if (bitPosition == 8)
            {
                bitValue = ReadByte();
                bitPosition = 0;
            }

            int returnValue = bitValue;
            bitValue = (byte)(2 * returnValue);
            ++bitPosition;

            return (byte)(returnValue >> 7);
        }

        public bool HasBit()
        {
            if (bitPosition == 8)
            {
                bitValue = ReadByte();
                bitPosition = 0;
            }

            int returnValue = bitValue;
            bitValue = (byte)(2 * returnValue);
            ++bitPosition;

            return Convert.ToBoolean(returnValue >> 7);
        }

        public T ReadBits<T>(int bitCount)
        {
            int value = 0;

            for (var i = bitCount - 1; i >= 0; --i)
                if (HasBit())
                    value |= (1 << i);

            return (T)Convert.ChangeType(value, typeof(T));
        }

        public void ResetBitReading()
        {
            if (bitPosition > 7)
                return;

            bitPosition = 8;
            bitValue = 0;
        }

        public string ReadString(int length)
        {
            if (length == 0)
                return "";

            return Encoding.UTF8.GetString(ReadBytes(length));
        }

        public string ReadCString()
        {
            StringBuilder tmpString = new StringBuilder();
            char tmpChar = ReadChar();
            char tmpEndChar = Convert.ToChar(Encoding.UTF8.GetString(new byte[] { 0 }));

            while (tmpChar != tmpEndChar)
            {
                tmpString.Append(tmpChar);
                tmpChar = ReadChar();
            }

            return tmpString.ToString();
        }
    }
}
