---V1.0.0

# 1.SCPI 命令简介

SCPI 指令是可编程仪器标准命令（Standard Commands for Programmable Instruments)的简称，它实际定义了一套用于控制可编程测量仪器的标准语法和命令。SCP 命令通过 ASCII 字符串形式在命令与仪器进行交互。命令一般由一系列的关键字构成，有的还需要包括参数，在协议中，命令规定为如下形式：*IDN?。在使用中，即可以写全名，也可以仅写仅包含大写字母的缩写。通常仪器指令可以分为控制命令和查询命令，控制命令没有返回值，可通过命令 SYSTem:ERRor?查询执行的结果是否正确，查询命令有返回值，其返回的内容也是 ASCII 字符串。

## 1.1 命令格式

命令由关键字和参数组成，关键字之间用冒号“:”分隔，关键字后面跟随可选的参数设置。 若命令行后面有“？”，表示查询功能。关键字和第一个参数之间要以空格分开。

例如：

控制命令 `CHANnel1:BWLimit 20M`第一级关键字是 CHANnel，第二级关键字是 BWLimit。各级关键字也是以“:”分割，20M 是参数，与关键字以空格分开。

查询命令 `CHANnel1:BWLimit?`问号“?”表示查询。

## 1.2 符号说明

以下符号不随命令发送。

1. 竖线  | 

竖线用于分隔多个参数选项，使用命令时必须选择其中一个参数。

2. 方括号 []

方括号中的内容是可省略的。

3. 三角括号 <>

三角括号中的参数必须用一个有效值来替换。 

## 1.3 命令缩写

所有命令对大小写不敏感，你可以全部采用大写或小写。但是如果要缩写，必须输完命令格式中的所有大写字母。

例如：`CHANnel:VALUe?`可以缩写成`CHAN:VALU?`

## 1.4 结尾符

SCPI 指令必须附带指令结尾符，结尾符可选其中一个(不包含双引号):“\r\n”, “\r”, “\n”或“\0”。在有些串口通讯工具软件中，提供“发送换行符”的选项，勾选上即表示软件会自动发送结尾符。

# 2.指令

## 2.1 IEEE488.2 共同指令

#### 清除寄存器标志 ：*CLS 

|**参数**|无|
|:----|:----|
|**返回值**|无|
|**说明**|清除下面的寄存器:a. 标准事件寄存器；b. 查询事件寄存器；c. 操作事件寄存器；e. 状态字节寄存器；f. 错误队列。|
|**示例**|发送：*CLS 返回：无|

#### 仪器标识查询 ：*IDN?

|**参数**|无|
|:----|:----|
|**返回值**|仪表标识|
|**说明**|仪器标识查询，返回的数据分 4 个部分:厂家型号产品序列号设备ID和软件版本号|
|**示例**|发送：*IDN?返回：ADDITEL,P27-const,123456,P27&EPU_LP_V1.0.0.23|

#### 状态复位 ：*RST

|**参数**|无|
|:----|:----|
|**返回值**|无|
|**说明**|将仪器恢复至出厂预设的默认状态清除当前测量设置（恢复默认量程、默认触发模式）|
|**示例**|发送：*RST 返回：无|

## 2.2 压力控制

#### 读取实时压力值 ：PRESsure? <UnquoStr>

|**参数**|<UnquoStr>用于指定获取不同气源的实时压力，具体如下：Pressure：获取的正压气源Vacuum：获取的真空气源参数为空：同时获取正压气源和真空气源|
|:----|:----|
|**返回值**|指定的压力值和单位，|
|**说明**|读取指定的压力值(设备首页展示的压力值)，支持读取正压、真空压力单位固定kPa不写参数时返回全部压力值|
|**示例**|读取气源的实时压力值发送：PRESsure? Pressure返回：5.655,kPa发送：PRESsure? Vacuum返回：5.655,kPa发送：PRESsure? 返回：6.605,kPa,12.408,kPa|

#### 获取指定气源控制状态：PRESsure:CONTrol? <UnquoStr>

|**参数**|<UnquoStr>用于获取不同气源的控制状态，具体如下:Pressure：正压Vacuum：真空|
|:----|:----|
|**返回值**|当前控制状态 1为运行，0为停止|
|**说明**|获取【正压/真空】气源控制状态|
|**示例**|读取正压气源当前控制状态发送：PRESsure:CONTrol? Pressure返回：1|

