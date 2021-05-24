FROM 2000cubits/raspbian-python:latest AS base
WORKDIR /app

RUN export DEBIAN_FRONTEND=noninteractive && apt-get update && apt-get upgrade -y
RUN export DEBIAN_FRONTEND=noninteractive && \
  apt-get install -y supervisor curl \
  python3-dev python3-pip python3-numpy python3-scipy python3-matplotlib

# for openCV
RUN export DEBIAN_FRONTEND=noninteractive && apt-get update && \
  apt-get install -y libavutil56 libcairo-gobject2 libgtk-3-0 libqtgui4 \
  libpango-1.0-0 libqtcore4 libavcodec58 libcairo2 libswscale5 libtiff5 \
  libqt4-test libatk1.0-0 libavformat58 libgdk-pixbuf2.0-0 libilmbase23 \
  libjasper1 libopenexr23 libpangocairo-1.0-0 libwebp6 libatlas-base-dev && apt-get clean all
RUN pip3 install --upgrade pip && pip3 install --upgrade setuptools && \
  pip3 install paho-mqtt && pip3 install opencv-python==4.4.0.42

# dotnet
RUN curl -sSL https://dot.net/v1/dotnet-install.sh | bash /dev/stdin
ENV PATH="$PATH:/root/.dotnet"
ENV PATH="$PATH:/root/.dotnet/tools"

# PAM
RUN sed -i -e '/pam_loginuid.so/s/^/#/' /etc/pam.d/cron

# Python Sample
COPY CamNode/supervisord.conf /etc/supervisor/conf.d/
COPY CamNode/src ./src
RUN chmod +x ./src/sample.py
ENTRYPOINT ["/usr/bin/supervisord"]