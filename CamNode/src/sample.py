#!usr/bin/env python3
# -*- coding: utf-8 -*-
import sys
import os
import uuid
import cv2
import paho.mqtt.client as mqtt
import numpy as np
from logging import getLogger, StreamHandler, Formatter, DEBUG
from json import load
from datetime import datetime
import signal
import time


def on_connect(client, userdata, flag, rc):
    logger.info("Connected with result code " + str(rc))


def on_disconnect(client, userdata, rc):
    if rc != 0:
        logger.error("Unexpected disconnection.")
        client.reconnect()


def scheduler(arg1, arg2):
    img = np.zeros((480, 640, 3), np.uint8)
    img[:, :, 0] = 255
    name = str(uuid.uuid4()) + ".png"
    # save tmp for draw timestamp
    cv2.imwrite(name, img)
    im_buf = cv2.imread(name)
    utcdate = datetime.utcnow().strftime('%Y-%m-%dT%H:%M:%S.%f')[:-3]
    cv2.putText(im_buf, utcdate, (10, 30), cv2.FONT_HERSHEY_PLAIN,
                1.5, (255, 255, 255), 1, cv2.LINE_AA)
    is_success, im_buf_arr = cv2.imencode(".png", im_buf)
    if is_success:
        client.publish("/putmovie", im_buf_arr.tobytes(), qos=0)
        os.remove(name)


def main():
    client.username_pw_set(username="rabbitmq", password="rabbitmq")
    client.on_connect = on_connect
    client.on_disconnect = on_disconnect
    client.reconnect_delay_set(min_delay=1, max_delay=120)
    client.connect("broker.local", 1883, 60)
    client.loop_start()

    signal.signal(signal.SIGALRM, scheduler)
    # 5fps
    signal.setitimer(signal.ITIMER_REAL, 0.2, 0.2)

    try:
        while True:
            time.sleep(600)
    except KeyboardInterrupt:
        print('\nCTRL-C pressed!!')
        sys.exit()


if __name__ == '__main__':
    client = mqtt.Client('camnode', clean_session=False)
    logger = getLogger(__name__)
    logger.setLevel(DEBUG)
    sh = StreamHandler(sys.stdout)
    sh.setLevel(DEBUG)
    fmt = Formatter(
        "%(asctime)s.%(msecs)03d [%(levelname)s] %(message)s", "%Y-%m-%dT%H:%M:%S")
    sh.setFormatter(fmt)
    logger.addHandler(sh)
    client.enable_logger(logger)
    main()
