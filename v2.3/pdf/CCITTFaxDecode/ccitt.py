
from bits import BitBuffer

fd = open("c32d.bin", "rb")
bits = BitBuffer(fd.read())
fd.close()