#### 设置指定气源控制状态 ：PRESsure:CONTrol<UnquoStr>,<state>

|**参数**|<UnquoStr>用于指定不同气源的控制状态，具体如下：Pressure ：正压Vacuum：真空<state>：控制状态 1为运行，0为停止|
|:----|:----|
|**返回值**|无|
|**说明**|设置【正压/真空】气源控制状态|
|**示例**|设置真空气源控制状态为运行发送：PRESsure:CONTrol Vacuum,1返回：无|

#### 获取指定气源压力单位 ：UNIT? <UnquoStr>

|**参数**|<UnquoStr>用于指定获取不同气源的压力单位，具体如下：Pressure ：正压Vacuum：真空|
|:----|:----|
|**返回值**|指定气源的压力单位|
|**说明**|读取指定气源的压力单位，支持读取正压、真空正压：kPa,MPa,psi,bar,kgf/cm2真空：**hPa**,kPa,MPa,psi,bar,kgf/cm2|
|**示例**|读取正压气源当前压力单位发送：UNIT?  Pressure返回：psi|

#### 设置指定气源压力单位 ：UNIT<UnquoStr>,<unit>

|**参数**|<UnquoStr>用于设置不同气源的压力单位，具体如下：Pressure ：正压Vacuum：真空<unit>：**hPa** | kPa | MPa | psi | bar | kgf/cm2|
|:----|:----|
|**返回值**|无|
|**说明**|设置指定气源的压力单位，支持配置正压、真空正压：kPa,MPa,psi,bar,kgf/cm2真空：**hPa**,kPa,MPa,psi,bar,kgf/cm2|
|**示例**|设置正压气源压力单位发送：UNIT? Pressure,MPa返回：无|

#### 获取指定气源压力范围 ：PRESsure:RANGe? <UnquoStr>

|**参数**|<UnquoStr>用于指定获取不同气源的压力控制范围，具体如下：Pressure ：正压Vacuum：真空|
|:----|:----|
|**返回值**|下限压力值:上限压力值|
|**说明**|读取指定气源的造压范围，支持读取正压、真空返回数据单位：kpa|
|**示例**|读取正压气源造压范围发送：PRESsure:RANGe? Pressure返回：10.00:120.00|

#### 设置指定气源压力范围  ：PRESsure:RANGe <UnquoStr>,<min>:<max>

|**参数**|<UnquoStr>用于设置不同气源的压力控制范围，具体如下：Pressure ：正压Vacuum：真空<min>: 下限压力值<max>:上限压力值|
|:----|:----|
|**返回值**|无|
|**说明**|设置指定气源的造压范围，支持配置正压、真空上下限入参压力值，单位：kpa有效范围：正压[0:8500] 真空[0:100]|
|**示例**|设置正压气源造压范围发送：PRESsure:RANGe Pressure,0:8500返回：无|

## 2.3校准指令

#### 读取指定气源传感器的校准记录 ：CALibration:DATA:VALue? <ModuleID>,<password>,<type>

|**参数**|<ModuleID>：Pressure：正压Vacuum：真空<password>用户校准或厂家校准密码123456：用户校准<type>单点校准或多点校准0：单点1：多点|
|:----|:----|
|**返回值**|校准点2个，返回格式如下：校准标准值A:B，原始压力值A:B，年，月，日|
|**说明**|读取指定气源多点(2点)校准记录 ，包含正压气源和真空气源单位：kpa|
|**示例**|读取正压气源 [多点] 校准数据发送：CALibration:DATA:VALue? Pressure,123456,1返回：0:200,0.01:200.01,2026,4,7读取正压气源 [单点] 校准数据发送：CALibration:DATA:VALue? Pressure,123456,0返回：0,0.01,2026,4,24|

#### 写入指定气源传感器的校准数据 ：CALibration:DATA:VALue <ModuleID>,<password>,<count>,<points>,<values>,<year>,<month>,<day>

|**参数**|<ModuleID>：Pressure：正压Vacuum：真空<password>：用户校准或厂家校准密码密码：123456<count>：2（设备目前规格2点）<points>：外部输入压力值，用冒号（:）隔开<values>：设备原始压力值，用冒号（:）隔开<year>,<month>,<day>：校准年，月，日|
|:----|:----|
|**返回值**|无|
|**说明**|写入指定气源多点(2点)校准数据 ，包含正压气源和真空气源单位：kpa|
|**示例**|写入正压气源 [多点] 校准数据发送：CALibration:DATA:VALue Pressure,123456,2,0.00:200.00,0.01:200.01,2026,4,7返回：无写入正压气源 [单点] 校准数据CALibration:DATA:VALue Pressure,123456,1,0,0.01,2026,4,24返回：无|


