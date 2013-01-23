#!/usr/bin/python
"""
Optimize Lottery
http://pyyaml.org/wiki/PyYAML
"""
import os, random, re, sys, yaml
from subprocess import Popen, PIPE

class EGender:
	Male, Female, Both = range(3)
	@staticmethod
	def Parse(s):
		return ['Male', 'Female', 'Both'].index(s)
class ECondition:
	Nothing, MaleOnly, FemaleOnly, NoMale, NoFemale, NoBoth = range(6)
	@staticmethod
	def Parse(s):
		return ['Nothing', 'MaleOnly', 'FemaleOnly', 'NoMale', 'NoFemale', 'NoBoth'].index(s)
class Rental:
	def __init__(self, dic):
		self.Name = dic['Name']
		self.Address = dic['Address']
		self.Size = dic['Size']
		self.Condition = ECondition.Parse(dic['Condition'])
		self.PremiumPrice = dic['PremiumPrice']
		self.NormalPrice = dic['NormalPrice']
		self.Vars = []
class Wish:
	def __init__(self, dic):
		self.Area = dic['Area']
		self.MinSize = dic['MinSize']
		self.MaxPrice = dic['MaxPrice']
		self.Premium = dic['Premium']
	def Find(self, rt, bi):
		if (self.MinSize > rt.Size or not self.Regex.match(rt.Address)): return None
		if (self.Premium and self.MaxPrice >= rt.PremiumPrice): return Var(rt, bi, True)
		if (self.MaxPrice >= rt.NormalPrice): return Var(rt, bi, False)
		return None
class Bid:
	def __init__(self, dic):
		self.Name = dic['Name']
		self.Gender = EGender.Parse(dic['Gender'])
		self.Wishes = [Wish(wi) for wi in dic['Wishes']]
		self.Vars = []
	def Find(self, rt):
		if (rt.Condition == ECondition.MaleOnly and self.Gender != EGender.Male): return None
		if (rt.Condition == ECondition.FemaleOnly and self.Gender != EGender.Female): return None
		if (rt.Condition == ECondition.NoMale and self.Gender == EGender.Male): return None
		if (rt.Condition == ECondition.NoFemale and self.Gender == EGender.Female): return None
		if (rt.Condition == ECondition.NoBoth and self.Gender == EGender.Both): return None
		for wi in self.Wishes:
			va = wi.Find(rt, self)
			if (va != None): return va
		return None
class Var:
	def __init__(self, rt, bi, pr):
		self.ID = -1
		self.Rental = rt
		self.Bid = bi
		self.Premium = pr
	def Price(self): return self.Rental.PremiumPrice if self.Premium else self.Rental.NormalPrice
	def __repr__(self):
		return "%s/%s/%s/%d" % (self.Rental.Name, self.Bid.Name, self.Premium, self.Price())
class OptLottery:
	@staticmethod
	def Do(dnam):
		f = open(dnam)
		dt = yaml.load_all(f)
		rentals = [Rental(rt) for rt in dt.next()]
		bids = [Bid(bi) for bi in dt.next()]
		vars = []
		for bi in bids:
			for wi in bi.Wishes:
				wi.Regex = re.compile(wi.Area)
		for rt in rentals:
			for bi in bids:
				va = bi.Find(rt)
				if (va == None): continue
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
		cnt = 0
		for rt in rentals:
			if (len(rt.Vars) == 0): continue
			cnt += 1
			f.write('r%d:' % cnt)
			for va in rt.Vars: f.write(' + v%d' % va.ID)
			f.write(' <= 1\n')
		for bi in bids:
			if (len(bi.Vars) == 0): continue
			cnt += 1
			f.write('b%d:' % cnt)
			for va in bi.Vars: f.write(' + v%d' % va.ID)
			f.write(' <= 1\n')
		f.write('binary\n')
		for va in vars: f.write('v%d\n' % va.ID)
		f.write('end\n')
		f.close()
		lines = Popen('symphony -L %s' % mnam, stdout = PIPE, shell = True).stdout.readlines()
		n = 0
		for s in lines:
			if (n != 2):
				if (s[1:4] == '+++'): n += 1
				continue
			ss = s.split()
			if (len(ss) < 2): break
			v = float(ss[1])
			if (v == 0): continue
			va = vars[int(ss[0][1:])]
			print va.Rental.Name, va.Bid.Name, va.Price()
		print 'Press Enter'
		sys.stdin.readline()

if __name__ == '__main__': OptLottery.Do('data.txt')
