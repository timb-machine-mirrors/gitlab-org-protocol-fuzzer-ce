
import rest_target
from flask import Flask, request
from flask_restful import Resource, Api, abort, reqparse
import sqlite3, logging, logging.handlers
#from OpenSSL import SSL
import ssl

logger = logging.getLogger(__name__)

PrivateKeyFile = "yourserver.key"
CertificateFile = "yourserver.crt"

if __name__ == '__main__':
    logger.setLevel(logging.DEBUG)
    
    logFormatter = logging.Formatter("%(asctime)s [%(levelname)-5.5s] %(message)s")
    syslogHandler = logging.handlers.SysLogHandler()
    syslogHandler.setFormatter(logFormatter)
    logger.addHandler(syslogHandler)
    
    consoleHandler = logging.StreamHandler()
    consoleHandler.setFormatter(logFormatter)
    logger.addHandler(consoleHandler)
    
    fileHandler = logging.FileHandler('rest_ssl_target.log')
    fileHandler.setFormatter(logFormatter)
    logger.addHandler(fileHandler)

    logger.info("rest_target.py initializing.")
    rest_target.CreateDb()
    logger.info("Starting REST application")

    rest_target.app.run(debug=True, host="0.0.0.0", ssl_context=(CertificateFile,PrivateKeyFile))
