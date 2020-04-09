using System.Text;

namespace RibbitServer
{
    public static class Extensions
    {
        public static string DeserializePacket(this byte[] data)
        {
            var stringBuilder = new StringBuilder();

            for (var i = 0; i < data.Length; i++)
            {
                var dataString = data[i].ToString("X");

                if (i == 16 || i == 32 || i == 48)
                    stringBuilder.Append("\n");

                if (dataString.Length == 1)
                    stringBuilder.Append("0");

                stringBuilder.Append(dataString + " ");
            }
            
            return stringBuilder.ToString();
        }
    }
}