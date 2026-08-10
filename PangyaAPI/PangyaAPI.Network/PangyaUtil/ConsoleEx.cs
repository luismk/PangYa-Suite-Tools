using System;

namespace PangyaAPI.Network.PangyaUtil
{
    public class ConsoleEx
    {
        public static void SetColor(int color)
        {
            ConsoleColor fg = ConsoleColor.White;
            ConsoleColor bg = ConsoleColor.Black;

            switch (color)
            {
                case 0: fg = ConsoleColor.White; bg = ConsoleColor.Black; break;
                case 1: fg = ConsoleColor.Red; break;
                case 2: fg = ConsoleColor.Green; break;
                case 3: fg = ConsoleColor.Yellow; break;
                case 4: fg = ConsoleColor.Blue; break;
                case 5: fg = ConsoleColor.Magenta; break;
                case 6: fg = ConsoleColor.Cyan; break;
                case 7: fg = ConsoleColor.Black; bg = ConsoleColor.Gray; break;
                case 8: fg = ConsoleColor.Black; bg = ConsoleColor.White; break;
                case 9: fg = ConsoleColor.Red; bg = ConsoleColor.White; break;
                case 10: fg = ConsoleColor.Green; bg = ConsoleColor.White; break;
                case 11: fg = ConsoleColor.Yellow; bg = ConsoleColor.White; break;
                case 12: fg = ConsoleColor.Blue; bg = ConsoleColor.White; break;
                case 13: fg = ConsoleColor.Magenta; bg = ConsoleColor.White; break;
                case 14: fg = ConsoleColor.Cyan; bg = ConsoleColor.White; break;
                case 15: fg = ConsoleColor.White; bg = ConsoleColor.White; break;
            }

            Console.ForegroundColor = fg;
            Console.BackgroundColor = bg;
        }

        public static void ResetColor()
        {
            Console.ResetColor();
        }

        public static void BarSpace()
        {
            SetColor(1);
            Console.WriteLine(new string('*', 120));
            ResetColor();
        }

        public static void TextCentralize(string str)
        {
            SetColor(2);
            int width = 121;
            int pad = (width - str.Length) / 2;
            Console.WriteLine(new string(' ', pad) + str);
            ResetColor();
        }

        public static void Log()
        {
            Log("PangYa Server");
        }

        public static void Log(string serverRole)
        {
            BarSpace();
            TextCentralize(FormatServerRole(serverRole));
            TextCentralize("Credits: luismk");
            TextCentralize("https://github.com/Robert-LTH/PangYa-Server");
            BarSpace();
        }

        private static string FormatServerRole(string serverRole)
        {
            if (string.IsNullOrWhiteSpace(serverRole))
                return "PangYa Server";

            var value = serverRole.Trim();
            if (value.Contains(' '))
                return value;

            var formatted = new System.Text.StringBuilder(value.Length + 4);
            for (var index = 0; index < value.Length; index++)
            {
                if (index > 0 && char.IsUpper(value[index]) && char.IsLower(value[index - 1]))
                    formatted.Append(' ');
                formatted.Append(value[index]);
            }
            return formatted.ToString();
        }

        public static void InfoServer(string info)
        {
            string timestamp = DateTime.Now.ToString("yyyy-MM-dd.HH:mm:ss");

            SetColor(0);
            Console.Write($"[{timestamp}] ");
            SetColor(3);
            Console.Write("[INFO] ");
            ResetColor();
            Console.WriteLine(info);
        }

        public static void SetTitle(string title)
        {
            Console.Title = title;
        }
    }
}
