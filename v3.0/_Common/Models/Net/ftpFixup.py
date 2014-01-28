class FtpPortToHexFixup:
  """
      FTP Python Fixup.
      Converts the port number of an IP/Port combination to hex.
      Input: 127.0.0.1,31337 Outputs: 127,0,0,1,122,105
  """

  def __init__(self, parent):
    self._parent = parent

  def fixup(self, element):
    splt = str(element.InternalValue).split(",")
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

class FtpHexPortToDecimalFixup:
  """
      FTP Python Fixup.
      Parses port for the FTP IP/Hex port combination as a decimal
      Input: 127,0,0,1,122,105 Outputs: 127.0.0.1,31337
  """

  def __init__(self, parent):
    self._parent = parent

  def fixup(self, element):
    splt = str(element.InternalValue).split(",")
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