using System;
using System.Collections.Specialized;
using System.IO;
using System.Net;
using System.Text;
using Stride.Graphics;
using System.Security.Cryptography;

namespace VL.Stride.Windows
{
    public static class UploadScreenshot
    {
        private static Uri CPictureUploadUri = new Uri("https://vvvv.org/web-api/picture-upload");

        private static MD5CryptoServiceProvider FMd5 = new MD5CryptoServiceProvider();

        public static string ToMD5(string input)
        {
            byte[] strarray = Encoding.Default.GetBytes(input);

            //Compute Hash
            byte[] hash = FMd5.ComputeHash(strarray);

            StringBuilder sb = new StringBuilder();
            foreach (byte hex in hash)
            {
                sb.Append(hex.ToString("x2"));
            }

            return sb.ToString();
        }

        public static string UploadTextureToVVVVOrg(Texture input, CommandList commandList, string title = "VL.Stride", string description = "I am from VL.Stride", string username = "guest", string password = "guest")
        {
            var message = "";

            MemoryStream fileData = new MemoryStream();
            input.Save(commandList, fileData, ImageFileType.Png);
            fileData.Seek(0, SeekOrigin.Begin);

            //add post-header
            NameValueCollection nvc = new NameValueCollection();
            nvc.Add("name", username);
            nvc.Add("pass", ToMD5(password));
            nvc.Add("header", "false");
            nvc.Add("title", title);
            nvc.Add("description", description);

            using (WebResponse response = Upload.PostFile(CPictureUploadUri, nvc, fileData, title + ".png", null, null, null, null))
            {
                // the stream returned by WebResponse.GetResponseStream
                // will contain any content returned by the server after upload

                using (StreamReader reader = new StreamReader(response.GetResponseStream()))
                {
                    string result = reader.ReadToEnd();
                    if (result.Contains("OK"))
                        message = "Upload Successful.";
                    else if (result.Contains("LOGIN FAILED"))
                        message = "Login failed!";
                    else if (result.Contains("SERVER BUSY"))
                        message = "Server is busy, please try again later.";
                    else
                        message = "ERROR: " + result;
                }
            }

            return message;
        }
    }
}
