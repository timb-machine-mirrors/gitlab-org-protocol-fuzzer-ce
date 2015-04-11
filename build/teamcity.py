#!/usr/bin/env python

import os

if __name__ == "__main__":
	for key in os.environ.keys():
		print('%s: %s' % (key, os.environ[key]))

	print("##teamcity[buildNumber '0.0.0']")
