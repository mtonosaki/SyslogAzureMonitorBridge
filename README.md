# SyslogAzureMonitorBridge   
Windows Service of Syslog listener to send the messages to Azure Monitor  

![](https://aqtono.com/tomarika/syslogazure/SyslogAzureMonitorBridgeIcon64.png)   

## Development environment  
Visual Studio (C#)  
.NET Framework 4.7.2  
Windows Service   

## Usage  

### 1.Build & Distribute  
Open the solution (SyslogAzureMonitorBridge.sln) w/ Visual Studio.
Then Build as Release.  
To distibute this program, copy the Release folder and paste it to a target windows PC.
 
### 2.Register as a Windows Service  
Open command prompt Administrator mode. Then exec below command.  
```sc create SyslogAzureMonitorBridge binpath=<full path name of the SyslogAzureMonitorBridge.exe>```  

### 3.Setup your Azure environment  
Open windows registry editor (regedit.exe)  and find below folder  
```Computer\HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\SyslogAzureMonitorBridge```  
  
Then add command parameter to the **ImagePath** setting.  

|  Parameter  |  Description  |  Example |  Remarks |  
| ---- | ---- | ---- | ---- |  
| /n= | Table name | Syslog | Actual name in Azure Log Analytics will be  "**\<Table name\>_CL**"  |  
| /p= | Port Number of syslog listener | 514 | It is necessary to open inbound UDP access with firewall |  
| /w= | Workspace ID | | Copy it from Azure Log Analytics screen. See detail below. |  
| /k= | Key | | Copy from the same screen of Workspace ID |  

<br>  

#### To know Azure Monitor ID/Key  
Open Log Analytics in Azure Portal of ARM (Azure Resource Manager) then select **[1.Advanced Settings]** - **[2.Connected Sources]** - **[3.Windows Servers]**  
  
![](https://aqtono.com/tomarika/syslogazure/arm001.png)   
  
  
Then, copy Workspace ID - [A], Primary Key - [B]  
Paste then **[A] for /w=**,  **[B] for /k=**  
  
![](https://aqtono.com/tomarika/syslogazure/arm002.png)   

This is a sample setting to **ImagePath** setting in Registry editor.  
```C:\MyApps\Release\SyslogAzureMonitorBridge.exe /n=Syslog /p=514 /w=12345678-1234-1234-1234-123456789012 /k=12345678901234567890123456789012345678901234567890123456789012345678901234567890123456==```

<hr>  
<br>  

### 4.Start the Service  
Exec below command with Windows command prompt administrator mode.  
```sc start SyslogAzureMonitorBridge```   

### 5.Query the syslog with Azure Monitor  

_This sample is on below settings. **/n=Syslog**_ 
  
Open Log Analytics workspace in Azure Portal (ARM) then click [Logs] command in left pane.  
  
<hr>  
  
Find your Syslog table like below KQL  
```search * | distinct $table```  

You will see **Syslog\_CL** in the KQL result if the syslog data have uploaded successfully.  

<hr>  

Try to see a **Syslog\_CL** data  
```
Syslog_CL
| where EventTime_t > ago(24h)
| limit 20
| order by EventTime_t desc
```  

**Record Column**  

|  Column  |  Description  |  
| ---- | ---- |  
| TimeGenerated | Generated time at uploaded to Azure Monitor |  
| EventTime_t | Syslog received time in SyslogAzureMonitorBridge service |  
| Computer | IP address of SyslogAzureMonitorBridge service |  
| Facility\_s | Syslog facility |  
| SeverityLevel\_s | Syslog severity level |  
| HostIP\_s | Syslog owner |  
| HostName\_s | Same with _HostIP\_s_ column |  
| SyslogMessage\_s | Syslog message trimmed start "\<priority\>" part. |  