#### 重置校准数据 ：CALibration:DATA:RESet <ModuleID>,<password>，<type>

|**参数**|<ModuleID>：Pressure：正压Vacuum：真空<password>用户校准或厂家校准密码123456：用户校准<type>单点校准或多点校准0：单点1：多点|
|:----|:----|
|**返回值**|校准点2个[A,B]，返回格式如下：校准标准值A:B，原始压力值A:B，年，月，日|
|**说明**|读取指定气源多点(2点)校准记录 ，包含正压气源和真空气源单位：kpa|
|**示例**|重置正压气源多点校准数据发送：CALibration:DATA:RESet? Pressure,123456,1返回：0:200,0.01:200.01,2026,4,7|


## 2.4 系统指令

#### 读取设备生产厂家 ：SYSTem:MFR?

|**参数**|无|
|:----|:----|
|**返回值**|设备生产厂家|
|**说明**|读取设备生产厂家|
|**示例**|发送：SYSTem:MFR?返回：ConST|


#### 读取设备型号 ：SYSTem:MODel?

|**参数**|无|
|:----|:----|
|**返回值**|设备型号|
|**说明**|读取设备型号|
|**示例**|发送：SYSTem:MODel?返回：171A|

#### 读取设备序列号 ：SYSTem:SN?

|**参数**|无|
|:----|:----|
|**返回值**|设备序列号|
|**说明**|读取设备序列号|
|**示例**|发送：SYSTem:SN?返回：121212|

#### 读取设备版本 ：SYSTem:VERSion?

|**参数**|BOOT|DM|HARD|FIRM|无|
|:----|:----|
|**返回值**|设备版本|
|**说明**|参数BOOT, 获取bootloader版本信息参数DM，获取显示模块版本信息参数HARD，获取硬件版本信息参数FIRM，获取固件版本信息不加参数，按照上面顺序以逗号隔开排列获取版本信息|
|**示例**|发送：SYSTem:VERSion? BOOT返回：BOOT=2.1.0发送：SYSTem:VERSion? DM返回：EPU_DM_V1.0.1.11发送：SYSTem:VERSion? HARD返回：EPU-LP V1.0发送：SYSTem:VERSion? FIRM返回：EPU_LP_V1.0.0.15发送：SYSTem:VERSion?返回：BOOT=2.1.0，EPU_DM_V1.0.1.11，EPU-LP V1.0，EPU_LP_V1.0.0.15|

#### 读取MCU与PC串口波特率：SYSTem:RS232:INFo?

|**参数**|无|
|:----|:----|
|**返回值**|返回当前RS232参数，参数之间‘,’隔开<BaudRate>波特率, 可取值为9600/19200/38400/57600/115200<DataBits>数据位, 可取值为7/8/9<StopBits>停止位,可取值为None,One,Two,OnePointFive<Parity>校验位None，无校验Odd,奇校验Even，偶校验|
|**说明**|读取设备设备与PC串口波特率|
|**示例**|发送：SYSTem:RS232:INFo?返回：9600,8,One,None|

#### 设置MCU与PC串口波特率 ：SYSTem:RS232:INFo <BaudRate>,<DataBits>,<StopBits>,<Parity>

|**参数**|<BaudRate>波特率, 可取值为9600/19200/38400/57600/115200<DataBits>数据位, 可取值为7/8/9<StopBits>停止位,可取值为None,One,Two,OnePointFive<Parity>校验位None，无校验Odd,奇校验Even，偶校验|
|:----|:----|
|**返回值**|无|
|**说明**|设置设备与PC串口波特率|
|**示例**|发送：SYSTem:RS232:INFo 115200,8,One,None返回：无|

#### 读取SCPI指令错误内容：SYSTem:ERRor?

|**参数**|无|
|:----|:----|
|**返回值**|错误信息无错误：0,"No error"有错误：120,"Command parameter error;PRESsure:ZERO"|
|**说明**|读取SCPI指令错误内容|
|**示例**|发送：SYSTem:ERRor?返回：0,"No error"|

