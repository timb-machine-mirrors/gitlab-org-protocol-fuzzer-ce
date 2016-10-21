from waflib.Configure import conf

@conf
def read_paket(self, lockfile):
	if not self.env.MCS:
		return

	# src = self.path.find_resource(lockfile)
	# if src:
	# 	contents = src.read()
	# 	for line in contents.split('\r\n'):
	# 		print line
