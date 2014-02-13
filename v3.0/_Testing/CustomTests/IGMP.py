test(name="IGMP",
     test="Default",
     platform="windows")

test(name="IGMP",
     test="Default",
     platform="linux")

if "osx" in get_platform():
    '''
    can get multicast udp to happen on osx, but not igmp. however, igmp did
    work on linux, so this code should be a good test.
    '''
    # http://stackoverflow.com/questions/603852/multicast-in-python
    import time
    import threading
    import socket
    import struct

    MCAST_GRP = '224.0.0.22'
    MCAST_PORT = 5007
    RECEIVED = ''

    def osx_test_send():
        sock = socket.socket(socket.AF_INET, socket.SOCK_RAW, socket.IPPROTO_IGMP)
        sock.setsockopt(socket.IPPROTO_IP, socket.IP_MULTICAST_TTL, 2)
        sock.sendto("ROBOTO", (MCAST_GRP, MCAST_PORT))

    def osx_test_receive():
        global RECEIVED
        sock = socket.socket(socket.AF_INET, socket.SOCK_RAW, socket.IPPROTO_IGMP)
        sock.settimeout(2)
        # sock.setsockopt(socket.SOL_SOCKET, socket.SO_REUSEPORT, 1)
        sock.bind(('', MCAST_PORT))
        mreq = struct.pack("4sl", socket.inet_aton(MCAST_GRP), socket.INADDR_ANY)

        sock.setsockopt(socket.IPPROTO_IP, socket.IP_ADD_MEMBERSHIP, mreq)

        try:
             rcvd = sock.recv(4096)
             RECEIVED += rcvd
        except:
            # catch recv timeout exception
            pass

    try:
        t1 = threading.Thread(target=osx_test_receive)
        t2 = threading.Thread(target=osx_test_send)

        t1.start()
        t2.start()

        t1.join(1)
        t2.join(1)
    except:
        # exceptions are failures....
        pass

    ###########################
    # this is our golden ticket
    if 'ROBOTO' in RECEIVED:
        test(name="IGMP",
             test="Default",
             platform="osx")

