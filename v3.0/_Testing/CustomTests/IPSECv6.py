#!/usr/bin/env python

test(name="IPSECv6",
     test="Default",
     platform="linux")

test(name="IPSECv6",
     test="AH",
     platform="linux")

test(name="IPSECv6",
    test="Default",
    platform="windows")

test(name="IPSECv6",
    test="AH",
    platform="windows")
