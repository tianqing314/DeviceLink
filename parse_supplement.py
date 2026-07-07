#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""解析补充指令"""

hex_str = "55 03 26 01 00 36 22 11 00 04 0D 0B 00 A8 01 29 80 00 00 01 02 00 01 25 26 53 24"
bytes_list = [int(x, 16) for x in hex_str.split()]

print(f"总字节数: {len(bytes_list)}")
print(f"原始数据: {hex_str}")

# CPPI V3.1 帧解析
offset = 0
print(f"\n帧头: 0x{bytes_list[offset]:02X}")
offset += 1

control = bytes_list[offset]
print(f"控制字段: 0x{control:02X} (bit0={control&1}: {'发送帧' if control&1 else '响应帧'}, bit[1:3]={(control>>1)&7}: CPPI V3.1)")
offset += 1

target = bytes_list[offset:offset+3]
target_addr = target[0] | (target[1] << 8) | (target[2] << 16)
print(f"目标地址: {' '.join(f'{b:02X}' for b in target)} → 0x{target_addr:06X} (设备ID=0x{target_addr>>8:04X}, 端口号=0x{target_addr&0xFF:02X}={target_addr&0xFF})")
offset += 3

source = bytes_list[offset:offset+3]
source_addr = source[0] | (source[1] << 8) | (source[2] << 16)
print(f"源地址: {' '.join(f'{b:02X}' for b in source)} → 0x{source_addr:06X}")
offset += 3

func = bytes_list[offset:offset+2]
func_code = func[0] | (func[1] << 8)
print(f"功能码: {' '.join(f'{b:02X}' for b in func)} → 0x{func_code:04X}")
offset += 2

seq = bytes_list[offset]
print(f"流水号: 0x{seq:02X} ({seq})")
offset += 1

data_len_bytes = bytes_list[offset:offset+2]
data_len = data_len_bytes[0] | (data_len_bytes[1] << 8)
print(f"数据长度: {' '.join(f'{b:02X}' for b in data_len_bytes)} → {data_len} 字节")
offset += 2

header_checksum = bytes_list[offset]
print(f"头部校验: 0x{header_checksum:02X}")
offset += 1

data_content = bytes_list[offset:offset+data_len]
print(f"数据内容 ({data_len}字节): {' '.join(f'{b:02X}' for b in data_content)}")
offset += data_len

data_checksum = bytes_list[offset:offset+2]
print(f"数据校验: {' '.join(f'{b:02X}' for b in data_checksum)}")

# 解析数据内容 - 推测为Modbus RTU转发
print(f"\n=== 数据内容解析（推测为 Modbus RTU 转发指令）===")
if len(data_content) >= 4:
    modbus_addr = data_content[0]
    modbus_func = data_content[1]
    print(f"Modbus 从机地址: 0x{modbus_addr:02X}")
    print(f"Modbus 功能码: 0x{modbus_func:02X} ({'F41 写寄存器' if modbus_func == 0x29 else '未知'})")
    
    if modbus_func == 0x29 and len(data_content) >= 11:
        reg_addr = (data_content[2] << 8) | data_content[3]
        reg_count = (data_content[4] << 8) | data_content[5]
        data_length = data_content[6]
        reg_value = (data_content[7] << 8) | data_content[8]
        crc16 = (data_content[9] << 8) | data_content[10]
        
        print(f"起始寄存器地址: 0x{reg_addr:04X}")
        print(f"寄存器数量: {reg_count}")
        print(f"数据长度: {data_length}")
        print(f"寄存器值: 0x{reg_value:04X}")
        print(f"CRC16: 0x{crc16:04X}")
        
        # 查找寄存器含义
        reg_desc = {
            0x8000: "OWI通信使能 (0x0001=进入OWI通信模式, 0x0000=DAC工作模式)",
            0x8002: "调试模式配置 (0x0001=进入调试模式, 0x0000=变送输出模式)",
            0x8003: "调试模式DAC值",
            0x8006: "恒流源0配置",
            0x8007: "恒流源1配置",
            0x800A: "ADC采样率配置",
        }
        
        if reg_addr in reg_desc:
            print(f"寄存器含义: {reg_desc[reg_addr]}")
        else:
            print(f"寄存器含义: 未知 (地址空间3 - 调试用)")
        
        if reg_addr == 0x8000 and reg_value == 0x0001:
            print(f"\n>>> 解读: 向下游变送器(地址0x{modbus_addr:02X})写入寄存器0x8000=0x0001, 启用OWI通信模式")