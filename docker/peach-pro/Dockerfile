FROM mono

RUN apt-get update \
	&& DEBIAN_FRONTEND=noninteractive apt-get install -y --no-install-recommends \
		libpcap0.8 \
	&& rm -rf /var/lib/apt/lists/*

COPY bin /opt/peach
