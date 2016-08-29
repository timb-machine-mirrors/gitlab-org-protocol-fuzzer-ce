FROM ubuntu:14.04

RUN apt-get update \
	&& apt-get upgrade -y \
	&& apt-get install -y curl software-properties-common \
	&& rm -rf /var/lib/apt/lists/*

RUN apt-key adv \
	--keyserver hkp://keyserver.ubuntu.com:80 \
	--recv-keys 3FA7E0328081BFF6A14DA29AA6A19B38D3D831EF

RUN add-apt-repository ppa:brightbox/ruby-ng -y

RUN echo "deb http://download.mono-project.com/repo/debian wheezy main" > /etc/apt/sources.list.d/mono-xamarin.list \
	&& apt-get update \
	&& apt-get install -y \
		build-essential \
		clang \
		doxygen \
		g++-multilib \
		libxml2-utils \
		mono-complete \
		npm \
		openjdk-7-jdk \
		python \
		ruby2.2 \
		xsltproc \
		zip \
	&& rm -rf /var/lib/apt/lists/* /tmp/*

RUN gem install bundler

RUN npm install -g \
	typescript@1.6

RUN ln -s /usr/bin/nodejs /usr/bin/node

VOLUME /build
WORKDIR /build
