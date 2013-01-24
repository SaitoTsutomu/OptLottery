#!/usr/bin/python
"""
Optimize Lottery
http://pyyaml.org/wiki/PyYAML
"""
import os, random, re, sys, yaml
from subprocess import Popen, PIPE

class Rental(yaml.YAMLObject):
	yaml_tag = '!Rental'
class Wish(yaml.YAMLObject):
	yaml_tag = '!Wish'
	def Find(self, rt, bi):
		if (self.MinSize > rt.Size or not self.Regex.match(rt.Address)): return None
		if (self.Premium and self.MaxPrice >= rt.PremiumPrice): return Var(rt, bi, True)
		if (self.MaxPrice >= rt.NormalPrice): return Var(rt, bi, False)
		return None
class Bid(yaml.YAMLObject):
	yaml_tag = '!Bid'
	def Find(self, rt):
		#Condition : 'Nothing', 'MaleOnly', 'FemaleOnly', 'NoMale', 'NoFemale', 'NoBoth'
		#Gender : 'Male', 'Female', 'Both'
		if (rt.Condition == 'MaleOnly' and self.Gender != 'Male'): return None
		if (rt.Condition == 'FemaleOnly' and self.Gender != 'Female'): return None
		if (rt.Condition == 'NoMale' and self.Gender == 'Male'): return None
		if (rt.Condition == 'NoFemale' and self.Gender == 'Female'): return None
		if (rt.Condition == 'NoBoth' and self.Gender == 'Both'): return None
		for wi in self.Wishes:
			va = wi.Find(rt, self)
			if (va != None): return va
		return None
class Var:
	def __init__(self, rt, bi, pr): self.Rental, self.Bid, self.Premium = [rt, bi, pr]
	def Price(self): return self.Rental.PremiumPrice if self.Premium else self.Rental.NormalPrice
class OptLottery:
	@staticmethod
	def Do(dnam):
		f = open(dnam)
		dt = yaml.load_all(f)
		f.close()
		rentals = dt.next()
		bids = dt.next()
		vars = []
		for bi in bids:
			bi.Vars = []
			for wi in bi.Wishes:
				wi.Regex = re.compile(wi.Area)
		for rt in rentals:
			rt.Vars = []
			for bi in bids:
				va = bi.Find(rt)
				if (va != None):
					vars.append(va)
					rt.Vars.append(va)
					bi.Vars.append(va)
		for i in xrange(len(vars)): vars[i].ID = i
		mnam = 'model.txt'
		f = open(mnam, 'w')
		f.write('maximize\n')
		f.write('obj:')
		for va in vars:
			co = va.Price() + random.random()
			f.write(' + %f v%d' % (co, va.ID))
		f.write('\nsubject to\n')
		for (i, rt) in enumerate(rentals):
			if (len(rt.Vars) > 0):
				f.write('r%d:' % i)
				for va in rt.Vars: f.write(' + v%d' % va.ID)
				f.write(' <= 1\n')
		for (i, bi) in enumerate(bids):
			if (len(bi.Vars) > 0):
				f.write('b%d:' % i)
				for va in bi.Vars: f.write(' + v%d' % va.ID)
				f.write(' <= 1\n')
		f.write('binary\n')
		for va in vars: f.write('v%d\n' % va.ID)
		f.write('end\n')
		f.close()
		lines = Popen('symphony -L %s' % mnam, stdout = PIPE, shell = True).stdout.readlines()
		i = [i for (i, line) in enumerate(lines) if line[1:4] == '+++'][1]
		for s in lines[i + 1:]:
			ss = s.split()
			if (len(ss) < 2): break
			va = vars[int(ss[0][1:])]
			print va.Rental.Name, va.Bid.Name, va.Price()
		print 'Press Enter'
		sys.stdin.readline()

if __name__ == '__main__': OptLottery.Do('data.txt')