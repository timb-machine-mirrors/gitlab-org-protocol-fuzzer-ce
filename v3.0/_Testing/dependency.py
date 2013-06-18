#!/usr/bin/env python

import os
import re
import glob
from xml.dom import minidom


class PeachPit(object):
    Pits = []

    def __init__(self, Name, PathToPit, Config='', PathDefines="."):
        self.Depts = []
        self.Name = Name
        self.Config = Config
        self.PathToPit = self.SanitizePath(PathToPit)
        self.PathDefines = PathDefines
        self.PitList = []

    def ReplacePathDefine(self, define):
        src = define.attributes['src'].value
        src = src.replace("##Path##", self.PathDefines)
        src = self.SanitizePath(src)
        define.attributes['src'].value = src

    def SanitizePath(self, path):
        return path.replace("\\", "/")

    def BuildPitsListToTest(self, pit, folder):
        if re.search('.+_Data\.xml', pit):
            pit = pit.replace("_Data", "")
            pit = folder + "/" + pit
            if pit not in PeachPit.Pits and os.path.exists(pit):
                PeachPit.Pits.append(pit)
        else:
            pit = folder + "/" + pit
            if pit not in PeachPit.Pits and os.path.exists(pit):
                PeachPit.Pits.append(pit)

    def PrintPitsToTest(self, indent, main=False):
        """
        Prints the dependency tree of the target pit.
        """
        if main:
            print self.Name

        for n in PeachPit.Pits:
            print ("\t" * indent) + n

    def GetPitList(self):
        """
        Returns the pit dependencies in the structure defined by the test program
        """
        pits = []
        for pit in PeachPit.Pits:
            path = os.path.dirname(pit)
            fn = os.path.basename(pit)
            pits.append({"path": path, "file": fn})
        return pits

    def AddClassName(self):
        PeachPit.Name = self.Name
        PeachPit.PitsChecked = []

    def IsPitMe(self, pit):
        """
        Returns True if the current pit includes the target test pit
        """
        if re.search('.+_Data\.xml', pit):
            pit = pit.replace("_Data", "")
            return pit == PeachPit.Name
        return False

    def AlreadyCheckedPit(self, pit):
        if re.search('.+_Data\.xml', pit):
            new_pit = pit.replace("_Data", "")
            return new_pit in PeachPit.PitsChecked
        return False

    def BuildPitList(self):
        """
        Adds all pits in the directory to the list to check if it depends on the
        given pit.
        """
        for pits in glob.glob(self.PathToPit + "/*.xml"):
            self.PitList.append(os.path.basename(pits))

    def TestDependencies(self):
        self.BuildPitList()
        for pits in self.PitList:
            pits = self.SanitizePath(pits)
            PeachPit.CurrName = pits
            if PeachPit.CurrName != PeachPit.Name:
                self.AddPitsThatDenpendOnMe(pits)
            PeachPit.PitsChecked.append(PeachPit.CurrName)
            self.Depts = []

    def AddPitsThatDenpendOnMe(self, pit=""):
        """
        Looks at all includes for the given file to see if it contains the test
        pit
        """
        if self.IsPitMe(pit):
            self.BuildPitsListToTest(PeachPit.CurrName,
                                     os.path.basename(self.PathToPit))
            return
        if self.AlreadyCheckedPit(pit):
            return

        self.Pit = minidom.parse(self.PathToPit + "/" + pit)
        defines = self.Pit.getElementsByTagName('Include')

        for define in defines:
            if 'src' in define.attributes.keys():
                self.ReplacePathDefine(define)
                name = os.path.basename(define.attributes['src'].value)
                path_to_pit = os.path.dirname(define.attributes['src'].value)
                path_to_pit = path_to_pit.split('file:')[1]
                self.Depts.append(PeachPit(name, path_to_pit, self.Config,
                                  self.PathDefines))
        for depts in self.Depts:
            depts.AddPitsThatDenpendOnMe(depts.Name)


def GetPits(path, fn):
    pit = PeachPit(fn, path)
    pit.AddClassName()
    pit.TestDependencies()
    return pit.GetPitList()


if __name__ == '__main__':
    print "Please run tester.py to test dependencies\n\n"
    exit(0)
