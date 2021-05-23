#!usr/bin/env python3
# -*- coding: utf-8 -*-
import sys
import os
import uuid
import cv2
import paho.mqtt.client as mqtt
import numpy as np
from logging import getLogger, config
from json import load
from datetime import datetime
import signal
import time


def on_connect(client, userdata, flag, rc):
    logger.info("Connected with result code " + str(rc))


def on_disconnect(client, userdata, rc):
    if rc != 0:
        logger.info("Unexpected disconnection.")


def on_publish(client, userdata, mid):
    logger.info("publish: {0}".format(mid))


def scheduler(arg1, arg2):
    img = np.zeros((480, 640, 3), np.uint8)
    img[:, :, 0] = 255
    name = str(uuid.uuid4()) + ".png"
    cv2.imwrite(name, img)
    im_buf = cv2.imread(name)
    utcdate = datetime.utcnow().strftime('%Y-%m-%d %H:%M:%S.%f')[:-3]
    cv2.putText(im_buf, utcdate, (10, 30), cv2.FONT_HERSHEY_PLAIN,
                1.5, (255, 255, 255), 1, cv2.LINE_AA)
    is_success, im_buf_arr = cv2.imencode(".png", im_buf)
    if is_success:
        client.publish("/putmovie", im_buf_arr.tobytes())
        os.remove(name)


def main():
    client.username_pw_set(username="rabbitmq", password="rabbitmq")
    client.on_connect = on_connect
    client.on_disconnect = on_disconnect
    client.on_publish = on_publish
    client.connect("broker.local", 1883, 60)
    client.loop_start()

    signal.signal(signal.SIGALRM, scheduler)
    # 4fps
    signal.setitimer(signal.ITIMER_REAL, 0.25, 0.25)

    try:
        while True:
            time.sleep(600)
    except KeyboardInterrupt:
        print('\nCTRL-C pressed!!')
        sys.exit()


if __name__ == '__main__':
    client = mqtt.Client()
    logger = getLogger(__name__)
    main()
