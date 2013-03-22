@echo off

md C:\peachfarm

echo INSTALLING...

if %PROCESSOR_ARCHITECTURE%==x86 (
  start /w erlang-x32.exe /S /D=C:\peachfarm\erlang
) else (
  start /w erlang-x64.exe /S /D=C:\peachfarm\erlang
)

set ERLANG_HOME=C:\peachfarm\erlang
start /w rabbitmq-server-3.0.4 /S /D=C:\peachfarm\rabbitmq
net stop RabbitMQ
DEL /S /F /Q "%APPDATA%\RabbitMQ\db\*"
copy rabbit.app "C:\peachfarm\rabbitmq\rabbitmq_server-3.0.4\ebin\"
net start RabbitMQ

netsh advfirewall firewall add rule name="Rabbit" dir=in action=allow protocol=TCP localport=5672
netsh advfirewall firewall add rule name="Rabbit" dir=in action=allow protocol=TCP localport=5671


if %PROCESSOR_ARCHITECTURE%==x86 (
  xcopy /s/e/I mongodb-win32-i386-2.2.3 C:\peachfarm\mongodb
) else (
  xcopy /s/e/I mongodb-win32-x86_64-2.2.3 C:\peachfarm\mongodb
)

C:\peachfarm\mongodb\mongo\mongod.exe --config C:\peachfarm\mongodb\mongod.cfg --install
net start MongoDB
C:\peachfarm\mongodb\mongo\mongo.exe admin --eval "db.addUser('beta','Beta53cr3t')"

netsh advfirewall firewall add rule name="Mongo" dir=in action=allow program="C:\peachfarm\mongodb\bin\mongod.exe"


rem xcopy /s/e/I controller C:\peachfarm\controller


echo FINISHED