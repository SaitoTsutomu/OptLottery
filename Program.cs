using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Yaml.Serialization;

/*
https://projects.coin-or.org/SYMPHONY
http://yaml.codeplex.com/
 */

namespace OptLottery
{
	//public enum EType { Single, Family }
	public enum EGender { Male, Female, Both }
	public enum ECondition { None, MaleOnly, FemaleOnly, NoMale, NoFemale, NoBoth }
	public class Rental
	{
		public string Name { get; set; }
		public string Address { get; set; }
		public decimal Size { get; set; }
		public ECondition Condition { get; set; }
		public int PremiumPrice { get; set; }
		public int NormalPrice { get; set; }
		internal List<Var> Vars;
		public Rental()
		{
			Vars = new List<Var>();
		}
	}
	public class Wish
	{
		public string Area { get; set; }
		public decimal MinSize { get; set; }
		public int MaxPrice { get; set; }
		public bool Premium { get; set; }
		internal Regex Regex;
		public Var Find(Rental re, Bid bi)
		{
			if (MinSize > re.Size || !Regex.IsMatch(re.Address)) return null;
			if (Premium && MaxPrice >= re.PremiumPrice) return new Var(re, bi, true);
			if (MaxPrice >= re.NormalPrice) return new Var(re, bi, false);
			return null;
		}
	}
	public class Bid
	{
		public string Name { get; set; }
		public EGender Gender { get; set; }
		public Wish[] Wishes { get { return _Wishes.ToArray(); } set { _Wishes = new List<Wish>(value); } }
		internal List<Wish> _Wishes { get; set; }
		internal List<Var> Vars;
		public Bid()
		{
			_Wishes = new List<Wish>();
			Vars = new List<Var>();
		}
		public Var Find(Rental re)
		{
			if (re.Condition == ECondition.MaleOnly && Gender != EGender.Male) return null;
			if (re.Condition == ECondition.FemaleOnly && Gender != EGender.Female) return null;
			if (re.Condition == ECondition.NoMale && Gender == EGender.Male) return null;
			if (re.Condition == ECondition.NoFemale && Gender == EGender.Female) return null;
			if (re.Condition == ECondition.NoBoth && Gender == EGender.Both) return null;
			Var va;
			foreach (var wi in Wishes) if ((va = wi.Find(re, this)) != null) return va;
			return null;
		}
	}
	public class Var
	{
		public int ID;
		public Rental Rental;
		public Bid Bid;
		public bool Premium;
		public int Price { get { return Premium ? Rental.PremiumPrice : Rental.NormalPrice; } }
		public Var(Rental re, Bid bi, bool pr)
		{
			Rental = re;
			Bid = bi;
			Premium = pr;
		}
		public override string ToString()
		{
			return string.Format("{0}/{1}/{2}/{3}", Rental.Name, Bid.Name, Premium, Price);
		}
	}
	class Program
	{
		static void Main(string[] args)
		{
			var ys = new YamlSerializer();
			var oo = ys.DeserializeFromFile(args.Length > 0 ? args[0]
				: "data.txt", typeof(Rental[]), typeof(Bid[]));
			Solve((Rental[])oo[0], (Bid[])oo[1]);
		}
		private static void Solve(Rental[] rentals, Bid[] bids)
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
					var co = va.Price + rnd.NextDouble();
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
			while (m.Success)
			{
				int i = int.Parse(m.Groups[1].Value);
				var va = vars[i];
				Console.WriteLine("{0} {1} {2}", va.Rental.Name, va.Bid.Name, va.Price);
				m = m.NextMatch();
			}
		}
	}
}
