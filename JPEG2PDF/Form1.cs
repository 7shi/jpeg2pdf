using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace JPEG2PDF
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            if (saveFileDialog1.ShowDialog(this) == DialogResult.OK)
            {
                var pdf = saveFileDialog1.FileName;
                Test(pdf);
                Process.Start(pdf);
            }
        }

        private void Test(string pdf)
        {
            using (var fs = new FileStream(pdf, FileMode.Create))
            using (var sw = new StreamWriter(fs) { AutoFlush = true })
            {
                var objp = new List<long>();

                sw.WriteLine("%PDF-1.2");

                sw.WriteLine();
                objp.Add(fs.Position);
                sw.WriteLine("1 0 obj");
                sw.WriteLine("<< /Type /Catalog /Pages 2 0 R >>");
                sw.WriteLine("endobj");

                sw.WriteLine();
                objp.Add(fs.Position);
                sw.WriteLine("2 0 obj");
                sw.WriteLine("<< /Type /Pages /Kids [ 3 0 R ] /Count 1 /MediaBox [ 0 0 595 842] >>");
                sw.WriteLine("endobj");

                sw.WriteLine();
                objp.Add(fs.Position);
                sw.WriteLine("3 0 obj");
                sw.WriteLine("<< /Type /Page /Parent 2 0 R /Contents 4 0 R >>");
                sw.WriteLine("endobj");

                var s2 = new StringWriter();
                s2.WriteLine("q");
                s2.WriteLine("200 0 0 283 0 0 cm");
                //s2.WriteLine("/ObjX Do");
                s2.WriteLine("Q");
                s2.Close();
                var str2 = s2.ToString();

                sw.WriteLine();
                objp.Add(fs.Position);
                sw.WriteLine("4 0 obj");
                sw.WriteLine("<< /Length {0} >>", str2.Length);
                sw.WriteLine("stream");
                sw.Write(str2);
                sw.WriteLine("endstream");
                sw.WriteLine("endobj");

                sw.WriteLine();
                var xref = fs.Position;
                sw.WriteLine("xref");
                var size = objp.Count + 1;
                sw.WriteLine("0 {0}", size);
                sw.WriteLine("{0:0000000000} {1:00000} f", 0, 65535);
                foreach (var p in objp)
                    sw.WriteLine("{0:0000000000} {1:00000} n", p, 0);
                sw.WriteLine("trailer");
                sw.WriteLine("<< /Root 1 0 R /Size {0} >>", size);
                sw.WriteLine("startxref");
                sw.WriteLine("{0}", xref);
                sw.WriteLine("%%EOF");
            }
        }
    }
}
