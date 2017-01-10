FROM mono:latest

RUN apt-get update \
	&& apt-get upgrade -y \
	&& apt-get install -y curl software-properties-common

RUN curl -sL https://deb.nodesource.com/setup_6.x | bash -

RUN apt-get update \
	&& apt-get install -y \
		build-essential \
		nodejs \
	&& rm -rf /tmp/*

RUN curl https://www.npmjs.com/install.sh | sh

		# clang \
		# doxygen \
		# g++-multilib \
		# libxml2-utils \
		# openjdk-7-jdk \
		# python \
		# xsltproc \
		# zip \

# RUN gem install bundler

RUN npm install -g \
	typescript@1.6

VOLUME /build
WORKDIR /build
