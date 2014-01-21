
#
# Example Python fixup for Peach 3
#
# Authors:
# Michael Eddingtion
#

# -- Example returning a string ---------------

class PortCommandFormatFixup:
  def __init__(self, parent):
    self._parent = parent

  def fixup(self, element):
    splt = element.InternalValue.split(",")
    ip = splt[0].replace(".", ",")
    port = hex(int(splt[1])).split("0x")[1]
    p1 = port[:2]
    p2 = port[2:]
    if p1 is not '':
      port = str(int(p1,16))+","+str(int(p2,16))
    else:
      port = str(int(p1,16))

    return ip + "," + port

# --------------------------------------------
# end