@echo off

echo INSTALLING...

net stop RabbitMQ
rem ###Rabbit SSL CA

cd ca
md private
md certs
echo 01 > serial
echo 2>index.txt

C:\OpenSSL-Win32\bin\openssl req -x509 -config openssl.cnf -newkey rsa:2048 -days 365 -out cacert.pem -outform PEM -subj /CN=MyTestCA/ -nodes
C:\OpenSSL-Win32\bin\openssl x509 -in cacert.pem -out cacert.cer -outform DER

cd ..
md server
cd server
C:\OpenSSL-Win32\bin\openssl genrsa -out key.pem 2048
C:\OpenSSL-Win32\bin\openssl req -new -key key.pem -out req.pem -outform PEM -subj /CN=$(hostname)/O=server/ -nodes
cd ..\ca
C:\OpenSSL-Win32\bin\openssl ca -config openssl.cnf -in ..\server\req.pem -out ..\server\cert.pem -notext -batch -extensions server_ca_extensions
cd ..\server
C:\OpenSSL-Win32\bin\openssl pkcs12 -export -out keycert.p12 -in cert.pem -inkey key.pem -passout pass:MySecretPassword

cd ..
md client
cd client
C:\OpenSSL-Win32\bin\openssl genrsa -out key.pem 2048
C:\OpenSSL-Win32\bin\openssl req -new -key key.pem -out req.pem -outform PEM -subj /CN=$(hostname)/O=client/ -nodes
cd ..\ca
C:\OpenSSL-Win32\bin\openssl ca -config openssl.cnf -in ..\client\req.pem -out ..\client\cert.pem -notext -batch -extensions client_ca_extensions
cd ..\client
C:\OpenSSL-Win32\bin\openssl pkcs12 -export -out keycert.p12 -in cert.pem -inkey key.pem -passout pass:MySecretPassword

cd ..

move ca C:\peachfarm\
move server C:\peachfarm\
move client C:\peachfarm\

DEL /S /F /Q "%APPDATA%\RabbitMQ\db\*"
DEL "C:\peachfarm\rabbitmq\rabbitmq_server-3.0.4\ebin\rabbit.app"
copy rabbit.app "C:\peachfarm\rabbitmq\rabbitmq_server-3.0.4\ebin\"

rem ##END Rabbit SSL CA

net start RabbitMQ

echo FINISHED