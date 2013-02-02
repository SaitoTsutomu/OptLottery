using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Yaml.Serialization;
using System.Diagnostics;
using System.Text.RegularExpressions;
using System.IO;

namespace OptLottery
{
	public partial class MainForm : Form
	{
		internal Rental[] rentals;
		internal Bid[] bids;
		public MainForm()
		{
			InitializeComponent();
		}
		private void MainForm_Load(object sender, EventArgs e)
		{
			var ys = new YamlSerializer();
			//var oo = ys.DeserializeFromFile(args.Length > 0 ? args[0]
			//    : "data.txt", typeof(Rental[]), typeof(Bid[]));
			var oo = ys.DeserializeFromFile("data.txt", typeof(Rental[]), typeof(Bid[]));
			//ys.SerializeToFile("data0.txt", oo);
			rentals = (Rental[])oo[0];
			bids = (Bid[])oo[1];
			foreach (Rental re in rentals) listBox1.Items.Add(re.Name);
			foreach (Bid bi in bids) listBox2.Items.Add(bi.Name);
		}
		private void button1_Click(object sender, EventArgs e)
		{
			Solve(rentals, bids);
		}
		private void Solve(Rental[] rentals, Bid[] bids)
		{
			var vars = new List<Var>();
			foreach (var bi in bids) foreach (var wi in bi.Wishes)
					wi.Regex = new Regex(wi.Area, RegexOptions.Compiled);
			foreach (var re in rentals)
			{
				foreach (var bi in bids)
				{
					var va = bi.Find(re);
					if (va == null) continue;
					vars.Add(va);
					re.Vars.Add(va);
					bi.Vars.Add(va);
				}
			}
			for (int i = 0; i < vars.Count; ++i) vars[i].ID = i;
			//foreach (var va in vars) Console.WriteLine(va);
			var mnam = "model.txt";
			using (var sw = new StreamWriter(mnam, false, Encoding.GetEncoding(932)))
			{
				sw.WriteLine("maximize");
				sw.Write("obj:");
				var rnd = new Random();
				foreach (var va in vars)
				{
					var co = va.Price + va.AddVal() + rnd.NextDouble();
					sw.Write(" + {0} v{1}", co, va.ID);
				}
				sw.WriteLine();
				sw.WriteLine("subject to");
				int cnt = 0;
				foreach (var re in rentals)
				{
					if (re.Vars.Count == 0) continue;
					sw.Write("r{0}:", cnt++);
					foreach (var va in re.Vars) sw.Write(" + v{0}", va.ID);
					sw.WriteLine(" <= 1");
				}
				foreach (var bi in bids)
				{
					if (bi.Vars.Count == 0) continue;
					sw.Write("b{0}:", cnt++);
					foreach (var va in bi.Vars) sw.Write(" + v{0}", va.ID);
					sw.WriteLine(" <= 1");
				}
				sw.WriteLine("binary");
				foreach (var va in vars) sw.WriteLine("v{0}", va.ID);
				sw.WriteLine("end");
			}
			var proc = new Process();
			proc.StartInfo.FileName = "symphony";
			proc.StartInfo.RedirectStandardOutput = true;
			proc.StartInfo.UseShellExecute = false;
			proc.StartInfo.Arguments = "-L " + mnam;
			proc.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
			proc.Start();
			var res = proc.StandardOutput.ReadToEnd();
			proc.WaitForExit();
			var pos = res.LastIndexOf("+++");
			if (pos < 0) return;
			var m = Regex.Match(res.Substring(pos + 3), "v([0-d]+)\\s+[0-9.]+");
			var sb = new StringBuilder();
			while (m.Success)
			{
				int i = int.Parse(m.Groups[1].Value);
				var va = vars[i];
				sb.AppendFormat("{0} {1} {2}\r\n", va.Rental.Name, va.Bid.Name, va.Price);
				m = m.NextMatch();
			}
			textBox1.Text = sb.ToString();
		}
	}
}
