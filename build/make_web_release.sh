#!/bin/bash

VARIANT="web_release"

set -e

requires() {
    command -v "$1" >/dev/null 2>&1 || { echo "'$1' is required but it's not installed.  Aborting." >&2; exit 1; }
}

usage() {
    echo "USAGE: make_web_release.sh --registry REGISTRY --buildtag 1.2.3 [--debug]"
    echo "To access AWS, there are two options for specifying credentials:"
    echo "1. Set the AWS_ACCESS_KEY_ID and AWS_SECRET_ACCESS_KEY environment variables."
    echo '2. Use `aws configure` and then optionally use the AWS_PROFILE environment variable.'
}

# packer gomes from: https://www.packer.io/downloads.html
# ovatool comes from: https://www.vmware.com/support/developer/ovf/

requires "aws"
requires "zip"
requires "docker"
requires "packer"
requires "ovftool"

while [[ $# -gt 0 ]]; do
    key="$1"

    case $key in
        --debug)
            VARIANT="web_debug"
        ;;
        --buildtag)
            export BUILDTAG="$2"
            shift
        ;;
        --registry)
            export REGISTRY="$2"
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

if [ -z "$REGISTRY" ]; then
    echo "Missing --registry."
    echo "AWS ECR is: 318398917258.dkr.ecr.us-east-1.amazonaws.com/peachweb"
    usage
    exit 1
fi

if [ -z "$AWS_DEFAULT_REGION" ]; then
    export AWS_DEFAULT_REGION=$(aws configure get region)
    if [ -z "$AWS_DEFAULT_REGION" ]; then
        echo "Missing AWS_DEFAULT_REGION environment variable"
        usage
        exit 1
    fi
fi

if [ -z "$AWS_ACCESS_KEY_ID" ]; then
    export AWS_ACCESS_KEY_ID=$(aws configure get aws_access_key_id)
    if [ -z "$AWS_ACCESS_KEY_ID" ]; then
        echo "Missing AWS_ACCESS_KEY_ID environment variable"
        usage
        exit 1
    fi
fi

if [ -z "$AWS_SECRET_ACCESS_KEY" ]; then
    export AWS_SECRET_ACCESS_KEY=$(aws configure get aws_secret_access_key)
    if [ -z "$AWS_SECRET_ACCESS_KEY" ]; then
        echo "Missing AWS_SECRET_ACCESS_KEY environment variable"
        usage
        exit 1
    fi
fi

echo ""
echo "Making docker container"
echo ""

$(aws ecr get-login)
docker build -t peachweb output/${VARIANT}/Web
docker tag peachweb ${REGISTRY}:${BUILDTAG}
docker push ${REGISTRY}
docker rmi ${REGISTRY}:${BUILDTAG}

echo ""
echo "Running packer"
echo ""

ovadir="$(pwd)/output"
pushd web/packer

# support local and remote packer builds
if [ -z "$ESXI_PASSWORD" ]; then
    # Ensure old packer directory is clean
    rm -rvf "output-vmware-iso" 2>/dev/null || {}
    remote_var=""
else
    # Ensure old packer directory is clean
    rm -rvf "peachweb" 2>/dev/null || {}
    remote_var="-var remote_type=esx5"
fi

export PACKER_CACHE_DIR="$HOME/.packer_cache"
packer build \
    -var "buildtag=${BUILDTAG}" \
    ${remote_var} \
    template.json

if [ -z "$ESXI_PASSWORD" ]; then
    ovftool "output-vmware-iso/peachweb.vmx" "${ovadir}/peachweb-${BUILDTAG}.ova"
else
    mv "peachweb/peachweb.ova/peachweb.ova" "${ovadir}/peachweb-${BUILDTAG}.ova"
fi

popd

echo ""
echo "Successfully created peachweb-${BUILDTAG}"
