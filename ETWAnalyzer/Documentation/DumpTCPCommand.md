# -Dump Tcp

When you have recorded ETW data with *Microsoft-Windows-TCPIP* ETW provider you get with ETWAnalyzer after extraction with 

```
ETWAnalyzer -extract All -fd xx.etl or 
ETWAnalyzer -extract TCP xx.etl:
```

- Number of received/sent packets per connection.
- Number of received/sent bytes per connection.
- Number of TCP retransmission events (sent and received).
    - Induced latency by summing up all retramission delays per connection.
- Invidual TCP retransmission events with time (*-ShowRetransmit*). Default is local time. It can be changed with *[-timefmt s](DumpProcessCommand.md)* or *here* to 
  WPA time or you current analyzing machine time.
- Min/Max/Median of all retransmission events (shown with *-Details*).
- Used TCP template during connection init (shown with *-Details*).

Since ETWAnalyzer is all about performance the network data is sorted by TCP retransmission event count which indicates possible network issues and is a hint
to observed application delays. 

Below is a picture which shows typical use cases where you analyze a specific extracted ETL file. The used commands were

```
EtwAnalyzer -Dump TCP -TopN 3 -NoCmdLine
EtwAnalyzer -Dump TCP -TopN 3 -NoCmdLine -Details
EtwAnalyzer -Dump TCP -TopN 3 -NoCmdLine -ShowRetransmit
```

to analyze one file, dump the top 3 connection with highest TCP retransmission count. When you show individual retransmission events with
*-ShowRetransmit* you can limit the output with *-TopNRetrans*. Individual retransmission events are sorted by time occurrence, but you can also 
sort by latency with *-SortRetransmitBy Delay*. 


![](Images/DumpTCP.png)

## Data Interpretation
When a TCP connection is initiated Windows Server editions measure the connection latency. Depending on the measured latency value and other factors Windows 
uses different TCP settings.
There are 4 possible values
- Auto
- DataCenter
- Internet
- DataCenterCustom
- InternetCustom

The default is Auto which uses for Windows Server editions DataCenter for low latency connections or Internet for the rest. The biggest
difference is the retransmission timeout which is 300ms for Internet template and 20ms for the Datacenter template. 
All client Operating systems (Windows 10/11) will always use the Internet Template. 
The TCP Template settings can be seen for existing connection with the powershell command Get-NetTCPConnection

```
PS > Get-NetTCPConnection -remoteAddress 146*

LocalAddress                        LocalPort RemoteAddress                       RemotePort State       AppliedSetting OwningProcess
------------                        --------- -------------                       ---------- -----       -------------- -------------
144.145.88.141                      7680      146.254.175.208                     57194      Established Internet       10560
144.145.88.141                      7680      146.254.175.208                     57476      Established Internet       10560
```

Get-NetTCPSetting (or netsh int tcp show supplemental template=internet) displays the corresponding tuning parameters for each network scenario.

```
PS> Get-NetTCPSetting


SettingName                     : Automatic
...

SettingName                     : Datacenter
MinRto(ms)                      : 20
InitialCongestionWindow(MSS)    : 10
CongestionProvider              : CUBIC
CwndRestart                     : False
DelayedAckTimeout(ms)           : 10
DelayedAckFrequency             : 2
MemoryPressureProtection        : Enabled
AutoTuningLevelLocal            : Normal
AutoTuningLevelGroupPolicy      : NotConfigured
AutoTuningLevelEffective        : Local
EcnCapability                   : Disabled
Timestamps                      : Disabled
InitialRto(ms)                  : 1000
ScalingHeuristics               : Disabled
DynamicPortRangeStartPort       : 49152
DynamicPortRangeNumberOfPorts   : 16358
AutomaticUseCustom              : Disabled
NonSackRttResiliency            : Disabled
ForceWS                         : Enabled
MaxSynRetransmissions           : 4
AutoReusePortRangeStartPort     : 0
AutoReusePortRangeNumberOfPorts : 0

SettingName                     : Internet
MinRto(ms)                      : 300
InitialCongestionWindow(MSS)    : 10
CongestionProvider              : CUBIC
CwndRestart                     : False
DelayedAckTimeout(ms)           : 40
DelayedAckFrequency             : 2
MemoryPressureProtection        : Enabled
AutoTuningLevelLocal            : Normal
AutoTuningLevelGroupPolicy      : NotConfigured
AutoTuningLevelEffective        : Local
EcnCapability                   : Disabled
Timestamps                      : Disabled
InitialRto(ms)                  : 1000
ScalingHeuristics               : Disabled
DynamicPortRangeStartPort       : 49152
DynamicPortRangeNumberOfPorts   : 16358
AutomaticUseCustom              : Disabled
NonSackRttResiliency            : Disabled
ForceWS                         : Enabled
MaxSynRetransmissions           : 4
AutoReusePortRangeStartPort     : 0
AutoReusePortRangeNumberOfPorts : 0
...
```

On Windows Server you can change the Template settings with *Set-NetTCPSetting* and assign specific IP addresses with *New-NetTransportFilter* a hard coded
TCP template if the automatic detection mechanism does not work for you. 
On client operating systems you cannot change the TCP template settings in a supported way (Windows 10,11). The most important setting is MinRto(ms) which defines
the minimum retransmission timeout. It is the time the TCP stack of Windows will resend packets if after the MinRto time no ACK from the receiver was returned.
If that did not work Windows will resend the missing packet with a delay of MinRto where the delay is doubled on each round until ca. 9 retransmissions have occurred.
After n failed retransmits Windows waits additionally ca. 10s and then resets the TCP connection by sending a RST packet (TcpDisconnectTcbRtoTimeout event) and close the connection on his side. 
If the connection could not be established after the initial SYN packet because no one did answer TcpConnectRestransmit events are logged which can be used to check
if invalid or not reachable hosts were tried to connect to.

## Recording Hints
The *Microsoft-Windows-TCPIP* provider traces many events which are internal to how TCP works on Windows. To record data for some minutes you need to filter out the irrelevant events.
The supplied profile https://raw.githubusercontent.com/Alois-xx/etwcontroller/refs/heads/master/ETWController/ETW/MultiProfile.wprp contains the Network profile which collects CPU sampling data, DNS and filtered network events
which should provide a good start. 
```
wpr -start MultipProfile.wprp!Network
```

## Open Points
- UDP Traffic is currently not covered although it is also traced by the TCP provider
- Transfer rates are currently also not covered by ETWAnalyzer.