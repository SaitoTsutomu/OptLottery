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
	public enum ECondition { Nothing, MaleOnly, FemaleOnly, NoMale, NoFemale, NoBoth }
	public class Rental
	{
		public string Name { get; set; }
		public string Address { get; set; }
		public decimal Size { get; set; }
		public string[] Attrib { get { return _Attrib == null ? new string[0] : _Attrib.ToArray(); } set { _Attrib = new List<string>(value); } }
		internal List<string> _Attrib;
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
		public string[] Attrib { get { return _Attrib == null ? new string[0] : _Attrib.ToArray(); } set { _Attrib = new List<string>(value); } }
		internal List<string> _Attrib;
		public Wish[] Wishes { get { return _Wishes.ToArray(); } set { _Wishes = new List<Wish>(value); } }
		internal List<Wish> _Wishes;
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
		public double AddVal()
		{
			double sum = 0;
			var dic = new Dictionary<string, string>();
			if (Rental.Attrib == null || Bid.Attrib == null) return 0;
			foreach (var s in Rental.Attrib) dic[s] = null;
			foreach (var s in Bid.Attrib) if (dic.ContainsKey(s)) ++sum;
			return sum * 100000;
		}
		public override string ToString()
		{
			return string.Format("{0}/{1}/{2}/{3}", Rental.Name, Bid.Name, Premium, Price);
		}
	}
	class Program
	{
		[STAThread]
		static void Main(string[] args)
		{
			new MainForm().ShowDialog();
		}
	}
}
