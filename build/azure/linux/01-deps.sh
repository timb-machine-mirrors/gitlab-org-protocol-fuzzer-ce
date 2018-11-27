#!/bin/bash

# Linux

set -ev

sudo apt -y update

echo ls /etc/apt/sources.list.d/mono-xamarin
ls /etc/apt/sources.list.d

############################
echo *** Remove existing Mono *************************************************

sudo DEBIAN_FRONTEND=noninteractive \
  apt --yes --force-yes --auto-remove \
  purge libmono* mono-complete mono-runtime
sudo rm -rf usr/lib/mono /usr/local/bin/mono /usr/local/etc/mono /usr/local/lib/mono


############################
echo *** Remove old mono apt respository *************************************************

sudo DEBIAN_FRONTEND=noninteractive \
  apt --yes --force-yes --auto-remove \
  install ppa-purge 

sudo rm -f /etc/apt/sources.list.d/mono-official-stable*

############################
echo *** Install Mono *************************************************

sudo apt-key adv \
  --keyserver keyserver.ubuntu.com \
  --recv-keys 3FA7E0328081BFF6A14DA29AA6A19B38D3D831EF

echo "deb http://download.mono-project.com/repo/debian wheezy/snapshots/4.8.1.0 main" | sudo tee /etc/apt/sources.list.d/mono-xamarin.list
sudo apt-get update
sudo DEBIAN_FRONTEND=noninteractive apt --yes --force-yes install mono-devel=4.8.1.0-0xamarin1

############################
echo *** Install typescript *************************************************

sudo npm install -g typescript@1.7

############################
echo *** Install GCC Multilib *************************************************

sudo DEBIAN_FRONTEND=noninteractive apt --yes --force-yes install \
  gcc-multilib \
  g++-multilib

# end
