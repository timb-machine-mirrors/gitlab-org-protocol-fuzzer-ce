
'''
Peach extension for Intel AMT 6.1 Fuzzing

@author: Michael Eddington
'''

#
# Copyright (c) Deja vu Security
#

# Authors:
#   Michael Eddington (mike@dejavusecurity.com)

import socket, time, sys, os, struct, re
import urllib2, httplib, urllib
import urllib2, httplib, urllib
from Peach.publisher import *
import Peach

def Debug(msg):
	if Peach.Engine.engine.Engine.debug:
		print msg


class AmtDdnsUdpListener(Publisher):
	
	def __init__(self, host, port, user, pwd, timeout = 2):
		Publisher.__init__(self)
		self._host = host
		self._port = port
		self._user = user
		self._pass = pwd
		self._timeout = float(timeout)
		self.count = 0
		
		if self._timeout == None:
			self._timeout = 2
			
		self._socket = None
			
	def stop(self):
		self.close()
			
	def close(self):
		if self._socket != None:
			self._socket.close()
			self._socket = None
	
	def send(self, data):
		try:
			self._socket.sendto(data, self.addr)
		
		except socket.error:
			pass

	def accept(self):
		self.count += 1
		
		if self._socket != None:
			# Close out old socket first
			self._socket.close()
		
		# Change our hostname
		
		passmgr = urllib2.HTTPPasswordMgrWithDefaultRealm();
		passmgr.add_password(None, "http://"+self._host+":16992", 
			self._user, self._pass)
		
		auth_handler = urllib2.HTTPDigestAuthHandler(passmgr)
		opener = urllib2.build_opener(auth_handler)
		urllib2.install_opener(opener)
		
		## Turn updates off
		
		req = urllib2.Request("http://"+self._host+":16992/fqdn.htm")
		fd = urllib2.urlopen(req)
		data = fd.read()
		fd.close()
		
		data = data[data.find('NAME="t" value="')+len('NAME="t" value="'):]
		data = data[:data.find('"')]
		data = "t=%s&HostName=peachy_%d&DomainName=fuzzer.peachy.com&PriUpdInt=1440" % (urllib.quote(data), self.count)
		#data = "t=%s&HostName=peachy_%d&DomainName=int.dejavusecurity.com&PriUpdInt=1440" % (urllib.quote(data), self.count)
		
		req = urllib2.Request("http://"+self._host+":16992/fqdnform", data, {"Content-Type" : "application/x-www-form-urlencoded"})
		fd = urllib2.urlopen(req)
		data = fd.read()
		fd.close()
		
		## Turn Updates on
		
		req = urllib2.Request("http://"+self._host+":16992/fqdn.htm")
		fd = urllib2.urlopen(req)
		data = fd.read()
		fd.close()
		
		data = data[data.find('NAME="t" value="')+len('NAME="t" value="'):]
		data = data[:data.find('"')]
		data = "t=%s&HostName=peachy_%d&DomainName=fuzzer.peachy.com&EnDnsClient=on&PriUpdInt=1440" % (urllib.quote(data), self.count)
		#data = "t=%s&HostName=peachy_%d&DomainName=int.dejavusecurity.com&EnDnsClient=on&PriUpdInt=1440" % (urllib.quote(data), self.count)
		
		req = urllib2.Request("http://"+self._host+":16992/fqdnform", data, {"Content-Type" : "application/x-www-form-urlencoded"})
		fd = urllib2.urlopen(req)
		data = fd.read()
		fd.close()
		
		## SETUP SOCKET
		
		self._socket = socket.socket(socket.AF_INET, socket.SOCK_DGRAM, 
			socket.IPPROTO_UDP)
		
		self._socket.bind( ("", int(self._port)) )
		
		
	def receive(self, size = None):
		data,self.addr = self._socket.recvfrom(65565)
		
		if hasattr(self, "publisherBuffer"):
			self.publisherBuffer.haveAllData = True
		
		return data

# end
