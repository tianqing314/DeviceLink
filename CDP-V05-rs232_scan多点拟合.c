/************************************************************
  Copyright (C), 2006-2007, CONST  Ltd.

  File name:    rs232_scan.C  

  Author:研发部  Version:V1.0     Date:2006-03-08

  Description:解析，扫描并处理串口通信指令     

  Others: 在本文件中，使用的全局变量有：
          
  Function List: 
  1. unsigned char Dns_Instruction(void);//解析指令
  2. void scanserial(unsigned char index);//扫描指令，并处理
  
  History: 
  2013-9-24  V02.04   
             1、调整设置波特率命令BOAUQ，当设置为38400时，模块自动变为高速模式.
             2、模块测量模式标志位掉电不保存，即上电恢复为普通模式。
             3、升级版本号为V02.04
  2013-10-25 V02.04
             修正迁移量程后，不更改单位，仍然保持原来量程单位。
  			 修正SPEED参数改变后重启AD避免1分钟数据不变
  			 增加TAG指令
  2013-11-20 V02.04.1
             修正写TAG指令，参数错误问题
  2014-04-19 V02.04.2
             由于811在38400波特率自动上传模式下与模块的串口命令交互有错误，测试后认为
             是AD中断处理中指令延时过长，现在由6us改为0us
  2014-04-21 V02.04.3
             增加串口下载功能
             增加OCODE指令功能与OTYPE相同，兼容其他表的指令
  2014-05-05 V02.05
             发布新版本
             修正写TAG指令，参数错误问题
             调整AD中断指令延时，由6us改为0us
             增加串口在线升级固件功能
             增加OCODE指令功能与OTYPE相同，兼容其他表的指令
  2014-09-10 V02.06
             发布新版本
             增加温度滤波，解决由温度传感器误差引起的压力输出值在0.001%FS跳到问题
             增加兼容串口示波器的自动发送指令
  2014-12-20 V02.07
             将可选单位信息改为所有单位全支持，解决老2005主机不兼容的问题
  2014-12-20 V05.01  
             在V02.07基础上升级到大的版本号到V05
             调整温补函数，改为多点线性拟合
             增加读校准数据指令
             其他程序与V02.07一致，目前只针对于使用ALLSENSOR30psi传感器的模块

             note:读写ODATE OTYPE OCODE ODCAL有时不返回
             
    
************************************************************/
#include "common.h"
#include "out_variable.h"
typedef struct //指令表
{
    unsigned char index;
    char rw;
    char command[9];
    unsigned char sum;
}COMMAND_STRUCT;
const unsigned char command_sum = 90;
const COMMAND_STRUCT command_format[90] = 
{
    //Rs232.station = 1
    {0,'W',"OAV",1},//写入放大倍数(0-7),返回ok
    {1,'R',"OAV",0},//读出放大倍数（1-128）
    {2,'W',"OIS",1},//写入恒流（1-10）
    {3,'R',"OIS",0},//读出恒流（1-10）
    {4,'W',"OIA",1},//输入电流，仪表自动确定放大倍数，之后连续返回放大倍数和ad数值
    {5,'R',"OIA",0},//仪表连续返回放大倍数和AD数值
    {6,'W',"OIAS",1},//判断所设定的电流和放大倍数是否存储
    
    {7,'W',"ORS",0},//开始确定量程
    {8,'W',"ORANZ",1},//加最小压力，从PC写入量程0点值，返回量程0点数值和AD测量数值
    {9,'W',"ORANF",1},//加最大压力，从PC写入量程满度值，返回量程满度数值和AD测量数值
    {10,'W',"ORKB",2},//写入，k,b系数
    {11,'R',"ORKB",0},//读出K，B系数
    {12,'W',"OROK",0},//退出量程设定，保存量程
    {13,'W',"ORNO",0},//退出量程设定，不保存量程
    
    {14,'W',"OPOYS",0},//进入设置拟合系数参数模式
    {15,'W',"OPOYCOE",5},//写入拟合系数,参数1:系数起始序号,参数2:系数个数,参数3:第一组2个系数
    //,参数2:第二组2个系数,参数5:第三组2个系数，系数以ASCII码的16进制表示
    //不足18个系数的空格补足，超出6个系数的分多次写
    {16,'R',"OPOYCOE",2},//读出拟合系数,参数1:系数起始序号,参数2:系数个数,参数3:第一组2个系数
    //,参数2:第二组2个系数,参数5:第三组2个系数，系数以ASCII码的16进制表示
    //不足18个系数的空格补足，超出6个系数的分多次读
    {17,'W',"OPOYPRA",2},//写标压力阶数np,温度阶数nt
    {18,'R',"OPOYPRA",0},//读标压力阶数np,温度阶数nt
    {19,'W',"OPOYOK",1},//退出设置拟合系数模式,保存拟合系数,拟合计算标志生效
    
    {20,'R',"OPOYSAT",0},//读是否有有效的拟合系数
    {21,'W',"OPOYCL",1},//=1拟合系数生效，=0拟合系数取消
    
    {22,'W',"ONOP3",0},//无效指令
    {23,'W',"ONOP4",0},//无效指令
    {24,'W',"ONOP5",0},//无效指令
    {25,'W',"ONOP6",0},//无效指令
    {26,'W',"ONOP7",0},//无效指令
    
    {27,'W',"OLS",0},//开始线形修正
    {28,'W',"OLSUM",1},//设定修正点的数目,返回修正点数目
    {29,'R',"OLSUM",0},//返回修正点数目
    {30,'W',"OLNR",2},//设定修正点号和修正点对应的压力，返回AD实际测得的压力数值
    {31,'R',"OLRAM",1},//读修正存储区某修正点的存储数值，返回存储数值
    {32,'W',"OLRAM",3},//写入某点的理论数值和实际数值
    {33,'W',"OLSND",1},//设定修正点的组号,返回修正点号
    {34,'W',"OPRES",1},//设定修正点的压力，返回修正点的压力
    {35,'W',"OLAD",1}, //由PC独立写入已设修正点的实际压力，返回实际压力
    {36,'W',"OLSAV",0},//不保存修正数据，退出
    {37,'W',"OLE",0},//设置线形修正完毕，退出线形修正
    
    {38,'W',"OCS",0},//开始校准
    {39,'R',"MCPAZ",1},//读取零点压力
    {40,'R',"MCPAM",1},//读取中间点压力//  
    {41,'R',"MCPAF",1},//读取满度压力
    {42,'W',"OCOK",0},//退出校准，保存校准数据
    {43,'W',"OCNO",0},//退出校准，不保存数据 
    
    {44,'W',"OFALT",0},//恢复出厂设置
    {45,'R',"ORAN",0},//读量程，返回0点数值,满度数值,传感器类型,仪表准确度 
    {46,'W',"MWORK",1},//选择工作模式
    {47,'R',"MWORK",0},//读取工作模式
    {48,'R',"ODATE",0},// 读生产日期，返回生产日期
    {49,'W',"ODATE",1},//写生产日期
    {50,'R',"OTYPE",0},//读仪器编号，返回仪器编号
    {51,'W',"OTYPE",1},//写仪器编号，编号组成：仪表型号--编号，例如2004PRESS-001
    {52,'R',"MRMD",0}, //读测量数据，返回测量数据
    {53,'R',"MRMN",0},//读无任何修正的数据
    {54,'R',"OADD",0},//读仪器地址，返回仪器地址
    {55,'W',"OADD",1},//写仪器地址
    {56,'R',"OTEMP",0},//读温度，返回当前环境温度
    {57,'R',"OVER",0},//读软件版本号
    {58,'R',"OUNIT",0},//读允许使用单位信息
    {59,'W',"OUNIT",1},//直接写入单位
    {60,'R',"OSTAT",0},//读温补，线形，校准标志状态
    {61,'W',"OZERO",0},//清零
    {62,'W',"OCRAN",3},//设置量程迁移数值,单位以kpa为准
    {63,'W',"OSRAN",1},//启动量程迁移
    {64,'W',"OCFS",0},//开始厂家校准
    {65,'W',"OCF",2},//厂家校准标准点
    {66,'W',"OCFOK",1},//厂家校准成功，退出
    {67,'W',"OCFCL",1},//取消厂家校准
    {68,'W',"OCONT",1},//仪表连续发出测量数
    {69,'W',"MRATE",1},//7,15,30,60,120,240
    {70,'W',"OBAUQ",1},//写入波特率
    {71,'W',"OSENS",1},//设定传感器类型
    {72,'W',"OACCY",1},//设定仪表准确度，参数有0.2，0.1，0.05.0.02四个等级
    {73,'W',"SPEED",1},//设定压力数据输出速度=0低速=1高速
    {74,'R',"SPEED",0},//读压力数据输出速度=0低速=1高速
    {75,'R',"ODCAL",0},//读校准日期
    {76,'W',"ODCAL",1},//写校准日期
    {77,'R',"OHVER",0},//读硬件版本号
    {78,'W',"ERAS",1},//擦除铁电指令，输入密码：211273 
    {79,'W',"ORPP",0},//仪表软复位
    {80,'R',"TAG",1},//读TAG
    {81,'W',"TAG",2},//写TAG
    {82,'R',"OCODE",0},//读仪器编号，返回仪器编号
    {83,'W',"OCODE",1},//写仪器编号，编号组成：仪表型号--编号，例如2004PRESS-001
    {84,'R',"ORIV",0},//读传感器的激励电流或电压，传感器的mV输出
    
    {85,'W',"OCRAM",3},//写用户校准信息 校准点号，标准压力，测量压力
    {86,'R',"OCRAM",1},//读用户校准信息 校准点号，标准压力，测量压力，单位为kPa
    {87,'W',"OFRAM",3},//写厂家校准信息 校准点号，标准压力，测量压力
    {88,'R',"OFRAM",1},//读用户校准信息 校准点号，标准压力，测量压力，单位为kPa
    {89,'R',"OCALI",0},//读校准状态 1/0(厂家已校准):1/0(用户已校准):2/3(当前几点校准生效)
    
    
};
/*
1000  接受缓冲区溢出
1001  越级操作
1002  没有这个放大倍数
1003  没有这个恒流
1004  数字字符串含非法字符
1005  单位不合法
1006  量程非法
1007  参数不正确
1008  仪表没有做过量程标定
1009  温补总数超范围
1010  温补参考点超范围
1011  温补点号超范围
1012  线性点号超范围
1013  线性点总数超范围
1014  工作模式错误
1015  仪表编号超长
1016  数据不可以清0
1017  参数个数不够
1018  没有这个指令
1019  操作码错误
1020  r/w错误
1021  文件号超范围
1022  电池检测温度值太低
1023  压力单位名称错误
1024  不正确的压力单位
1025  串口地址超范围1-255
1026  波特率不正确
1027  24v关断时间参数不正确
1028  标签号不对
1029  参数超长
1030  数据不在清零范围
*/

