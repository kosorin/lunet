using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;

namespace Pegi.Client.Gui
{
    public static class ConsoleColorExtensions
    {
        private static Dictionary<ConsoleColor, Color> _colors = new Dictionary<ConsoleColor, Color>
        {
            [ConsoleColor.Black] = ParseHex("000000"),
            [ConsoleColor.DarkBlue] = ParseHex("000080"),
            [ConsoleColor.DarkGreen] = ParseHex("008000"),
            [ConsoleColor.DarkCyan] = ParseHex("008080"),
            [ConsoleColor.DarkRed] = ParseHex("800000"),
            [ConsoleColor.DarkMagenta] = ParseHex("800080"),
            [ConsoleColor.DarkYellow] = ParseHex("808000"),
            [ConsoleColor.Gray] = ParseHex("C0C0C0"),
            [ConsoleColor.DarkGray] = ParseHex("808080"),
            [ConsoleColor.Blue] = ParseHex("0000FF"),
            [ConsoleColor.Green] = ParseHex("00FF00"),
            [ConsoleColor.Cyan] = ParseHex("00FFFF"),
            [ConsoleColor.Red] = ParseHex("FF0000"),
            [ConsoleColor.Magenta] = ParseHex("FF00FF"),
            [ConsoleColor.Yellow] = ParseHex("FFFF00"),
            [ConsoleColor.White] = ParseHex("FFFFFF"),
        };

        public static Color ToColor(this ConsoleColor color)
        {
            return _colors[color];
        }

        public static Color ParseHex(string hex)
        {
            int value = Convert.ToInt32(hex, 16);

            var r = (byte)((value >> 16) & 0xFF);
            var g = (byte)((value >> 8) & 0xFF);
            var b = (byte)((value >> 0) & 0xFF);

            return Color.FromRgb(r, g, b);
        }
    }
}