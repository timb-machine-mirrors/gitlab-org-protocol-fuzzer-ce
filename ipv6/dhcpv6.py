
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


class AmtUdp6Listener(Publisher):
	'''
	A simple UDP publisher.
	'''
	
	def __init__(self, host, port, timeout = 2):
		Publisher.__init__(self)
		self._host = host
		self._port = port
		self._timeout = float(timeout)
		
		if self._timeout == None:
			self._timeout = 2
			
		self._socket = None
		self.mreq = None
					
	def stop(self):
		self.close()
			
	def close(self):
		if self._socket != None:
			try:
				self._socket.setsockopt(socket.IPPROTO_IPV6, socket.IPV6_LEAVE_GROUP, self.mreq)
			except:
				pass
			self._socket.close()
			self._socket = None
	
	def send(self, data):
		try:
			self._socket.sendto(data, self.addr)
		
		except socket.error:
			pass

	def accept(self):	
		if self._socket != None:
			# Close out old socket first
			self._socket.setsockopt(socket.IPPROTO_IPV6, socket.IPV6_LEAVE_GROUP, self.mreq)
			self._socket.close()
			self._socket = None
		
		# Turn IPV6 Off/On
		
		passmgr = urllib2.HTTPPasswordMgrWithDefaultRealm();
		passmgr.add_password(None, "http://192.168.1.3:16992", 
			"admin", "Admin!98")
		
		auth_handler = urllib2.HTTPDigestAuthHandler(passmgr)
		opener = urllib2.build_opener(auth_handler)
		urllib2.install_opener(opener)
		
		## TURN OFF
		
		req = urllib2.Request("http://192.168.1.3:16992/ipv6.htm")
		fd = urllib2.urlopen(req)
		data = fd.read()
		fd.close()
		
		data = data[data.find('NAME="t" value="')+len('NAME="t" value="'):]
		data = data[:data.find('"')]
		data = "t=%s&Ipv6Address=&DefaultGatway=&DNSServer=&AlternativeDns=" % (urllib.quote(data))
		
		req = urllib2.Request("http://192.168.1.3:16992/ipv6form", data, {"Content-Type" : "application/x-www-form-urlencoded"})
		fd = urllib2.urlopen(req)
		data = fd.read()
		fd.close()
		
		## TURN BACK ON
		
		time.sleep(1)
		
		req = urllib2.Request("http://192.168.1.3:16992/ipv6.htm")
		fd = urllib2.urlopen(req)
		data = fd.read()
		
		data = data[data.find('NAME="t" value="')+len('NAME="t" value="'):]
		data = data[:data.find('"')]
		data = "t=%s&Ipv6Enabled=on&Ipv6Address=&DefaultGatway=&DNSServer=&AlternativeDns=" % (urllib.quote(data))
		
		req = urllib2.Request("http://192.168.1.3:16992/ipv6form", data, {"Content-Type" : "application/x-www-form-urlencoded"})		
		fd = urllib2.urlopen(req)
		data = fd.read()
		fd.close()
		
		## SETUP SOCKET
		
		self._socket = socket.socket(socket.AF_INET6, socket.SOCK_DGRAM, 
			socket.IPPROTO_UDP)
		
		self._socket.bind( ("", int(self._port)) )
		
		addrinfo = socket.getaddrinfo("ff02::1:2", None)[0]
		group_bin = socket.inet_pton(addrinfo[0], addrinfo[4][0])
		self.mreq = group_bin + struct.pack("@I", 0)
		
		#self._socket.setsockopt(socket.IPPROTO_IPV6, socket.IPV6_LEAVE_GROUP, self.mreq)
		self._socket.setsockopt(socket.IPPROTO_IPV6, socket.IPV6_JOIN_GROUP, self.mreq)
		
		
	def receive(self, size = None):
		start = time.time()
		while(1):
			data,self.addr = self._socket.recvfrom(65565)
			if self.addr[0].lower().find(self._host.lower()) != -1:
				print "GOT ONE! ", self.addr
				break
			
			print "yuck", self.addr
			
			if time.time() - start > 60:
				print "timeout"
				raise SoftException()
		
		if hasattr(self, "publisherBuffer"):
			self.publisherBuffer.haveAllData = True
		
		return data

# end