#### 切换到主界面：SYSTem:HOMe

|**参数**|无|
|:----|:----|
|**返回值**|无|
|**说明**|将设备 UI 切换到主界面。使用此指令时，请确保设备处于空闲状态，没有任何正在进行中的测试和校准任务。|
|**示例**|发送：SYSTem:HOMe返回：无|

#### 查询锁屏状态：SYSTem:LOCK?

|**参数**|无|
|:----|:----|
|**返回值**|返回当前锁屏是否启用0：未启用1：已启用|
|**说明**|无|
|**示例**|查询当前锁屏状态，返回1，表示当前已锁屏发送：SYSTem:LOCK?返回：1|

#### 设置锁屏状态：SYSTem:LOCK <State>

|**参数**|<State>设置当前锁屏是否启用0：未启用1：已启用|
|:----|:----|
|**返回值**|无|
|**说明**|无|
|**示例**|设置当前锁屏状态为已锁屏发送：SYSTem:LOCK 1返回：无|

#### 设备重启：SYSTem:RESTart

|**参数**|无|
|:----|:----|
|**返回值**|无|
|**说明**|无|
|**示例**|发送：SYSTem:RESTart返回：无|

#### 恢复出厂：SYSTem:RESet <Password>

|**参数**|参数**<Password>**密码：123456|
|:----|:----|
|**返回值**|无|
|**说明**|无|
|**示例**|发送：SYSTem:RESet 123456返回：无|

#### 读取当前系统声音启用状态：SYSTem:SOUNd? 

|**参数**|无|
|:----|:----|
|**返回值**|当前系统声音启用状态0：未启用1：启用|
|**说明**|无|
|**示例**|发送：SYSTem:SOUNd? 返回：1|

#### 设置当前系统声音启用状态：SYSTem:SOUNd <Value>

|**参数**|<State>设置当前系统声音是否启用0：未启用1：已启用|
|:----|:----|
|**返回值**|无|
|**说明**|无|
|**示例**|设置当前系统声音为启用发送：SYSTem:SOUNd 1返回：无|

#### 读取当前系统亮度：SYSTem:BRIG?

|**参数**|无|
|:----|:----|
|**返回值**|当前系统亮度值，取值范围0-100|
|**说明**|无|
|**示例**|发送：SYSTem:BRIGhtness?返回：70|

#### 设置当前系统亮度：SYSTem:BRIG<Value>

|**参数**|<Value>亮度值，取值范围0-100|
|:----|:----|
|**返回值**|无|
|**说明**|无|
|**示例**|设置当前系统亮度值为80发送：SYSTem:BRIGhtness 80返回：无|

#### 读取当前语言：SYSTem:LANGuage?

|**参数**|无|
|:----|:----|
|**返回值**|返回当前语言en-US：英语 (English)zh-CN： 简体中文|
|**说明**|无|
|**示例**|发送：SYSTem:LANGuage?返回：zh-CN|

#### 设置当前语言：SYSTem:LANGuage <Language>

|**参数**|<Language>en-US：英语 (English)zh-CN： 简体中文|
|:----|:----|
|**返回值**|无|
|**说明**|无|
|**示例**|设置当前语言为英文发送：SYSTem:LANGuage en-US返回：无|


#### 

# 3.指令附表

指令附表1，SCPI单位Id列表

|单位Id|**单位**|
|:----|:----|
|2000|文本单位|
|32767|空单位|
| | |
|1211|mA|
|1212|μA|
|1209|A|
|1240|V|
|1241|mV|
|1281|Ω|
|1284|kΩ|
|1283|MΩ|
|1000|K|
|1001|℃|
|1002|℉|
|1003|°R|
|999|°Re|
|1005|°|
|1342|%|
| | |
|1133|kPa|
|1130|Pa|
|1131|GPa|
|1132|MPa|
|1134|mPa|
|1135|μPa|
|1136|hPa|
|1137|bar|
|1138|mbar|
|1139|torr|
|1140|atm|
|1141|psi|
|1142|psia|
|1143|psig|
|1144|gf/cm2|
|1145|kgf/cm2|
|1147|inH2O@4C|
|1148|inH2O@68F|
|1150|mmH2O@4C|
|1151|mmH2O@20C|
|1153|ftH2O@4C|
|1154|ftH2O@68F|
|1156|inHg@0C|
|1158|mmHg@0C|
|2001|mtorr|
|2002|lb/ft2|
|2003|tsi|
|2004|psf|
|2005|inH2O@60F|
|2006|ftH2O@60F|
|2007|cmH2O@4C|
|2008|mH2O@4C|
|2009|cmHg@0C|
|2010|mHg@0C|
|2011|kgf/m2|
|2012|oz/in2|
|2013|cmH2O@68F|
|2014|mH2O@68F|
|2015|mmH2O@15C|
|2020|inH2O@39F|
|2021|inH2O@68F|
|2022|ftH2O@39F|
|2023|ftH2O@68F|
|2024|inHg@60F|
|2025|mmH2O@39F|
|2026|mmH2O@68F|
|2027|cmH2O@39F|
|2028|cmH2O@20C|
|2029|mH2O@39F|
|2030|mH2O@20C|
|2998|m|
|2999|ft|

 


  

 

