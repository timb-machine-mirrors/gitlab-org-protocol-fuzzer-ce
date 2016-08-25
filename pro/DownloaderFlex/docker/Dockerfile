FROM ubuntu:14.04

RUN apt-get update \
	&& apt-get upgrade -y \
	&& apt-get install -y curl software-properties-common \
	&& rm -rf /var/lib/apt/lists/*

RUN apt-key adv \
	--keyserver hkp://keyserver.ubuntu.com:80 \
	--recv-keys 3FA7E0328081BFF6A14DA29AA6A19B38D3D831EF

RUN echo "deb http://download.mono-project.com/repo/debian wheezy main" > /etc/apt/sources.list.d/mono-xamarin.list \
	&& apt-get update \
	&& apt-get install -y \
		mono-complete \
		mono-fastcgi-server \
		nginx \
	&& rm -rf /var/lib/apt/lists/* /tmp/*

COPY etc/init.d/monoserve /etc/init.d/monoserve
COPY etc/nginx/fastcgi_params /etc/nginx/fastcgi_params
COPY etc/nginx/sites-available/default /etc/nginx/sites-available/default

VOLUME /var/www/dl.peachfuzzer.com
EXPOSE 80

#CMD [fastcgi-mono-server4 /applications=/:/var/www/dl.peachfuzzer.com/ /socket=tcp:127.0.0.1:9000]
