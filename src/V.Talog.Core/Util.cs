using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;

namespace V.Talog
{
    internal static class Util
    {
        public static string Md5(this string input)
        {
            if (string.IsNullOrWhiteSpace(input))
            {
                return input;
            }

            using (var md5Hash = MD5.Create())
            {
                // Convert the input string to a byte array and compute the hash.
                var data = md5Hash.ComputeHash(Encoding.UTF8.GetBytes(input));

                // Create a new Stringbuilder to collect the bytes
                // and create a string.
                var sBuilder = new StringBuilder();

                // Loop through each byte of the hashed data 
                // and format each one as a hexadecimal string.
                for (int i = 0; i < data.Length; i++)
                {
                    sBuilder.Append(data[i].ToString("x2"));
                }

                // Return the hexadecimal string.
                return sBuilder.ToString();
            }
        }
    }
}
