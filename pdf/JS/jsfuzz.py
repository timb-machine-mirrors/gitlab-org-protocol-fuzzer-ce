
from Peach.Publishers.file import FileWriterLauncherGui
import os

class JsFuzzPublisher(FileWriterLauncherGui):

	JsOpen = "function fuzz_me()\n{\nconsole.show();console.println('fuzz_me starting');\n"
	JsClose = "\nconsole.println('fuzz_me ending');};\n\n"

	def __init__(self, filename, windowname, debugger = "false", waitTime = 3):
		FileWriterLauncherGui.__init__(self, filename, windowname, debugger, waitTime)
	
	def call(self, method, args):
		
		if method == 'FuzzJavascript':
			
			# do stuff
			js = self.JsOpen + args[0] + "("
			
			for a in args[1:]:
				js += "'%s'," % a
			
			js = js[:-1] + ");\n" + self.JsClose
		
			self._fd.write(js)
		
		else:
			os.system("PdfJsMaker.exe")
			return FileWriterLauncherGui.call(self, method, args)

	def send(self, data):
		pass


# end

