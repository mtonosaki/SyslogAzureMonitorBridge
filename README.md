# SyslogAzureMonitorBridge  
Windows Service of Syslog listener and send the messages to Azure Monitor  

![](https://aqtono.com/tomarika/syslogazure/SyslogAzureMonitorBridgeIcon.png)   

## Development environment  
Visual Studio, .NET Framework 4.7.2, Windows Service  

## How to install    
 
### Register as a Windwos Service  
in windows command prompt (cmd.exe administrator mode)    
sc create SyslogAzureMonitorBridge binpath=<full path name of the SyslogAzureMonitorBridge.exe>  

### Setup your Azure environment  
todo:  

### Start the Service  
sc start SyslogAzureMonitorBridge  
