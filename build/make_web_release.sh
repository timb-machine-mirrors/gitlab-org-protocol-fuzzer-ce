#!/bin/bash

VARIANT="web_release"
REGISTRY="10.0.1.201:5000"

set -e

requires()
{
    command -v "$1" >/dev/null 2>&1 || { echo "'$1' is required but it's not installed.  Aborting." >&2; exit 1; }
}

# packer gomes from: https://www.packer.io/downloads.html
# ovatool comes from: https://www.vmware.com/support/developer/ovf/

requires "zip"
requires "docker"
requires "packer"
requires "ovftool"

while [[ $# -gt 0 ]]
do
key="$1"

case $key in
    --debug)
        VARIANT="web_debug"
    ;;
    --buildtag)
        BUILDTAG="$2"
        shift
    ;;
    *)
        echo "USAGE: make_web_release.sh --buildtag 1.2.3 [--debug]"
        echo "Unknown Option: $key"
        exit 1
    ;;
esac
shift
done

if [ -z "$BUILDTAG" ]; then
    echo "USAGE: make_web_release.sh --buildtag 1.2.3 [--debug]"
    exit 1
fi

echo ""
echo "Verifying docker registry ${REGISTRY}"
echo ""

# Verify CA cert of private registry is correctly installed
docker pull ${REGISTRY}/peachweb >/dev/null || {
	cat <<FOO
Error communicating with registry ${REGISTRY}

Ensure the registry's CA cert is properly installed by running:

cat <<EOF > "/etc/docker/certs.d/${REGISTRY}/ca.crt"
-----BEGIN CERTIFICATE-----
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
d/IJVFdjBuUQoiHg42CuyeUvLZEmhmaq
-----END CERTIFICATE-----
EOF
FOO
    exit 1
}

echo ""
echo "Making docker container"
echo ""

pushd output/${VARIANT}/Web
docker build -t peachweb:${BUILDTAG} .
docker tag peachweb:${BUILDTAG} peachweb:latest
docker tag peachweb:${BUILDTAG} ${REGISTRY}/peachweb:${BUILDTAG}
docker tag peachweb:${BUILDTAG} ${REGISTRY}/peachweb:latest
docker push ${REGISTRY}/peachweb
popd

echo ""
echo "Making ova"
echo ""

ovadir="$(pwd)/output"
pushd web/packer

# Ensure old packer directory is clean
rm -rvf "peachweb" 2>/dev/null || {}

# Packer doesn't support '~' expansion in its cache dir variable
var="${var/#\~/$HOME}"
remote_var=""

if [ ! -z "$ESXI_PASSWORD" ]; then
    remote_var="remote_type='esx5'"
fi

PACKER_CACHE_DIR="$HOME/.packer_cache" packer build \
    -var "buildtag=${BUILDTAG}" \
    ${remote_var} \
    template.json

mv "peachweb/peachweb.ova/peachweb.ova" "${ovadir}/peachweb-${BUILDTAG}.ova"

popd

echo ""
echo "Successfully created ${ovadir}/peachweb-${BUILDTAG}.ova"
