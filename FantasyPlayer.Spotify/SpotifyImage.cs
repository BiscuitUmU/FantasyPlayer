using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Net;
using SpotifyAPI.Web;

namespace FantasyPlayer.Spotify
{
    public class SpotifyImage
    {
        public SpotifyImage(string url, int width, int height, string id)
        {
            using var client = new WebClient();
            using Stream imageStream = new MemoryStream(client.DownloadData(url));
            var image = System.Drawing.Image.FromStream(imageStream);
            var mainBitmap = new Bitmap(image);
            var imageBitmap = new Bitmap(mainBitmap, width, height);
            var imageToByte = ImageToByte(imageBitmap);

            var imageName = $"{id}.png";

            if (!Directory.Exists(GetFolderPath()))
                Directory.CreateDirectory(GetFolderPath());
                    
            if (!File.Exists(GetFolderPath(imageName)))
                File.WriteAllBytes(GetFolderPath(imageName), imageToByte);
        }

        private static byte[] ImageToByte(System.Drawing.Image img)
        {
            var converter = new ImageConverter();
            return (byte[]) converter.ConvertTo(img, typeof(byte[]));
        }

        public static string GetFolderPath(string fileName = null)
        {
            var path =  Path.Combine(new string[]
            {
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "XIVLauncher",
                "pluginConfigs",
                "FantasyPlayer.Dalamud",
                "AlbumImages"
            });

            return fileName == null ? path : Path.Combine(path, fileName);
        }
    }
}