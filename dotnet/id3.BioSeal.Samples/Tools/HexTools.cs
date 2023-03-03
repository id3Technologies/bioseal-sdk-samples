using System;
using System.Linq;

namespace id3.BioSeal.Samples
{
    /// <summary>
    /// This class contains methods to format string in hexadecimal format.
    /// </summary>
    public static class HexTools
	{
		/// <summary>
		/// Gets the bytes from an hexadecimal string.
		/// </summary>
		/// <param name="hex"></param>
		/// <returns>Data byte array</returns>
		public static byte[] GetBytes(string hex)
		{
			string text = hex.Replace(" ", "").Replace("\r", "").Replace("\n", "").Replace("\\", "");
			return Enumerable.Range(0, text.Length)
							 .Where(x => x % 2 == 0)
							 .Select(x => Convert.ToByte(text.Substring(x, 2), 16))
							 .ToArray();
		}

		/// <summary>
		/// Converts a short value to an hexadecimal string.
		/// </summary>
		/// <param name="value"></param>
		/// <returns>Converted string</returns>
		public static string GetString(ushort value)
		{
			byte SW1 = (byte)(((short)value) >> 8);
			byte SW2 = (byte)((short)value);

			return BitConverter.ToString(new byte[] { SW1, SW2 }).Replace("-", "");
		}

        /// <summary>
        /// Converts a short value to an hexadecimal string.
        /// </summary>
        /// <param name="value">Integer (max. 256^3)</param>
        /// <returns></returns>
        public static string GetString(int value)
        {
            byte swId1 = (byte)(((short)value) >> 16);
            byte swId2 = (byte)(((short)value) >> 8);
            byte swVersion = (byte)((short)value);

            return BitConverter.ToString(new byte[] { swId1, swId2, swVersion }).Replace("-", " ");
        }

        /// <summary>
        /// Converts a byte value to an hexadecimal string.
        /// </summary>
        /// <param name="value"></param>
        /// <returns>Converted string</returns>
        public static string GetString(byte value)
		{
			return BitConverter.ToString(new byte[] { value }).Replace("-", " ");
		}

        /// <summary>
        /// Converts a byte array to an hexadecimal string.
        /// </summary>
        /// <param name="value"></param>
        /// <returns>Converted string</returns>
        public static string GetString(byte[] data)
		{
			return BitConverter.ToString(data).Replace("-", "");
		}

		/// <summary>
		/// Formats the data buffer to an hexadecimal string.
		/// </summary>
		/// <param name="data"></param>
		/// <param name="offset"></param>
		/// <param name="size"></param>
		/// <returns>Formatted string</returns>
		public static string FormatData(byte[] data, int offset, int size, int tabsize)
		{
			string message = string.Empty;

			if (data != null && size > 0)
			{
                //const int step = 16;
				//for (int i = 0; i < size; i += step)
				//{
				//	if (i != 0)
				//		message += @" \" + Environment.NewLine + new string(' ', tabsize);// "                     ";
				//	message += BitConverter.ToString(data, i + offset, Math.Min(step, size - i)).Replace("-", " ");
				//}
                message = BitConverter.ToString(data, offset, size).Replace("-", " ");
            }
			//else
			//	message += "none";// Environment.NewLine;

			return message;
		}

		/// <summary>
		/// Formats the data buffer to an hexadecimal string.
		/// </summary>
		/// <param name="data"></param>
		/// <param name="offset"></param>
		/// <param name="size"></param>
		/// <returns>Formatted data</returns>
		public static string FormatData(byte[] data, int offset, int size)
		{
			return FormatData(data, offset, size, 21);
		}

        /// <summary>
        /// Formats the data buffer to an hexadecimal string.
        /// </summary>
        /// <param name="data"></param>
        /// <returns>Formatted data</returns>
        public static string FormatData(byte[] data)
		{
			return FormatData(data, 0, data?.Length ?? 0);
		}

        /// <summary>
        /// Formats a short value to an hexadecimal string.
        /// </summary>
        /// <param name="value"></param>
        /// <returns>Formatted data</returns>
        public static string FormatData(ushort value)
		{
			byte[] data = new byte[]
			{
				(byte)(value >> 8),
				(byte)(value & 0xFF)
			};
			return FormatData(data);
		}
	}
}
