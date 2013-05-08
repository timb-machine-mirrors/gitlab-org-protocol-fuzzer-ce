#!/bin/sh
#This needs to be copied to /scratch/ on the ESXi target

VIF=$1
# echo "looking for dhclient running on ${VIF}"
DHCP_PID=$(ps -c | grep dhclient| grep ${VIF} | cut -f1 -d' ')
# echo "killing ${DHCP_PID}"
kill -9 $DHCP_PID
# echo "kill -9 $DHCP_PID returned $?"
# echo "destroying leases"
> /var/db/dhclient6.leases
IP=$(esxcli network ip interface ipv6 address list | grep DHCP | awk "/${VIF}/ {print \$2}")/64
if [ "$IP" == "" ]; then
    # echo "Found ${VIF} IP: ${IP}, removing"
    esxcli network ip interface ipv6 address remove -i ${VIF} -I ${IP}
fi
# echo running nohup /sbin/dhclient-uw -d -6 ${VIF}
nohup /sbin/dhclient-uw -d -6 ${VIF} > dhcp_running 2>&1&
# echo "Dhclient is running with pid $! or exited with $?"
sleep 3

