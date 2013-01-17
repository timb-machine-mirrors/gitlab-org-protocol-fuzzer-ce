from Peach.fixup import Fixup


class CRC24Fixup(Fixup):
	def __init__(self, ref):
		Fixup.__init__(self)
		self.ref = ref

	def fixup(self):
		self.context.defaultValue = "0"
		data = self._findDataElementByName(self.ref).getValue()
		crc24_init = 0xb704ce
		crc24_poly = 0x1864cfb
		crc = crc24_init
		for i in list(data):
			crc = crc ^ (ord(i) << 16)
			for j in range(0, 8):
				crc = crc << 1
				if crc & 0x1000000:
					crc = crc ^ crc24_poly
		return crc & 0xffffff
