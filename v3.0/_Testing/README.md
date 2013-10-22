This dir contains a set of tools to test that pits launch and run. As you're writing pits please use this tool to verify your pit works.

Since a lot of pits have a lot of cross reliance changes to one pit can easily break another. These testing tools will make sure that pits are not broken by changes to peach itself or other pits. 

This is run nightly against Peach Community and PeachE:
Peach Community - http://10.0.1.76:8010/ 
PeachE - http://10.0.1.76:8011/ 

It can also be run in a stand alone mode.

To run tests in stand alone change directories to pits/v3.0 and run `_Testing/testlib.py` as root or administrator. 

The testing system requires win32 python2.7 on windows, and python2.7 on Linux/OSX.

If you already have peach in your path (`peach.bat` if windows, `peach` if Linux) the tests should run. If not it will be necessary to define where the peach binary via arguments. To get a list of other arguments use the help argument -h:

```
$ _Testing/tester.py -h
usage: tester.py [-h] [-c COUNT] [-p PEACH] [-l LOGDIR] [target [target ...]]

Test peach pits (this must be run as root)

positional arguments:
  target                The the specific targets to test, e.g.: Net/TCPv4.xml

optional arguments:
  -h, --help            show this help message and exit
  -c COUNT, --count COUNT
                        The number of iterations to run
  -p PEACH, --peach PEACH
                        The location of the peach binary
  -l LOGDIR, --logdir LOGDIR
                        location to log test output

```

To test an individual pit pass it's location on the command line:

```
$ sudo _Testing/tester.py -p `which peach` Net/ARP.xml
Password: 
running /home/josh/bin/peach -D Path=. -1 --definedvalues Net/ARP.xml.config Net/ARP.xml Default
SUCCESS!
```
Any number of pits can be added as positional arguments. Most pits require no special configuration, however some require set up or can only run on a specific platform. For these you will need to add a definition. 

Definitions are python files. They exist in the pits/v3.0/_Testing/CustomTests directory and must be named PROTO.py where PROTO matches the pit name. They have the following syntax:

```
test(name="PROTO",
     platform="plat",
     test="TestName"
     setup=function,
     teardown=function)

define(name="PROTO",
       Key="value")
```

The only mandatory value to set is name because it is used to link a pit to a test. The absolute minimum for a valid test case is the following:

```
test(name="PROTO")
```

The value of "name" that is passed in to "test()" must be unique, unless the value "test" is defined. The following *is* valid:

```
test(name="PROTO"
    test="test1")

test(name="PROTO",
     test="test2")

```

the "test" variable represents the pit test. It the default value is "Default". The example above would execute the following peach command lines:

```
peach -D Path=. -1 --definedvalues Net/PROTO.xml.config Net/PROTO.xml test1
peach -D Path=. -1 --definedvalues Net/PROTO.xml.config Net/PROTO.xml test2
```

The setup and teardown functions are passed an instance of the PeachTest. This means that values that need to be passed between setup and teardown can be added to the context. An example of using setup and teardown can be found in the TCPv4 and TCPv6 tests. 

Testing code is subject to change, but test definitions should be forward compatible for the forseeable future.

If there are any questions, ask me. josh@dejavusecurity.com
