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
#if DEBUG
            folderBrowserDialog1.SelectedPath = @"E:\Temp";
#endif
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
            var path = textBox1.Text;
            if (path == "") return;

            var jpgs = Directory.GetFiles(path, "*.jpg");
            if (jpgs.Length == 0) return;

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
                sw.WriteLine("<< /Type /Pages /Count 1 /Kids [ 3 0 R ] >>");
                sw.WriteLine("endobj");

                var sz = GetJpegSize(jpgs[0]);
                sw.WriteLine();
                objp.Add(fs.Position);
                sw.WriteLine("3 0 obj");
                sw.WriteLine("<<");
                sw.WriteLine("  /Type /Page /Parent 2 0 R /Contents 4 0 R");
                sw.WriteLine("  /MediaBox [ 0 0 {0} {1} ]", sz.Width, sz.Height);
                sw.WriteLine("  /Resources");
                sw.WriteLine("  <<");
                sw.WriteLine("    /ProcSet [ /PDF /ImageB /ImageC /ImageI ]");
                sw.WriteLine("    /XObject << /Obj5 5 0 R >>");
                sw.WriteLine("  >>");
                sw.WriteLine(">>");
                sw.WriteLine("endobj");

                sw.WriteLine();
                objp.Add(fs.Position);
                sw.WriteLine("4 0 obj");
                var st4 = string.Format("q {0} 0 0 {1} 0 0 cm /Obj5 Do Q", sz.Width, sz.Height);
                sw.WriteLine("<< /Length {0} >>", st4.Length);
                sw.WriteLine("stream");
                sw.WriteLine(st4);
                sw.WriteLine("endstream");
                sw.WriteLine("endobj");

                sw.WriteLine();
                var buf = File.ReadAllBytes(jpgs[0]);
                objp.Add(fs.Position);
                sw.WriteLine("5 0 obj");
                sw.WriteLine("<<");
                sw.WriteLine("  /Type /XObject /Subtype /Image /Filter /DCTDecode");
                sw.WriteLine("  /BitsPerComponent 8 /ColorSpace /DeviceRGB");
                sw.WriteLine("  /Width {0} /Height {1} /Length {2}",
                    sz.Width, sz.Height, buf.Length);
                sw.WriteLine(">>");
                sw.WriteLine("stream");
                fs.Write(buf, 0, buf.Length);
                sw.WriteLine();
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

        private void button2_Click(object sender, EventArgs e)
        {
            if (folderBrowserDialog1.ShowDialog(this) == DialogResult.OK)
                textBox1.Text = folderBrowserDialog1.SelectedPath;
        }

        public static Size GetJpegSize(string jpg)
        {
            using (var fs = new FileStream(jpg, FileMode.Open))
            {
                try
                {
                    var buf = new byte[8];
                    while (fs.Read(buf, 0, 2) == 2 && buf[0] == 0xff)
                    {
                        if (buf[1] == 0xc0 && fs.Read(buf, 0, 7) == 7)
                            return new Size(buf[5] * 256 + buf[6], buf[3] * 256 + buf[4]);
                        else if (buf[1] != 0xd8)
                        {
                            if (fs.Read(buf, 0, 2) == 2)
                                fs.Position += buf[0] * 256 + buf[1] - 2;
                            else
                                break;
                        }
                    }
                }
                catch { }
            }
            return Size.Empty;
        }
    }
}
