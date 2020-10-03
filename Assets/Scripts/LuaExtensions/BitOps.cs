using System;
using System.Collections.Generic;
using MoonSharp.Interpreter;

namespace LuaExtensions
{
    [MoonSharpUserData(AccessMode = InteropAccessMode.Preoptimized)]
    public class BitOps
    {
        private static uint Bit(double val)
        {
            if (val < 0)
            {
                return ~(uint)Math.Floor(-val) + 1;
            }
            return (uint)Math.Floor(val);
        }

        public static double tobit(double val)
        {
            return Bit(val);
        }

        public static double bnot(double val)
        {
            return ~Bit(val);
        }

        private static double BitOp(double[] vals, Func<uint, uint, uint> op)
        {
            if (vals.Length == 0)
            {
                return 0;
            }
            uint res = Bit(vals[0]);
            for (int i = 1; i < vals.Length; i++)
            {
                res = op.Invoke(res, Bit(vals[i]));
            }
            return res;
        }

        public static double band(params double[] vals)
        {
            return BitOp(vals, (uint a, uint b) => a & b);
        }

        public static double bor(params double[] vals)
        {
            return BitOp(vals, (uint a, uint b) => a | b);
        }

        public static double bxor(params double[] vals)
        {
            return BitOp(vals, (uint a, uint b) => a ^ b);
        }

        public static double lshift(double a, double b)
        {
            return Bit(a) << (int)(Bit(b) & 31);
        }

        public static double rshift(double a, double b)
        {
            return Bit(a) >> (int)(Bit(b) & 31);
        }

        public static double arshift(double a, double b)
        {
            return (int)a >> (int)(Bit(b) & 31);
        }

        public static double rol(double a, double b)
        {
            uint ba = Bit(a);
            int bb = (int)(Bit(b) & 31);
            return (ba << bb) | (ba >> (32 - bb));
        }

        public static double ror(double a, double b)
        {
            uint ba = Bit(a);
            int bb = (int)(Bit(b) & 31);
            return (ba << (32 - bb)) | (ba >> bb);
        }

        public static double bswap(double val)
        {
            uint b = Bit(val);
            return (b >> 24) | ((b >> 8) & 0xff00) | ((b & 0xff00) << 8) | (b << 24);
        }

        public static string tohex(List<double> vals)
        {
            uint b = vals.Count == 0 ? 0 : Bit(vals[0]);
            int n = vals.Count <= 1 ? 8 : (int)vals[1];
            string hexdigits = "0123456789abcdef";
            if (n < 0)
            {
                n = -n;
                hexdigits = "0123456789ABCDEF";
            }
            if (n > 8)
            {
                n = 8;
            }
            char[] buf = new char[n];
            for (int i = n; --i >= 0;)
            {
                buf[i] = hexdigits[(int)(b & 15)];
                b >>= 4;
            }
            return new string(buf);
        }
    }
}
