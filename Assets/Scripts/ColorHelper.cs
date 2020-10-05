using System;
using UnityEngine;

namespace PathOfBuilding
{
    static class ColorHelper
    {
        private static bool IsHexDigit(char c)
        {
            return (c >= '0' && c <= '9') ||
                    (c >= 'a' && c <= 'f') ||
                    (c >= 'A' && c <= 'F');
        }

        private static Color[] colorEscape = new Color[]
        {
            new Color(0.0f, 0.0f, 0.0f), // ^0 Black
            new Color(1.0f, 0.0f, 0.0f), // ^1 Red
            new Color(0.0f, 1.0f, 0.0f), // ^2 Green
            new Color(0.0f, 0.0f, 1.0f), // ^3 Blue
            new Color(1.0f, 1.0f, 0.0f), // ^4 Yellow
            new Color(1.0f, 0.0f, 1.0f), // ^5 Purple
            new Color(0.0f, 1.0f, 1.0f), // ^6 Aqua
            new Color(1.0f, 1.0f, 1.0f), // ^7 White
            new Color(0.7f, 0.7f, 0.7f), // ^8 Gray
            new Color(0.4f, 0.4f, 0.4f), // ^9 DarkGray
        };

        public static int IsColorEscape(string str)
        {
            if (str.Length < 2)
            {
                return 0;
            }
            if (str[0] != '^')
            {
                return 0;
            }
            if (char.IsDigit(str[1]))
            {
                return 2;
            }
            else if (str.Length >= 8 && (str[1] == 'x' || str[1] == 'X'))
            {
                for (int c = 0; c < 6; c++)
                {
                    if (!IsHexDigit(str[c + 2]))
                    {
                        return 0;
                    }
                }
                return 8;
            }
            return 0;
        }

        public static Color ReadColorEscape(string str)
        {
            Color outColor = Color.white;
            int len = IsColorEscape(str);
            switch (len)
            {
                case 2:
                    outColor = colorEscape[str[1] - '0'];
                    break;
                case 8:
                    outColor.r = Convert.ToInt32(str.Substring(2, 2), 16) / 255f;
                    outColor.g = Convert.ToInt32(str.Substring(4, 2), 16) / 255f;
                    outColor.b = Convert.ToInt32(str.Substring(6, 2), 16) / 255f;
                    break;
            }

            return outColor;
        }
    }
}
