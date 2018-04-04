#!/bin/bash

set -ex

. build/common.sh

usage() {
    echo "USAGE: make_packer_release.sh --buildtag 1.2.3 --filesdir /path/to/files"
}

# packer comes from: https://www.packer.io/downloads.html
# ovatool comes from: https://www.vmware.com/support/developer/ovf/

requires "packer"
if [ -z $ESXI_PASSWORD ]; then
    requires "ovftool"
fi


while [[ $# -gt 0 ]]; do
    key="$1"

    case $key in
        --buildtag)
            export BUILDTAG="$2"
            shift
        ;;
        --except)
            export NO_BUILD="$2"
            shift
        ;;
        --filesdir)
            export FILES_DIR="$2"
            shift
        ;;
        *)
            usage
            echo "Unknown Option: $key"
            exit 1
        ;;
    esac
    shift
done
if [ -z "$BUILDTAG" ]; then
    echo "Missing --buildtag."
    usage
    exit 1
fi

echo ""
echo "Running packer"
echo ""

if [ -z "$FILES_DIR" ]; then
    echo "Missing --filesdir"
    usage
    exit 1
fi

ovadir="$(pwd)/output"
pushd packer

ESXI_PASSWORD=""

# support local and remote packer builds
if [ -z "$ESXI_PASSWORD" ]; then
    # Ensure old packer directory is clean
    rm -rvf "output-vmware-iso ${ovadir}/peach-targetvm-${BUILDTAG}.ova" 2>/dev/null || {}
    remote_var=""
else
    # Ensure old packer directory is clean
    rm -rvf "peach-targetvm" 2>/dev/null || {}
    remote_var="-var remote_type=esx5"
fi

# Ensure tmp directory for .tar files is clean
rm -rvf "tmp" 2>/dev/null || {}
mkdir tmp

# If packer is unable to connect over VNC to the esxi server
# ensure that the firewall settings did not get reset
# https://nickcharlton.net/posts/using-packer-esxi-6.html

export AWS_DEFAULT_REGION="us-west-1"
export PACKER_CACHE_DIR="/root/.packer_cache"

packer build \
    -var "buildtag=${BUILDTAG}" \
    -var "ami_name_suffix=test" \
    -var "files_dir=${FILES_DIR}" \
    ${remote_var} \
    template.json

if [ -z "$ESXI_PASSWORD" ]; then
    ovftool "output-vmware-iso/peach-targetvm.vmx" "${ovadir}/peach-targetvm-${BUILDTAG}.ova"
else
    mv "peach_targetvm/peach-targetvm.ova/peach-targetvm.ova" "${ovadir}/peach-targetvm-${BUILDTAG}.ova"
fi

popd

echo ""
echo "Successfully created peach-targetvm-${BUILDTAG}"