指令附表2 错误定义

  

|**序号**|**错误码**|**错误描述**|**说明**|
|:----|:----|:----|:----|
|1   |0|No error|无错误|
|**指令错误**||||
| 2   |120|Commandparameter error|指令参数错误|
| 3   |-108|Parameter not allowed|参数太多，或不带参数的指令里带了参数|
| 4   |-109|Missing parameter|缺少参数|
| 5   |-110|Command header error|指令头错误|
| 6   |-114|Header suffix out of range|指令头的后缀超范围|
| 7   |-123|Numeric overflow|数字溢出，数字的指数绝对值大于43|
| 8   |-151|Invalid string data|无效的字符串，例如引号不匹配|
| 9   |-171|Invalid expression|无效的表达式，例如括号不匹配|
|**执行错误**||||
|10  |-200|Execution error|执行错误|
|11  |-221|Settings conflict|设置冲突|
|12  |-222|Data out of range|参数值超出指令的有效范围|
|13  |-223|Too much data|数据太多而超出处理能力|
|14  |-224|Illegal parameter value|非法参数值|
|15  |-230|Data corrupt or stale|数据无效，或正在读取数据中，还未获得有效数据|
|16  |-240|Hardware error|硬件故障|
|17  |-256|File name not found|没有找到文件名|
|18  |-282|Illegal program name|非法的程序名|
|19  |220|Measure error|测量错误|
|20  |221|Failed to set meaure function|切换测量项失败|
|21  |222|Failed to read measure value|读取测量值失败|
|22  |223| | |
|23  |224| | |
|24  |240|Control error|控制错误|
|25  |241| | |
|26  |242| | |
|27  |243| | |
|28  |260|Calibration error|校准错误|
|29  |261|Calibration secured|设备处于校准保护状态，不能执行校准|
|30  |262|Invalid calibration secure code|无效的校准密码|
|31  |263|Missing calibration value|电流/电压校准时，没有设置校准点的情况下设置校准值，会发生此错误|
|32  |264|Missing calibration data|连续设置校准点，而没有设置校准值，会发生此错误|
|33  |265|Failed to set calibration function|设置校准项失败|
|34  |266|Calibration data is not enough|在保存校准数据时，如果校准数据没有达到3个点，会发生此错误|
|35  |271|Setion_name_not_found|没有找到段名|
|36  |272|Key_name_not_found|没有找到键名|
|37  |291|Update secured|设备处于升级保护状态，不能升级|
|38  |292|Invalid update secure code|无效的升级密码|
|39  |293|Not found the service pack|没有找到升级包|
|40  |294|The service pack unavailable|升级包不可用|
|41  |295|AppUpdate not found|没找到AppUpdate.exe|
|**设备相关错误**||||
|42  |-310|System error|系统错误|
|43  |-311|Memory error|内存错误|
|44  |-350|Queue overflow|错误队列溢出|
|45  |-360|Communication error|通信错误|
|46  |301|Internal module is not connected|未连接内部模块|
|47  |302|External module is not connected|未连接外部模块|
|48  |303|Supply module is not connected|未连接正压模块|
|49  |304|Vacuum module is not connected|未连接负压模块|
|50  |361|Open WLAN Failed|打开WIFI失败|
|51  |362|Set WLAN address mode failed|设置WIFI地址模式失败|
|52  |363|Set WLAN address failed|设置WIFI地址失败|
|53  |364|Communication port to WIFI module is not open|与WIFI模块的通信端口没有打开|
|54  |365|WLANisnotconnected|WIFI未连接|

  


