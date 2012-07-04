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

        private class Args
        {
            public string pdf;
            public string[] jpgs;
        }

        private void button1_Click(object sender, EventArgs e)
        {
            if (backgroundWorker1.IsBusy)
                backgroundWorker1.CancelAsync();
            else
            {
                var path = textBox1.Text;
                if (path == "") return;

                var jpgs = new List<string>(Directory.GetFiles(path, "*.jpg"));
                if (jpgs.Count == 0) return;

                saveFileDialog1.FileName = Path.GetFileName(textBox1.Text) + ".pdf";
                if (saveFileDialog1.ShowDialog(this) == DialogResult.OK)
                {
                    jpgs.Sort(new NumberStringComparer());
                    button1.Text = "中止";
                    var args = new Args { pdf = saveFileDialog1.FileName, jpgs = jpgs.ToArray() };
                    backgroundWorker1.RunWorkerAsync(args);
                }
            }
        }

        private void MakePDF(string pdf, string[] jpgs)
        {
            var sizes = new Size[jpgs.Length];
            var bpps = new int[jpgs.Length];
            var devs = new[] { "", "DeviceGray", "", "DeviceRGB", "DeviceCMYK" };
            int prg = 0;
            for (int i = 0; i < jpgs.Length; i++)
            {
                if (backgroundWorker1.CancellationPending) return;
                int pp = i * 20 / jpgs.Length;
                if (prg != pp) backgroundWorker1.ReportProgress(prg = pp);
                sizes[i] = GetJpegInfo(jpgs[i], out bpps[i]);
            }

            using (var fs = new FileStream(pdf, FileMode.Create))
            using (var sw = new StreamWriter(fs) { AutoFlush = true })
            {
                var objp = new List<long>();
                var no_j = 3 + jpgs.Length * 2;

                sw.WriteLine("%PDF-1.2");

                sw.WriteLine();
                objp.Add(fs.Position);
                sw.WriteLine("1 0 obj");
                sw.WriteLine("<<");
                sw.WriteLine("  /Type /Catalog /Pages 2 0 R");
#if false
                sw.WriteLine("  /ViewerPreferences << /Direction /R2L >>");
#endif
                sw.WriteLine(">>");
                sw.WriteLine("endobj");

                sw.WriteLine();
                objp.Add(fs.Position);
                sw.WriteLine("2 0 obj");
                sw.WriteLine("<<");
                sw.WriteLine("  /Type /Pages /Count {0}", jpgs.Length);
                sw.WriteLine("  /Kids");
                sw.Write("  [");
                for (int i = 0; i < jpgs.Length; i++)
                {
                    if ((i & 7) == 0)
                    {
                        sw.WriteLine();
                        sw.Write("   ");
                    }
                    sw.Write(" {0} 0 R", 3 + i * 2);
                }
                sw.WriteLine();
                sw.WriteLine("  ]");
                sw.WriteLine(">>");
                sw.WriteLine("endobj");

                for (int i = 0; i < jpgs.Length; i++)
                {
                    if (backgroundWorker1.CancellationPending) return;
                    int pp = 20 + i * 10 / jpgs.Length;
                    if (prg != pp) backgroundWorker1.ReportProgress(prg = pp);

                    var no_p = 3 + i * 2;
                    var no_c = no_p + 1;
                    var name = "/Jpeg" + (i + 1);
                    var sz = sizes[i];

                    sw.WriteLine();
                    objp.Add(fs.Position);
                    sw.WriteLine("{0} 0 obj", no_p);
                    sw.WriteLine("<<");
                    sw.WriteLine("  /Type /Page /Parent 2 0 R /Contents {0} 0 R", no_c);
                    sw.WriteLine("  /MediaBox [ 0 0 {0} {1} ]", sz.Width, sz.Height);
                    if (sz.Width > sz.Height) sw.WriteLine("  /Rotate 90");
                    sw.WriteLine("  /Resources");
                    sw.WriteLine("  <<");
                    sw.WriteLine("    /ProcSet [ /PDF /ImageB /ImageC /ImageI ]");
                    sw.WriteLine("    /XObject << {0} {1} 0 R >>", name, no_j + i);
                    sw.WriteLine("  >>");
                    sw.WriteLine(">>");
                    sw.WriteLine("endobj");

                    sw.WriteLine();
                    objp.Add(fs.Position);
                    sw.WriteLine("{0} 0 obj", no_c);
                    var st4 = string.Format("q {0} 0 0 {1} 0 0 cm {2} Do Q",
                        sz.Width, sz.Height, name);
                    sw.WriteLine("<< /Length {0} >>", st4.Length);
                    sw.WriteLine("stream");
                    sw.WriteLine(st4);
                    sw.WriteLine("endstream");
                    sw.WriteLine("endobj");
                }

                for (int i = 0; i < jpgs.Length; i++)
                {
                    if (backgroundWorker1.CancellationPending) return;
                    int pp = 30 + i * 70 / jpgs.Length;
                    if (prg != pp) backgroundWorker1.ReportProgress(prg = pp);

                    using (var fsj = new FileStream(jpgs[i], FileMode.Open))
                    {
                        var name = "/Jpeg" + (i + 1);
                        var sz = sizes[i];

                        sw.WriteLine();
                        objp.Add(fs.Position);
                        sw.WriteLine("{0} 0 obj", no_j + i);
                        sw.WriteLine("<<");
                        sw.WriteLine("  /Type /XObject /Subtype /Image /Name {0}", name);
                        sw.WriteLine("  /Filter /DCTDecode /BitsPerComponent 8 /ColorSpace /{0}", devs[bpps[i]]);
                        sw.WriteLine("  /Width {0} /Height {1} /Length {2}", sz.Width, sz.Height, fsj.Length);
                        sw.WriteLine(">>");
                        sw.WriteLine("stream");
                        var buf = new byte[4096];
                        int len;
                        while ((len = fsj.Read(buf, 0, buf.Length)) > 0)
                            fs.Write(buf, 0, len);
                        sw.WriteLine();
                        sw.WriteLine("endstream");
                        sw.WriteLine("endobj");
                    }
                }

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

        public static Size GetJpegInfo(string jpg, out int bpp)
        {
            using (var fs = new FileStream(jpg, FileMode.Open))
            {
                var buf = new byte[8];
                while (fs.Read(buf, 0, 2) == 2 && buf[0] == 0xff)
                {
                    if (buf[1] == 0xc0 && fs.Read(buf, 0, 8) == 8)
                    {
                        bpp = buf[7];
                        return new Size(buf[5] * 256 + buf[6], buf[3] * 256 + buf[4]);
                    }
                    else if (buf[1] != 0xd8)
                    {
                        if (fs.Read(buf, 0, 2) == 2)
                            fs.Position += buf[0] * 256 + buf[1] - 2;
                        else
                            break;
                    }
                }
            }
            throw new Exception("not JPEG: " + jpg);
        }

        private void backgroundWorker1_DoWork(object sender, DoWorkEventArgs e)
        {
            var args = e.Argument as Args;
            MakePDF(args.pdf, args.jpgs);
            if (!backgroundWorker1.CancellationPending) e.Result = args.pdf;
        }

        private void backgroundWorker1_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            progressBar1.Value = e.ProgressPercentage;
        }

        private void backgroundWorker1_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            button1.Text = "作成";
            if (e.Result != null)
            {
                Process.Start(e.Result as string);
                progressBar1.Value = 0;
            }
            else if (e.Error != null)
                MessageBox.Show(this, e.ToString(), Text, MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
        }
    }
}
