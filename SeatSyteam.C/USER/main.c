#include "stm32f10x.h"
#include "delay.h"
#include "timer.h"
#include "serial.h"
#include "radar.h"
#include "json.h"
#include "esp8266.h"
#include "config.h"

int main(void)
{
		//局部变量声明
    uint32_t last_scan = 0;			          //记录上次雷达扫描的时间戳
    uint32_t last_heartbeat = 0;          //记录上次发送心跳的时间戳
    uint32_t last_tick_print = 0;					//记录上次打印系统节拍的时间
    uint32_t last_reconnect_check = 0;		//记录上次检查MQTT连接的时间戳
    
		//初始化
    SystemInit();
    Timer_Init();													//使用TIM2提供系统节拍
    Radar_Init();
    Serial_Init(1);
    Serial_Init(2);
    
    Serial_SendString(1, "\r\n===图书馆座位管理系统===\r\n");  //启动信息（通过串口1打印）
    
		//esp8266初始化
    ESP8266_Init();
    if (!ESP8266_Init_Success)
    {
        Serial_SendString(1, "ESP8266初始化失败！\r\n");
        while (1);
    }
    
    MQTT_Init();
    if (!MQTT_Init_Success)
    {
        Serial_SendString(1, "MQTT用户信息初始化失败！\r\n");
        while (1);
    }
    
    //强制上报初始状态
    Radar_ReadAll();  															//读取当前引脚电平，更新全局数组
    Build_JSON();																		//根据全局数组seat_status生成json字符串，存入json_buffer
    Serial_SendString(1, "初始json数据: ");					
    Serial_SendString(1, json_buffer);							//打印json数据，串口调试使用
    Serial_SendString(1, "\r\n");										
    MQTT_Publish_SeatStatus(json_buffer);						//通过esp8266向onenet云发送数据
    last_heartbeat = GetTick();											//记录心跳起始时间
    last_scan = GetTick();													//纪录扫描起始时间
    
    Serial_SendString(1, "Enter main loop\r\n");
    
    while (1)
    {
				//获取当前时间
        uint32_t now = GetTick();
        
        // 定时打印系统节拍，供串口调试使用（5秒）
        if (now - last_tick_print >= 5000)
        {
            last_tick_print = now;
            Serial_Printf(1, "当前定时: %lu\r\n", now);
        }
        
        //
        if (now - last_reconnect_check >= 10000)
        {
            last_reconnect_check = now;
            if (!MQTT_Init_Success)
            {
                Serial_SendString(1, "MQTT已断开, 正在尝试重新连接...\r\n");
								//轻量级重连方式，重新调用 MQTT_Init()函数
                MQTT_Init();
								//重连后执行操作
                if (MQTT_Init_Success)
                {
										//重新读取雷达数据
                    Radar_ReadAll();
										//生成字符串
                    Build_JSON();
										//重新发送当前数据
                    MQTT_Publish_SeatStatus(json_buffer);
										//重置心跳
                    last_heartbeat = now;
                }
            }
        }
        
        //状态变化上报
        if (now - last_scan >= SCAN_INTERVAL_MS)//100毫秒上报一次，SCAN_INTERVAL_MS已在config定义为100ms
        {
						//当前雷达扫描时间等于当前时间戳
            last_scan = now;
						//读取引脚电平并与上次比较，状态变化则返回1
            if (Radar_CheckChange())
            {
								//状态变化重复操作，封装，发布，重置时间戳
                Build_JSON();
                MQTT_Publish_SeatStatus(json_buffer);
                last_heartbeat = now;
                Serial_SendString(1, "已更新当前数据\r\n");
            }
        }
        
        //心跳保活，作用是维持与云平台的长连接，防止长时间无数据被云平台断开连接
        if (now - last_heartbeat >= HEARTBEAT_INTERVAL_MS)//超过30s自动发送一次数据
        {
            Serial_SendString(1, "心跳超时，正在发送上次数据...\r\n");
						//重复操作重置，封装，发布
            last_heartbeat = now;
            Build_JSON();
            MQTT_Publish_SeatStatus(json_buffer);
        }
        //延时
        Delay_ms(10);
    }
}