void Scanserial(unsigned char index)//扫描指令，并处理
{
    signed char i,j;
    unsigned char over_limit = 0;//过限标志
    long x,x1;
    float y,y1;
    
    switch (index)
    {
    case (0):////{0,'W',"OAV",1}
        {
            if (Math_String(Serialorder.data0)) { Send_Error("1004");}
            else
            {
                x = String_To_Long(Serialorder.data0); 
                if (x == 1 || x == 2 || x == 4 || x == 8 || x == 16 || x == 32 || x == 64|| x==128)
                {
                    AD.av = x;
                    AD_Restart();
                    _DINT();
                    AD_Down();
                    _EINT();
                    Send_String('F',"OK","","","","");
                }
                else
                {
                    Send_Error("1002");
                }
            }    
        }break;
        
    case (1)://{1,'R',"OAV",0}
        {
            Long_To_String(AD.av,Serialorder.data0);
            Send_String('F',Serialorder.data0,"","","","");
        }break;
        
    case (2)://{2,'W',"OIS",1}//改写此命令，只用来设置恒流和恒压：<10恒流,=30恒压
        { 
            if (Math_String(Serialorder.data0)) { Send_Error("1004");}
            else
            {
                x = String_To_Long(Serialorder.data0);            
                if (x > 0 && x < 10)Pressure.range.i_adj = x;
                else Pressure.range.i_adj = 30;  
                Pressure.range.i_adj = x;
                _DINT();
                Pressure_Down();
                _EINT();
                Send_String('F',"OK","","","","");
            } 
        }break;
        
    case (3)://{3,'R',"OIS",0}//改写此命令，只用来读恒流和恒压：<10恒流,=30恒压
        {
            Long_To_String(Pressure.range.i_adj,Serialorder.data0);
            Send_String('F',Serialorder.data0,"","","","");
        }break;
        
    case (4)://{4,'W',"OIA",1}
        {
            if (Math_String(Serialorder.data0)) { Send_Error("1004");}
            else
            {
                x = String_To_Long(Serialorder.data0);            
                if (x > 0 && x < 10)Pressure.range.i_adj = x;
                else Pressure.range.i_adj = 30;
                Auto_ia_flag = 1;
            }  
        }break;
        
    case (5): //{5,'R',"OIA",0}
        {
            Auto_ia_flag = 2;   
        }break;
        
    case (6): //{6,'W',"OIAS",1}        
        {
            if (Math_String(Serialorder.data0)) { Send_Error("1004");}
            else
            {
                x = String_To_Long(Serialorder.data0);            
                if (x > 2)Send_Error("1007");
                else
                {
                    Auto_ia_save = x;
                }
            }
        }break;
        
    case (7)://{7,'W',"ORS",0}
        {
            if (Rs232.station == 0)
            {
                Rs232.station = 2;
                comm[0] = P_calib.ok;
                P_calib.ok = 0;
                Pressure.offset = 0;
                comm[1] = Pressure.range.ok;
                Pressure.range.ok = 0;
                comm[2] = Pressure.range.unit;
                Pressure.range.unit = 0;
                comm[3] = Pressure.unit;
                Pressure.unit = 0;
                comm[4] = Pressure.work_type;
                Pressure.work_type = 0;
                Poyfit.ok=0;
                Send_String('F',"OK","","","","");           
            }
            else
            {
                over_limit = 1;
            }
        }break;
    case (8)://{8,'W',"ORANZ",1},
        {
            if (Rs232.station == 2)
            {
                if ( Math_String(Serialorder.data0) ) Send_Error("1004");
                else
                {
                    Pressure.range.min_p = String_To_Float(Serialorder.data0);
                    Pressure.range.min_orig = Sameple_value;
                    if (Pressure.range.max_orig - Pressure.range.min_orig != 0)
                    {  
                        Pressure.range.faultcoek = (Pressure.range.max_p - Pressure.range.min_p) /
                            (Pressure.range.max_orig - Pressure.range.min_orig); 
                    }
                    Pressure.range.faultcoeb = Pressure.range.max_p - Pressure.range.max_orig * Pressure.range.faultcoek; 
                    Long_To_String(Pressure.orig_value,Serialorder.data1);
                    Send_String('F',Serialorder.data0,Serialorder.data1,"","","");
                }
            }
            else
            {
                over_limit = 1;
            }
        }break;
    case (9)://{9,'W',"ORANF",1},
        {
            if (Rs232.station == 2)
            {
                if ( Math_String(Serialorder.data0) ) Send_Error("1004");
                else
                {
                    Pressure.range.max_p = String_To_Float(Serialorder.data0);
                    Pressure.range.max_orig = Sameple_value;
                    if (Pressure.range.max_orig - Pressure.range.min_orig != 0)
                    {  
                        Pressure.range.faultcoek = (Pressure.range.max_p - Pressure.range.min_p) /
                            (Pressure.range.max_orig - Pressure.range.min_orig); 
                    }
                    Pressure.range.faultcoeb = Pressure.range.max_p - Pressure.range.max_orig * Pressure.range.faultcoek;
                    Long_To_String(Pressure.orig_value,Serialorder.data1);
                    Send_String('F',Serialorder.data0,Serialorder.data1,"","","");
                }
            }
            else
            {
                over_limit = 1;
            }
        }break;
    case (10)://{10,'W',"ORKB",2}
        {
            if (Rs232.station == 2)
            {
                if ( Math_String(Serialorder.data0) && Math_String(Serialorder.data1)) Send_Error("1004");
                else
                {
                    Pressure.range.faultcoek = String_To_Float(Serialorder.data0);
                    Pressure.range.faultcoeb = String_To_Float(Serialorder.data1);
                    Send_String('F',"OK","","","","");
                }
            }
            else
            {
                over_limit = 1;
            }
        }break;
    case (11):// {11,'R',"ORKB",0}  
        {
            Float_To_Science(Pressure.range.faultcoek,Serialorder.data0);
            Float_To_Science(Pressure.range.faultcoeb,Serialorder.data1);
            Send_String('F',Serialorder.data0,Serialorder.data1,"","","");
        }break;
    case (12)://{12,'W',"OROK",0}
        {
            if (Rs232.station == 2)
            {
                Pressure.range.unit = KPA;
                if (Pressure.range.max_p - Pressure.range.min_p <= 0) {Send_Error("1006"); goto RANGE_ERR;}
                else if (Pressure.range.max_p - Pressure.range.min_p < 1.0)Pressure.unit = PA;
                else if (Pressure.range.max_p - Pressure.range.min_p >= 1000.0)Pressure.unit = MPA;
                else  Pressure.unit = KPA;
                Pressure.offset = 0;
                Pressure.work_type = 0;
                P_calib.ok = 0;
                calib_select = 0;
                Pressure.range.ok = 1; 
                Poyfit.ok=0;
                Pressure_Unitselect();
                Pressure.dot_len=Data_Format(Pressure.unit);
                _DINT();
                Press_F_Calib_Down();
                Pressure_Down();
                PoyfitOK_Down();
                _EINT();
                Send_String('F',"OK","","","","");
                Rs232.station = 0;
            }
            else
            {
                over_limit = 1;
            }
        RANGE_ERR:_NOP();
        }break;
    case (13)://{13,'W',"ORNO",0}
        {
            if (Rs232.station == 2)
            {
                P_calib.ok = comm[0];
                Pressure.range.ok = comm[1];
                Pressure.range.unit = comm[2];
                Pressure.unit = comm[3];
                Pressure.work_type = comm[4];
                _DINT();
                Pressure_Load_Zero();
                PoyfitOK_Load();
                _EINT();
                Send_String('F',"OK","","","","");
                Rs232.station = 0;
            }
        }break;
    case (14):// {14,'W',"OPOYS",0},//进入设置拟合系数参数模式
        {
            if (Rs232.station == 0)
            {
                if (Pressure.range.ok == 0)
                {
                    Send_Error("1008");
                }
                else
                {
                    Rs232.station = 3;
                    Poyfit.ok = 0;
                    comm[0]=Pressure.work_type;
                    Pressure.work_type = 0;
                    comm[1]=P_calib.ok;
                    P_calib.ok = 0;
                    Pressure.offset = 0;
                    Send_String('F',"OK","","","","");
                }
            }
            else
            {
                over_limit = 1;
            }
        }break;
    case (15):// {15,'W',"OPOYCOE",5},//写入拟合系数,参数1:系数起始序号,参数2:系数个数,参数3:第一组2个系数
        //,参数2:第二组2个系数,参数5:第三组2个系数，系数以ASCII码的16进制表示
        //超出6个系数的分多次写
        {
            if (Rs232.station == 3)
            {
                x = String_To_Long(Serialorder.data0);
                if(x<1||x>36)
                {
                    Send_Error("1007");
                    return;
                }
                x=x-1;//对应数组下标
                x1 = String_To_Long(Serialorder.data1);
                if(x1<1||x1>6)
                {
                    Send_Error("1007");
                    return;
                }
                for(i=0,j=0;i<x1;i++)
                {
                    Poyfit.coeff[i+x]=Ascii_Hex_To_Double(&Serialorder.data2[j]);
                    j=j+8;
                    if(j>=16)
                    {
                        break;
                    }
                }
                i++;
                for(j=0;i<x1;i++)
                {
                    Poyfit.coeff[i+x]=Ascii_Hex_To_Double(&Serialorder.data3[j]);
                    j=j+8;
                    if(j>=16)
                    {
                        break;
                    }
                }
                i++;
                for(j=0;i<x1;i++)
                {
                    Poyfit.coeff[i+x]=Ascii_Hex_To_Double(&Serialorder.data4[j]);
                    j=j+8;
                    if(j>=16)
                    {
                        break;
                    }
                }
                Send_String('F',"OK","","","","");
            }
            else
            {
                over_limit = 1;
            }
        }break;
    case (16):// {16,'R',"OPOYCOE",2},//读出拟合系数,参数1:系数起始序号,参数2:系数个数,参数3:第一组2个系数
        //,参数2:第二组2个系数,参数5:第三组2个系数，系数以ASCII码的16进制表示
        //超出6个系数的分两次读
        {
            x = String_To_Long(Serialorder.data0);
            if(x<1||x>36)
            {
                Send_Error("1007");
                return;
            }
            x=x-1;//对应数组下标
            x1 = String_To_Long(Serialorder.data1);
            if(x1<1||x1>6)
            {
                Send_Error("1007");
                return;
            }
            Long_To_String(0,Serialorder.data2);
            Long_To_String(0,Serialorder.data3);
            Long_To_String(0,Serialorder.data4);
            for(i=0,j=0;i<x1;i++)
            {
                Double_To_Ascii_hex(Poyfit.coeff[i+x],&Serialorder.data2[j]);
                j=j+8;
                if(j>=16)
                {
                    break;
                }
            }
            i++;
            for(j=0;i<x1;i++)
            {
                Double_To_Ascii_hex(Poyfit.coeff[i+x],&Serialorder.data3[j]);
                j=j+8;
                if(j>=16)
                {
                    break;
                }
            }
            i++;
            for(j=0;i<x1;i++)
            {
                Double_To_Ascii_hex(Poyfit.coeff[i+x],&Serialorder.data4[j]);
                j=j+8;
                if(j>=16)
                {
                    break;
                }
            }
            Send_String('F',Serialorder.data0,Serialorder.data1,Serialorder.data2,Serialorder.data3,Serialorder.data4); 
        }break;
    case (17)://{17,'W',"OPOYPRA",2}写标压力阶数np,温度阶数nt
        {
            if (Rs232.station == 3)
            {
                x=String_To_Long(Serialorder.data0);
                x1=String_To_Long(Serialorder.data1);
                if((x+1)*(x1+1)>36)
                {
                    Send_Error("1007");
                    return;
                }
                Poyfit.np=x;
                Poyfit.nt=x1;
                Send_String('F',"OK","","","","");
            }
            else
            {
                over_limit = 1;
            }
        }break;
    case (18)://{18,'R',"OPOYPRA",0}读标压力阶数np,温度阶数nt
        {
            Long_To_String(Poyfit.np,Serialorder.data0);
            Long_To_String(Poyfit.nt,Serialorder.data1);
            Send_String('F',Serialorder.data0,Serialorder.data1,"","","");
        }break;
    case (19)://{19,'W',"OPOYOK",1}
        {
            if (Rs232.station == 3)
            {
                x = String_To_Long(Serialorder.data0);
                if((x!=0)&&(x!=1))
                {
                    Send_Error("1007"); //参数不正确
                    return;
                }
                if(x==1)
                {
                    Pressure.work_type = 0;
                    P_linear.ok=0;
                    P_calib.ok = 0;
                    calib_select = 0;
                    Pressure.offset = 0;
                    Poyfit.ok=1;
                    _DINT();
                    Poyfit_Down();
                    Pressure_Down();
                    Press_F_Calib_Down();
                    _EINT();
                }
                else
                {
                    _DINT();
                    Poyfit_Load();
                    Pressure.work_type = comm[0];
                    P_calib.ok = comm[1];
                    Pressure_Load_Zero();
                    _EINT();
                }
                Send_String('F',"OK","","","","");
                Rs232.station = 0;
            }
            else
            {
                over_limit = 1;
            }
        }break; 
    case (20)://{20,'R',"OPOYSAT",0}
        {
            Long_To_String(Poyfit.ok,Serialorder.data0);
            Send_String('F',Serialorder.data0,"","","","");
        }break;
    case (21)://{21,'W',"OPOYCL",0},//=1拟合系数生效，=0拟合系数取消
        {
            x = String_To_Long(Serialorder.data0);
            if((x!=0)&&(x!=1))
            {
                Send_Error("1007"); //参数不正确
                return;
            }
            if(Poyfit.ok!=x)
            {
                Pressure.work_type=0;
                P_linear.ok=0;
                P_calib.ok=0;
                calib_select = 0;
                Pressure.offset=0;
                Poyfit.ok=x;
                _DINT();
                PoyfitOK_Down();
                Pressure_Down();
                Press_F_Calib_Down();
                _EINT();
            }
            Send_String('F',"OK","","","","");
        }break;
    case (22):
    case (23):
    case (24):
    case (25):
    case (26):
        {
            Send_String('F',"OK","","","","");
        }break;
    case (27)://{27,'W',"OLS",0}
        {
            if (Rs232.station == 0)
            {
                Rs232.station = 4;
                //Pressure.unit = Pressure.range.unit;
                Pressure.offset = 0;
                Pressure.work_type = 0;
                P_calib.ok = 0;
                P_linear.ok = 0;
                Send_String('F',"OK","","","","");      
            }
            else
            {
                over_limit = 1;
            }
        }break;
    case (28)://{28,'W',"OLSUM",1}
        {
            if (Rs232.station == 4)
            {
                x = String_To_Long(Serialorder.data0);
                if (x < 3 || x > 60 )Send_Error("1013");
                else
                {
                    P_linear.sum = x;
                    Send_String('F',"OK","","","","");
                }
            }
            else
            {
                over_limit = 1;
            }
        }break;
    case (29)://{29,'R',"OLSUM",0}
        {
            Long_To_String(P_linear.sum,Serialorder.data0);
            Send_String('F',Serialorder.data0,"","","","");
        }break;
    case (30)://{30,'W',"OLNR",2}
        {
            if (Rs232.station == 4)
            {
                x = String_To_Long(Serialorder.data0);
                if (x < 1 || x > P_linear.sum )Send_Error("1012");
                else
                {
                    if (Math_String(Serialorder.data1))Send_Error("1004");
                    else
                    {
                        P_linear.dot[x - 1] = String_To_Float(Serialorder.data1);
                        P_linear.value[x - 1] = Pressure.final_value;
                        
                        if ((Pressure.range.max_p - Pressure.range.min_p) >= 1000.0)Float_To_String(Pressure.final_value,Serialorder.data0,8,2);
                        else if((Pressure.range.max_p - Pressure.range.min_p) < 10.0)Float_To_String(Pressure.final_value,Serialorder.data0,8,6);
                        else Float_To_String(Pressure.final_value,Serialorder.data0,8,4);
                        strcat(Serialorder.data0,"KPA");
                        
                        Float_To_String(Env_temp_value,Serialorder.data1,5,2);
                        strcat(Serialorder.data1, "℃");
                        
                        Send_String('F',Serialorder.data0,Serialorder.data1,"","","");
                    }   
                }
            }
            else
            {
                over_limit = 1;
            }  
        }break;
    case (31)://{31,'R',"OLRAM",1}
        {
            x = String_To_Long(Serialorder.data0);
            if (x < 1 || x > P_linear.sum )Send_Error("1012");
            else
            {
                if ((Pressure.range.max_p - Pressure.range.min_p) >= 1000.0)Float_To_String(P_linear.value[x - 1],Serialorder.data1,8,2);
                else if((Pressure.range.max_p - Pressure.range.min_p) < 10.0)Float_To_String(P_linear.value[x - 1],Serialorder.data1,8,6);
                else Float_To_String(P_linear.value[x - 1],Serialorder.data1,8,4);
                strcat(Serialorder.data1,"KPA");
                
                if ((Pressure.range.max_p - Pressure.range.min_p) >= 1000.0)Float_To_String(P_linear.dot[x - 1],Serialorder.data0,8,2);
                else if((Pressure.range.max_p - Pressure.range.min_p) < 10.0)Float_To_String(P_linear.dot[x - 1],Serialorder.data0,8,6);
                else Float_To_String(P_linear.dot[x - 1],Serialorder.data0,8,4);
                strcat(Serialorder.data0,"KPA");
                
                Send_String('F',Serialorder.data0,Serialorder.data1,"","","");
            }
        }break;
    case (32)://{32,'W',"OLRAM",3}
        {
            if (Rs232.station == 4)
            {
                x = String_To_Long(Serialorder.data0);
                if (x < 1 || x > P_linear.sum )Send_Error("1012");
                else
                {
                    if (Math_String(Serialorder.data1) || Math_String(Serialorder.data2))Send_Error("1004");
                    else
                    {
                        P_linear.dot[x - 1] = String_To_Float(Serialorder.data1);
                        P_linear.value[x - 1] = String_To_Float(Serialorder.data2);
                        Send_String('F',"OK","","","","");
                    }
                }
            }
            else
            {
                over_limit = 1;
            }
        }break;
    case (33)://{33,'W',"OLSND",1}
        {
            if ( Math_String(Serialorder.data0) )Send_Error("1001");
            else
            {
                x = String_To_Long(Serialorder.data0);
                if (x > P_linear.sum)Send_Error("1012");
                else
                {
                    P_linear.point = x;
                    Send_String('F',"OK","","","","");
                }
            }   
        }break;
    case (34)://{34,'W',"OPRES",1}
        {
            if (Rs232.station == 4)
            { 
                if ( Math_String(Serialorder.data0) )Send_Error("1001");
                else
                {
                    P_linear.dot[P_linear.point - 1] = String_To_Float(Serialorder.data0); 
                    Send_String('F',"OK","","","","");
                }
            }
            else
            {
                over_limit = 1;
            }
        }break;
    case (35)://{35,'W',"OLAD",1}
        {
            if (Rs232.station == 4)
            { 
                if ( Math_String(Serialorder.data0) )Send_Error("1001");
                else
                {
                    P_linear.value[P_linear.point - 1] = String_To_Float(Serialorder.data0); 
                    Send_String('F',"OK","","","","");
                }
            }
            else
            {
                over_limit = 1;
            }
        }break;
    case (36)://{36,'W',"OLSAV",0}
        {
            if (Rs232.station == 4)
            { 
                _DINT();
                Pressure_Load();
                _EINT();
                Send_String('F',"OK","","","","");
            }
            else
            {
                over_limit = 1;
            }
        }break;
    case (37)://{37,'W',"OLE",0}
        {
            if (Rs232.station == 4)
            { 
                P_linear.ok = 1;
                calib_select = 0;
                
                _DINT();
                Press_F_Calib_Down();
                Pressure_Down();
                _EINT();
                Rs232.station = 0;
                Send_String('F',"OK","","","","");
            }
            else
            {
                over_limit = 1;
            }
        }break;
        
    case (38)://{38,'W',"OCS",0}
        {
            if (Rs232.station == 0)
            {
                Rs232.station = 7;
                P_calib.ok = 0;
                Pressure.offset = 0;
                Send_String('F',"OK","","","","");           
            }
            else
            {
                over_limit = 1;
            }
        }break;
    case (39)://{39,'R',"MCPAZ",1}
        {
            if (Rs232.station == 7)
            {
                if ( Math_String(Serialorder.data0) )Send_Error("1001");
                else
                {
                    y = String_To_Float(Serialorder.data0);
                    if (P_change_range_flag == 0) 
                    {
                        if (Pressure.range.max_p > 0 && Pressure.range.min_p < 0) x = 0;
                        else x= 1;
                    } 
                    else
                    {
                        if (P_change_range_max > 0 && P_change_range_min < 0) x = 0;
                        else x= 1;
                    }
                    
                    P_calib.dot[x] = y;
                    P_calib.value[x] = Pressure.final_value;
                    if (P_change_range_flag == 0) 
                    {
                        if (Pressure.range.max_p >= 100000.0)Float_To_String(Pressure.final_value,Serialorder.data0,7,1);
                        else Float_To_String(Pressure.final_value,Serialorder.data0,9,4);
                    }
                    else
                    {
                        if (P_change_range_max >= 100000.0)Float_To_String(Pressure.final_value,Serialorder.data0,7,1);
                        else Float_To_String(Pressure.final_value,Serialorder.data0,9,4);
                    }
                    Send_String('F',"OK",Serialorder.data0,"KPA","","");
                }
            }
            else
            {
                over_limit = 1;
            }
        }break;
    case (40)://{40,'R',"MCPAM",1}
        {
            if (Rs232.station == 7)
            {
                if ( Math_String(Serialorder.data0) )Send_Error("1001");
                else
                {
                    P_calib.dot[1] = String_To_Float(Serialorder.data0);
                    P_calib.value[1] = Pressure.final_value;
                    if (P_change_range_flag == 0) 
                    {
                        if (Pressure.range.max_p >= 100000.0)Float_To_String(Pressure.final_value,Serialorder.data0,7,1);
                        else Float_To_String(Pressure.final_value,Serialorder.data0,9,4);
                    }
                    else
                    {
                        if (P_change_range_max >= 100000.0)Float_To_String(Pressure.final_value,Serialorder.data0,7,1);
                        else Float_To_String(Pressure.final_value,Serialorder.data0,9,4);
                    }
                    Send_String('F',"OK",Serialorder.data0,"KPA","","");
                }
            }
            else
            {
                over_limit = 1;
            }
        }break;
    case (41)://{41,'R',"MCPAF",1}
        {
            if (Rs232.station == 7)
            {
                if ( Math_String(Serialorder.data0) )Send_Error("1001");
                else
                {
                    P_calib.dot[2] = String_To_Float(Serialorder.data0);
                    P_calib.value[2] = Pressure.final_value;
                    if (P_change_range_flag == 0) 
                    {
                        if (Pressure.range.max_p >= 100000.0)Float_To_String(Pressure.final_value,Serialorder.data0,7,1);
                        else Float_To_String(Pressure.final_value,Serialorder.data0,9,4);
                    }
                    else
                    {
                        if (P_change_range_max >= 100000.0)Float_To_String(Pressure.final_value,Serialorder.data0,7,1);
                        else Float_To_String(Pressure.final_value,Serialorder.data0,9,4);
                    }
                    Send_String('F',"OK",Serialorder.data0,"KPA","","");
                }
            }
            else
            {
                over_limit = 1;
            }
        }break;
    case (42)://{42,'W',"OCOK",0}
        {
            if (Rs232.station == 7)
            {
                calib_select |= USER_CAL_OK;
                P_calib.ok = 0x01;
                _DINT();
                Pressure_Down();
                _EINT();
                Rs232.station = 0;
                Send_String('F',"OK","","","","");
            }
            else
            {
                over_limit = 1;
            }
        }break;
    case (43)://{43,'W',"OCNO",0}
        {
            if (Rs232.station == 7)
            {
                _DINT();
                Pressure_Load_Zero();
                _EINT();
                
                if((calib_select&USER_CAL_OK)==USER_CAL_OK)//有用户校准，则用户校准生效
                {
                    _DINT();
                    Press_Calib_Load();
                    _EINT();
                }
                else if(calib_select==FACTORY_CAL_OK)//无用户校准，有厂家校准，则厂家校准生效
                {
                    _DINT();
                    Press_F_Calib_Load();
                    _EINT();
                }
                else//无用户校准，也无厂家校准，都不恢复，置P_calib.ok = 0;
                {
                    P_calib.ok = 0;
                    calib_select = 0;
                }
                _DINT();
                Pressure_Load_Unit();
                _EINT();
                
                Rs232.station = 0;
                Send_String('F',"OK","","","","");
            }
            else
            {
                over_limit = 1;
            }
        }break;
        
    case (44)://{44,'W',"OFALT",0}
        {		
            if (P_calib.ok == 1)
            {
                Pressure.offset = 0;
                P_calib.ok = 0;
                _DINT();
                Pressure_Down();
                _EINT();
                calib_select&=~USER_CAL_OK;
                if(calib_select==FACTORY_CAL_OK)//有厂家校准则厂家校准生效
                {
                    _DINT();
                    Press_F_Calib_Load();
                    _EINT();
                }
            }
            else
            {
                _NOP();
            }
            Send_String('F',"OK","","","",""); 
        }break;
        
    case (45)://{45,'R',"ORAN",0}
        {
            if (P_change_range_flag == 0)
            {
                if (Pressure.range.max_p >= 100000.0)Float_To_String(Pressure.range.min_p,Serialorder.data0,7,1);
                else Float_To_String(Pressure.range.min_p,Serialorder.data0,9,4);
                strcat(Serialorder.data0,"KPA");
                
                if (Pressure.range.max_p >= 100000.0)Float_To_String(Pressure.range.max_p,Serialorder.data1,7,1);
                else Float_To_String(Pressure.range.max_p,Serialorder.data1,9,4);
                strcat(Serialorder.data1,"KPA");
                
                if (Pressure.range.sensor_type == 1)Serialorder.data2[0] = 'A';
                else if (Pressure.range.sensor_type == 2)Serialorder.data2[0] = 'D';
                else Serialorder.data2[0] = 'G';
                Serialorder.data2[1] = 0;  
                Long_To_String(Pressure.range.accuracy, Serialorder.data3);
                Send_String('F',Serialorder.data0,Serialorder.data1,Serialorder.data2,Serialorder.data3,"");
            }
            else
            {
                y = P_change_range_min*UNIT_CONV[P_change_range_unit].math_coe;
                y1 = P_change_range_max*UNIT_CONV[P_change_range_unit].math_coe;
                if (y1 >= 100000.0)Float_To_String(y,Serialorder.data0,7,1);
                else Float_To_String(y,Serialorder.data0,9,4);
                strcat(Serialorder.data0,UNIT_CONV[P_change_range_unit].str);
                
                if (y1 >= 100000.0)Float_To_String(y1,Serialorder.data1,7,1);
                else Float_To_String(y1,Serialorder.data1,9,4);
                strcat(Serialorder.data1,UNIT_CONV[P_change_range_unit].str);
                //Send_String('F',Serialorder.data0,Serialorder.data1,"","","");
                
                if (Pressure.range.sensor_type == 1)Serialorder.data2[0] = 'A';
                else if (Pressure.range.sensor_type == 2)Serialorder.data2[0] = 'D';
                else Serialorder.data2[0] = 'G';
                Serialorder.data2[1] = 0;  
                Long_To_String(Pressure.range.accuracy, Serialorder.data3);
                Send_String('F',Serialorder.data0,Serialorder.data1,Serialorder.data2,Serialorder.data3,"");
            }
        }break;
        
    case (46)://{46,'W',"MWORK"}
        {
            x = String_To_Long(Serialorder.data0);
            if (x < 0 || x > 3) Send_Error("1007");
            else
            {
                if (x == 0)
                {
                    Pressure.work_type = 0;
                    Pressure.offset = 0;
                    _DINT();
                    Pressure_Down();
                    _EINT();
                    Send_String('F',"OK","","","","");
                }
                else if (x == 1)
                {
                    Pressure.work_type = 1;
                    Pressure.offset = 0;
                    _DINT();
                    Pressure_Down();
                    _EINT();
                    Send_String('F',"OK","","","","");
                }
                else if (x == 2)
                {
                    if (P_linear.ok) 
                    {
                        Pressure.work_type = 2;
                        Pressure.offset = 0;
                        _DINT();
                        Pressure_Down();
                        _EINT();
                        Send_String('F',"OK","","","","");
                    }
                    else Send_Error("1014");
                }
                else 
                {
                    if (P_linear.ok) 
                    {
                        Pressure.work_type = 3;
                        Pressure.offset = 0;
                        _DINT();
                        Pressure_Down();
                        _EINT();
                        Send_String('F',"OK","","","","");
                    }
                    else Send_Error("1014");
                }
            }
        }break;
        
    case (47)://{47,'R',"MWORK",0}
        {
            Long_To_String(Pressure.work_type,Serialorder.data0);   
            Send_String('F',Serialorder.data0,"","","","");
        }break;
        
    case (48)://{48,'R',"ODATE",0}
        {
            Send_String('F',(char *)Instrument_time,"","","","");
        }break;
        
    case (49)://{49,'W',"ODATE",1}
        {  
            for (i = 0;i <20 ;i++)
            {
                if (Serialorder.data0[i] == 0) i = 30;
            }
            if (i < 30) Send_Error("1015");
            else
            {
                Instrument_time[0] = 0;
                strcat((char *)Instrument_time,Serialorder.data0);
                _DINT();
                Comm_Down();
                _EINT();
                Send_String('F',"OK","","","","");
            }
        }break; 
        
    case (50)://{50,'R',"OTYPE",0}
    case (82)://{82,'R',"OCODE",0} 
        {
            Send_String('F',(char *)Instrument_code,"","","","");
        }break;
        
    case (51)://{51,'W',"OTYPE",1}
    case (83)://{83,'W',"OCODE",1}
        {
            for (i = 0;i <20 ;i++)
            {
                if (Serialorder.data0[i] == 0) i = 30;
            }
            if (i < 30) Send_Error("1015");
            else
            {
                Instrument_code[0] = 0;
                strcat((char *)Instrument_code,Serialorder.data0);
                _DINT();
                Comm_Down();
                _EINT();
                Send_String('F',"OK","","","","");
            }
        }break;
        
    case (52)://{52,'R',"MRMD",0}
        {
            if (Pressure.range.ok == 1)
            {
                i=Pressure.dot_len;
                if(Pressure.final_value>AbsMaxRangeValue())//超过量程后，实时计算小数点
                {
                    i=Compare_bit(Pressure.now_value,BIT_LEN);
                }
                Float_To_String(Pressure.now_value,Serialorder.data0,BIT_LEN,i);
                Send_String('F',Serialorder.data0,(char *)UNIT_CONV[Pressure.unit].str,"","","");
            }
            else
            {
                Long_To_String(Pressure.orig_value,Serialorder.data0);
                Send_String('F',Serialorder.data0,"","","","");
            }
        }break;
        
    case (53)://{53,'R',"MRMN",0}
        {
            if (Pressure.range.ok == 1)
            {
                if (P_change_range_flag == 0) j = Pressure.range.unit;
            	else j = P_change_range_unit;
                i=Data_Format(j);
                if(Pressure.final_value>AbsMaxRangeValue())//超过量程后，实时计算小数点
                {
                    i=Compare_bit(Pressure.basic_value,BIT_LEN);
                }
                Float_To_String(Pressure.basic_value,Serialorder.data0,BIT_LEN,i);
                Send_String('F',Serialorder.data0,(char *)UNIT_CONV[j].str,"","","");
            }
            else
            {
                Long_To_String(Pressure.orig_value,Serialorder.data0);
                Send_String('F',Serialorder.data0,"","","","");
            }
        }break;
        
    case (54)://{54,'R',"OADD",0}
        {
            Long_To_String(Rs232.addr,Serialorder.data0);
            Send_String('F',Serialorder.data0,"","","","");
        }break;
        
    case (55)://{55,'W',"OADD",1}
        {
            x  = String_To_Long(Serialorder.data0);
            if (x <= 0 || x > 255)
            {
                Send_Error("1025");
            }
            else
            {
                if (Rs232.addr != x)
                {
                    Rs232.addr = x;
                    _DINT();
                    Rs232_Down();
                    _EINT();    
                }
                Send_String('F',"OK","","","","");
            } 
        }break;
        
    case (56)://{56,'R',"OTEMP",0}
        {
            Float_To_String(Env_temp_value,Serialorder.data0,4,2);
            strcat (Serialorder.data0,"℃");
            Send_String('F',Serialorder.data0,"","","","");
        }break;  
        
    case (57)://{57,'R',"OVER",0}
        {
            Send_String('F',(char *)SOFT_VER,"","","","");
        }break;
        
        case(58)://{58,'R',"OUNIT",0}
            {
                if (P_change_range_flag == 0)
                {  
                    Long_To_String(Pressure.range.unit_select,Serialorder.data0);
                    Send_String('F',Serialorder.data0,"","","","");
                }
                else
                {
                    Long_To_String(P_change_range_unit_select,Serialorder.data0);
                    Send_String('F',Serialorder.data0,"","","","");
                }
            }break;  
            
    case (59)://{59,'W',"OUNIT",1}
        {
            x  = String_To_Long(Serialorder.data0);
            if (x < 1 || x > 12)
            {
                Send_Error("1024");
            }
            else
            {
                if (P_change_range_flag == 0) x1 = Pressure.range.unit_select;
                else x1 = P_change_range_unit_select;
                if ( ( x1 >> (12-x) ) & 0x01 )
                {
                    Pressure.unit = x;
                    Pressure.dot_len=Data_Format(Pressure.unit);
                    _DINT();
                    Pressure_Down();
                    Tare_Down();
                    _EINT();
                    Send_String('F',"OK","","","","");
                }
                else Send_Error("1024");
            }
        }break;
        
    case (60)://{60,'R',"OSTAT",0}
        {
            i = 0;
            if (P_calib.ok) i |= 0x01;
            //if (P_temper.ok) i |= 0x02;
            if (P_linear.ok) i |= 0x04;
            if (calib_select&0x01)i|=0x08;
            if (calib_select&0x10)i|=0x10;
            Serialorder.data0[0] = 0x30 + i;
            Serialorder.data0[1] = 0;
            Send_String('F',Serialorder.data0,"","","","");
        }break;  
        
    case (61)://{61,'W',"OZERO",0}
        {
            if (P_change_range_flag == 0)
            {
                if ((fabs)(Pressure.final_value + Pressure.offset) <= (Pressure.range.max_p - Pressure.range.min_p) / 100)
                {
                    Pressure.offset = Pressure.final_value + Pressure.offset;
                    _DINT();
                    Pressure_Down_Zero();
                    _EINT();
                    Send_String('F',"OK","","","","");
                }
                else Send_Error("1030"); 
                
            }
            else
            {
                if ((Pressure.final_value + Pressure.offset) <= (P_change_range_max - P_change_range_min) / 100)
                {
                    Pressure.offset = Pressure.final_value + Pressure.offset;
                    _DINT();
                    Pressure_Down_Zero();
                    _EINT();
                    Send_String('F',"OK","","","","");
                }
                else Send_Error("1030");
            }
        }break;
        
    case (62)://{62,'W',"OCRAN",3}
        {
            if ((P_change_range_flag == 0) && (Pressure.range.ok == 1))
            {  
                if (strcmp(Serialorder.data2,"PA") == 0) P_change_range_unit = 1;
                else if (strcmp(Serialorder.data2,"KPA") == 0) P_change_range_unit = 2;
                else if (strcmp(Serialorder.data2,"MPA") == 0) P_change_range_unit = 3;
                else if (strcmp(Serialorder.data2,"MBAR") == 0) P_change_range_unit = 4;
                else if (strcmp(Serialorder.data2,"BAR") == 0) P_change_range_unit = 5;
                else if (strcmp(Serialorder.data2,"PSI") == 0) P_change_range_unit = 6;
                else if (strcmp(Serialorder.data2,"H2O") == 0) P_change_range_unit = 7;
                else if (strcmp(Serialorder.data2,"HG") == 0) P_change_range_unit = 8;
                else if (strcmp(Serialorder.data2,"HPA") == 0) P_change_range_unit = 9;
                else if (strcmp(Serialorder.data2,"KGF") == 0) P_change_range_unit = 10;
                else if (strcmp(Serialorder.data2,"INH2O") == 0) P_change_range_unit = 11;
                else if (strcmp(Serialorder.data2,"INHG") == 0) P_change_range_unit = 12;
                else P_change_range_unit = Pressure.range.unit;
                
                y = (String_To_Float(Serialorder.data0)) / UNIT_CONV[P_change_range_unit].math_coe;
                y1 = (String_To_Float(Serialorder.data1)) / UNIT_CONV[P_change_range_unit].math_coe;
                if (y > y1)
                {
                    P_change_range_max = y;
                    P_change_range_min = y1;
                }
                else
                {
                    P_change_range_max = y1;
                    P_change_range_min = y;
                }
                _DINT();
                Tare_Down();
                _EINT();
                Send_String('F',"OK","","","","");
            }
            else
            {
                Send_Error("1001");
            }
        }break;
    case (63)://{63,'W',"OSRAN",1}
        {
            if (Pressure.range.ok == 1)
            {  
                x  = String_To_Long(Serialorder.data0);
                if (x == 0)
                {
                    P_change_range_flag = 0;
                    Pressure.unit = Pressure.range.unit;
                }
                else if (x == 1)
                {
                    P_change_range_flag = 1;
                    Pressure.unit = Pressure.range.unit;//此处专为811更改，迁移量程不更改单位
                    //Pressure.unit = P_change_range_unit;
                }
                else 
                {
                    Send_Error("1007");
                    goto RANGE_CS_ERR;
                }
                if ((calib_select&USER_CAL_OK)==USER_CAL_OK) 
                {
                    P_calib.ok = 0;
                    _DINT();
                    Press_Calib_Down();
                    _EINT();
                    calib_select&=~USER_CAL_OK;
                    if(calib_select==FACTORY_CAL_OK)//有厂家校准则厂家校准生效
                    {
                        _DINT();
                        Press_F_Calib_Load();
                        _EINT();
                    }
                }
                Pressure_Unitselect();
                Pressure.dot_len=Data_Format(Pressure.unit);
                Pressure.offset = 0;
            	_DINT();
            	Pressure_Down_Zero();
                Pressure_Down_Unit();
                Tare_Down();
                _EINT();
                Send_String('F',"OK","","","","");
            RANGE_CS_ERR:
                _NOP();
            }
            else
            {
                Send_Error("1001");
            }
        }break;
    case (64)://w:OCFS
        {
            if (Rs232.station == 0)
            {
                Rs232.station = 8;
                P_calib.ok = 0;
                Pressure.offset = 0;
                if (P_change_range_flag == 0) Pressure.unit = Pressure.range.unit;
                else Pressure.unit = P_change_range_unit;
                Pressure.dot_len=Data_Format(Pressure.unit);
                Send_String('F',"OK","","","","");
            }
            else
            {
                over_limit = 1;
            }
        }break;
    case (65)://w:OCF
        {
            if (Rs232.station == 8)
            {
                if (P_change_range_flag == 0) y = (String_To_Float(Serialorder.data1)) / UNIT_CONV[Pressure.range.unit].math_coe;
                else y = (String_To_Float(Serialorder.data1)) / UNIT_CONV[P_change_range_unit].math_coe;
                
                if (strcmp(Serialorder.data0,"Z") == 0)
                {
                    if (P_change_range_flag == 0)
                    {
                        if (Pressure.range.max_p > 0 && Pressure.range.min_p < 0) x = 0;
                        else x= 1;
                    }
                    else
                    {
                        if ((P_change_range_max > 0 && P_change_range_min < 0))x = 0;
                        else x = 1;
                    }
                    
                    P_calib.dot[x] = y;
                    P_calib.value[x] = Pressure.final_value;
                    Send_String('F',"OK","","","",""); 
                }
                else if (strcmp(Serialorder.data0,"M") == 0)
                {
                    if (P_change_range_flag == 0)
                    {
                        if (Pressure.range.max_p > 0 && Pressure.range.min_p < 0)//三点校准
                        {
                            P_calib.dot[1] = y;
                            P_calib.value[1] = Pressure.final_value;
                            Send_String('F',"OK","","","",""); 
                        }
                        else
                        {
                            Send_Error("1007");
                        }
                    }
                    else
                    {
                        if (P_change_range_max > 0 && P_change_range_min < 0)//三点校准
                        {
                            P_calib.dot[1] = y;
                            P_calib.value[1] = Pressure.final_value;
                            Send_String('F',"OK","","","",""); 
                        }
                        else
                        {
                            Send_Error("1007");
                        }
                    }
                }
                else if (strcmp(Serialorder.data0,"F") == 0)
                {
                    P_calib.dot[2] = y;
                    P_calib.value[2] = Pressure.final_value;
                    Send_String('F',"OK","","","","");
                }
                else
                {
                    Send_Error("1007");
                }
            }
            else
            {
                over_limit = 1;
            }
        }break;
        
    case (66)://w:OCFOK
        {
            if (Rs232.station == 8)
            {
                x  = String_To_Long(Serialorder.data0);
                if (x == 0)
                {
                    _DINT();
                    Pressure_Load_Zero();
                    _EINT();
                    if((calib_select&USER_CAL_OK)==USER_CAL_OK)//有用户校准，则用户校准生效
                    {
                        _DINT();
                        Press_Calib_Load();
                        _EINT();
                    }
                    else if(calib_select==FACTORY_CAL_OK)//无用户校准，有厂家校准，则厂家校准生效
                    {
                        Press_F_Calib_Load();
                    }
                    else//无用户校准，也无厂家校准，都不恢复，置P_calib.ok = 0;
                    {
                        calib_select=0;
                        P_calib.ok = 0;
                    }
                    _DINT();
                    Pressure_Load_Unit();
                    _EINT();
                }   
                else
                {
                    P_calib.ok = 0;
                    _DINT();
                    Press_Calib_Down();
                    _EINT();
                    P_calib.ok = 0x01;
                    _DINT();
                    Press_F_Calib_Down();
                    _EINT();
                    calib_select = FACTORY_CAL_OK;
                    _DINT();
                    Pressure_Down_Zero();
                    Pressure_Down_Unit();
                    _EINT();
                }
                Rs232.station = 0;
                Send_String('F',"OK","","","","");
            }
            else
            {
                over_limit = 1;
            }
        }break;
        
    case (67)://w:OCFCL
        {
            P_calib.ok = 0;
            _DINT();
            Press_F_Calib_Down();
            _EINT();
            calib_select&=~FACTORY_CAL_OK;
            if((calib_select&USER_CAL_OK)==USER_CAL_OK)//有用户校准，则用户校准生效
            {
                _DINT();
                Press_Calib_Load();	
                _EINT();
            }
            Send_String('F',"OK","","","","");
        }break;
    case (68)://{68,'W',"OCONT",1},//仪表连续发出测量数
        {
            x = String_To_Long(Serialorder.data0);
            if (x < 0 || x > 3) Send_Error("1007");
            else
            {
                if (x == 0) Rs232.auto_send = 0;
                else Rs232.auto_send = 1;
                autosend_type = x;
                //Rs232.tok = 0;
                Send_String('F',"OK","","","","");
            }
        }break;
        
    case (69)://{69,'W',"MRATE",1},
        {
            x = String_To_Long(Serialorder.data0);
            if (x < 7 || x > 240) Send_Error("1007");
            else
            {
                Pressure.range.rate = x; 
                AD_Restart();
                _DINT();
                Pressure_Down();
                _EINT();
                Send_String('F',"OK","","","","");
            }
        }break;
    case (70)://{70,'W',"OBAUQ",1},//写入波特率
        {
            x = String_To_Long(Serialorder.data0);
            if ((x == 1200) || (x == 2400) || (x == 4800) || (x == 9600) || (x == 19200) || (x == 38400))
            {
                if(x==38400)
                {
                    AD.speed=1;//38400波特率自动识别为高速模式
                    AD_Remeasure();
                }
                Send_String('F',"OK","","","","");
                if (x != Rs232.baud)
                {
                    Rs232.baud = x;
                    _DINT();
                    Rs232_Init();
                    _EINT();
                }
            }
            else
            {
                Send_Error("1007"); //参数不正确
            }  
        }break;
    case (71)://{71,'W',"OSENS",1},//设定传感器类型
        {
            if (strcmp(Serialorder.data0,"G") == 0) x = 0;//表压
            else if (strcmp(Serialorder.data0,"A") == 0) x = 1;//绝压
            else if (strcmp(Serialorder.data0,"D") == 0) x = 2;//差压
            else x = 3;
            if (x == 3)Send_Error("1007"); //参数不正确
            else 
            {
                Pressure.range.sensor_type = x;
                _DINT();
                Pressure_Down();
                _EINT();
                Send_String('F',"OK","","","","");
            }  
        }break;
    case (72)://{72,'W',"oaccy",1}
        {
            Pressure.range.accuracy = String_To_Long(Serialorder.data0);
            _DINT();
            Pressure_Down();
            _EINT();
            Send_String('F',"OK","","","","");
        }break; 
    case (73)://{73,'W',"SPEED",1},//设定压力数据输出速度=0低速=1高速
        {
            x=String_To_Long(Serialorder.data0);
            if(x>1)
            {
                Send_Error("1007");
                return;
            }
            AD.speed= x;
            if(AD.speed==1)
            {
                AD_Remeasure();
            }
            Send_String('F',"OK","","","","");
        }break;
    case (74)://{74,'R',"SPEED",0},//读压力数据输出速度=0低速=1高速
        {
            Long_To_String(AD.speed,Serialorder.data0);
            Send_String('F',Serialorder.data0,"","","","");
        }break;
    case (75)://{75,'R',"ODCAL",0},//读校准日期
        {
            Send_String('F',(char *)cal_date,"","","","");
        }break;
        
    case (76)://{76,'W',"ODCAL",1},//写校准日期
        {  
            for (i = 0;i <20 ;i++)
            {
                if (Serialorder.data0[i] == 0) i = 30;
            }
            if (i < 30) Send_Error("1015");
            else
            {
                cal_date[0] = 0;
                strcat((char *)cal_date,Serialorder.data0);
                _DINT();
                Comm_Down();
                _EINT();
                Send_String('F',"OK","","","","");
            }
        }break;
    case (77)://{77,'R',"OHVER",0}
        {
            Serialorder.data0[0] = 0;
            strcat((char *)Serialorder.data0,(char *)hardware);
            Send_String('F',Serialorder.data0,"","","","");
        }break;
        case(78)://{78,'W',"ERAS",1},//擦除铁电指令，输入密码：211273 
            {
                x  = String_To_Long(Serialorder.data0);
                if(x == 211273)
                {
                    EEPROM_Erasure();
                }
                else
                {
                    Send_Error("1001");
                }
            }break;
    case (79)://{79,'W',"ORPP",0},//仪表软复位
        {
            Send_String('F',"OK","","","","");
            TimerA_Stop(); 
            WDTCTL = 0;
        }break;
    case (80)://{80,'R',"TAG",1},//读TAG
        {
            x  = String_To_Long(Serialorder.data0);
            if ((x < 1) ||( x > 48)) 
            {
                Send_Error("1007");
                return;
            }
            Tag_Load((char*)&Serialorder.data0,x);
            Serialorder.data0[x]=0;
            Send_String('F',Serialorder.data0,"","","","");
        }break;
    case (81)://{81,'W',"TAG",2},//写TAG
        {
            x  = String_To_Long(Serialorder.data0);
            
            if ((x < 1) ||( x > 48)) 
            {
                Send_Error("1007");
                return;
            }
            Tag_Down(Serialorder.data1,x);
            Send_String('F',"OK","","","","");
        }break;
        //case (82)://{82,'R',"OCODE",0} 
        //case (83)://{83,'W',"OCODE",1}
        //与TYPE共用
    case (84)://{84,'R',"ORIV",0}
        {
            Float_To_String(Sensor.power,Serialorder.data0,6,5);
            Float_To_String(Sensor.output,Serialorder.data1,9,6);
            Send_String('F',Serialorder.data0,Serialorder.data1,"","","");
        }break;
    case (85)://{85,'W',"OCRAM",3},//写入用户校准参数//直接写入kPa数据
    case (87)://{87,'W',"OFRAM",3},//写入厂家校准参数
        { 
            if(index==85)
            {
                i=7;
            }
            else
            {
                i=8;
            }
            if (Rs232.station == i)
            {
                if (Math_String(Serialorder.data0)) 
                { 
                    Send_Error("1004");
                    return;
                }
                x=String_To_Long(Serialorder.data0);
                if((x<1)||(x>3))
                {
                    Send_Error("1007");
                    return;
                }
                x=x-1;
                
                if (Math_String(Serialorder.data1) || Math_String(Serialorder.data2))Send_Error("1004");
                else
                {
                    P_calib.dot[x] = String_To_Float(Serialorder.data1);
                    P_calib.value[x] = String_To_Float(Serialorder.data2);
                    Send_String('F',"OK","","","","");
                }
            }
            else
            {
                over_limit = 1;
            }
        }break;
    case (86)://{86,'R',"OCRAM",1},//读用户校准数据
    case (88)://{88,'R',"OFRAM",1},//读厂家校准数据
        { 
            if (Math_String(Serialorder.data0)) 
            { 
                Send_Error("1004");
                return;
            }
            x=String_To_Long(Serialorder.data0);
            if((x<1)||(x>3))
            {
                Send_Error("1007");
                return;
            }		
            x=x-1;
            if(index==86)//读用户校准数据
            {
                if((calib_select&USER_CAL_OK)==USER_CAL_OK)//有用户校准数据,就只能用户数据生效
                {
                    y=P_calib.dot[x];
                    y1=P_calib.value[x];
                }
                else
                {
                    Send_Error("1033");
                    return;
                }
            }
            else//读厂家校准数据
            {
                if((calib_select&FACTORY_USER_CAL_OK)==FACTORY_USER_CAL_OK)//厂家用户数据共存
                {
                    Press_F_Calib_Load();
                    y=P_calib.dot[x];
                    y1=P_calib.value[x];
                    Press_Calib_Load();
                }
                else if((calib_select&FACTORY_CAL_OK)==FACTORY_CAL_OK)//只有厂家校准数据
                {
                    y=P_calib.dot[x];
                    y1=P_calib.value[x];
                }
                else
                {
                    Send_Error("1033");
                    return;
                }
            }
            //直接发送7位kPa数据
            i=Compare_bit(y,7);
            Float_To_String(y,Serialorder.data1,7,i);
            i=Compare_bit(y1,7);
            Float_To_String(y1,Serialorder.data2,7,i);
            Send_String('F',Serialorder.data0,Serialorder.data1,Serialorder.data2,"KPA","");
        }break;
    case (89)://{89,'R',"OCALI",0},//读校准状态
        {
            Serialorder.data0[0]=((calib_select>>4)&0x0f)+0x30;
            Serialorder.data0[1]=0;
            Serialorder.data1[0]=(calib_select&0x0f)+0x30;
            Serialorder.data1[1]=0;
            if (P_change_range_flag == 0)
            {
                if (Pressure.range.max_p > 0 && Pressure.range.min_p < 0) x = 3;
                else x= 2;
            }
            else
            {
                if ((P_change_range_max > 0 && P_change_range_min < 0))x = 3;
                else x = 2;
            }
            Long_To_String(x,Serialorder.data2);
            Send_String('F',Serialorder.data0,Serialorder.data1,Serialorder.data2,"","");
        }break;
    case (0xfa):
        {
            Send_Error("1029");
        }break;
    case (0xfb):
        {
            Send_Error("1017");
        }break;
    case (0xfc):
        {
            Send_Error("1018");
        }break;
    case (0xfd):
        {
            Send_Error("1019");
        }break;
    case (0xfe):
        {
            Send_Error("1020");
        }break;
    default:
        {
            _NOP();
        }
    }
    if (over_limit == 1) Send_Error("1001");
    
}