using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace JPEG2PDF
{
    public class NumberStringComparer : IComparer<string>
    {
        public int Compare(string x, string y)
        {
            string[] xs = Split(x), ys = Split(y);
            int len = Math.Min(xs.Length, ys.Length);
            for (int i = 0; i < len; i++)
            {
                string xx = xs[i], yy = ys[i];
                if (xx.Length < yy.Length) return -1;
                if (xx.Length > yy.Length) return 1;
                int cmp = xx.CompareTo(yy);
                if (cmp != 0) return cmp;
            }
            if (xs.Length < ys.Length) return -1;
            if (xs.Length > ys.Length) return 1;
            return 0;
        }

        public static string[] Split(string s)
        {
            var ret = new List<string>();
            var num = new StringBuilder();
            foreach (char ch in s)
            {
                if ('0' <= ch && ch <= '9')
                    num.Append(ch);
                else if ('０' <= ch && ch <= '９')
                    num.Append((char)('0' + (ch - '０')));
                else
                {
                    if (num.Length > 0)
                    {
                        ret.Add(num.ToString());
                        num.Length = 0;
                    }
                    ret.Add(ch.ToString());
                }
            }
            if (num.Length > 0) ret.Add(num.ToString());
            return ret.ToArray();
        }
    }
}
