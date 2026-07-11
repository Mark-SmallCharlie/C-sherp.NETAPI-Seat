#include "esp8266.h"
#include "serial.h"
#include "delay.h"
#include "timer.h"
#include "config.h"
#include <string.h>
#include <stdio.h>
//全局标志
uint8_t ESP8266_Init_Success = 0;//esp8266初始化是否成功
uint8_t MQTT_Init_Success = 0;//noenet连接是否成功
static uint8_t publish_fail_cnt = 0;//记录发送失败的次数
//等待响应函数
uint8_t ESP_WaitResp(char *wait_str, uint16_t timeout_ms)
{
    uint32_t start = 0;
    while (start < timeout_ms)
    {
        if (strstr(USART2_RX_BUF, wait_str) != NULL)
        {
            return 1;
        }
        Delay_ms(1);
        start++;
    }
    return 0;
}
//esp8266初始化
void ESP8266_Init(void)
{
    uint8_t i;
    uint8_t conn_flag, ip_flag;
    uint32_t start_time;
    char cmd[128];
    
    Serial_Init(1);
    Serial_Init(2);
    Delay_ms(500);
    //连续发送两次AT，确保稳定
    for (i = 0; i <= 1; i++)
    {
        USART2_RX_LEN = 0;
        memset(USART2_RX_BUF, 0, sizeof(USART2_RX_BUF));
        Serial_SendString(2, "AT\r\n");
        Serial_SendString(1, "AT测试\r\n");
        Delay_ms(1000);
    }
    if (!ESP_WaitResp("OK", 6000))
    {
        Serial_SendString(1, "AT测试失败\r\n");
        return;
    }
    Serial_SendString(1, "AT测试成功\r\n");
    //复位模块
    USART2_RX_LEN = 0;
    memset(USART2_RX_BUF, 0, sizeof(USART2_RX_BUF));
    Serial_SendString(2, "AT+RST\r\n");
    Serial_SendString(1, "模块复位\r\n");
    Delay_ms(1000);
    //设置STA模式
    USART2_RX_LEN = 0;
    memset(USART2_RX_BUF, 0, sizeof(USART2_RX_BUF));
    Serial_SendString(2, "AT+CWMODE=1\r\n");
    Serial_SendString(1, "设置STA模式\r\n");
    Delay_ms(300);
    //连接WiFi
    USART2_RX_LEN = 0;
    memset(USART2_RX_BUF, 0, sizeof(USART2_RX_BUF));
    sprintf(cmd, "AT+CWJAP=\"%s\",\"%s\"\r\n", WIFI_SSID, WIFI_PASSWORD);
    Serial_SendString(2, cmd);
    Serial_SendString(1, "正在连接WiFi...\r\n");
    
    conn_flag = 0;
    ip_flag = 0;
    start_time = 0;
		//轮询检查
    while (start_time < 10000)
    {
        if (strstr(USART2_RX_BUF, "WIFI CONNECTED") != NULL) conn_flag = 1;
        if (strstr(USART2_RX_BUF, "WIFI GOT IP") != NULL) ip_flag = 1;
        if (conn_flag && ip_flag) break;
        Delay_ms(10);
        start_time += 10;
    }
    if (conn_flag && ip_flag)
    {
        Serial_SendString(1, "WiFi连接成功\r\n");
    }
    else
    {
        Serial_SendString(1, "WiFi连接失败\r\n");
        return;
    }
    
    ESP8266_Init_Success = 1;
    Delay_ms(2000);
}
//MQTT初始化
void MQTT_Init(void)
{
    char cfg_cmd[256];
    char sub_cmd[128];       
    uint32_t start;
    uint8_t connected;
    
    USART2_RX_LEN = 0;
    memset(USART2_RX_BUF, 0, sizeof(USART2_RX_BUF));
    sprintf(cfg_cmd, "AT+MQTTUSERCFG=0,1,\"%s\",\"%s\",\"%s\",0,0,\"\"",
            ONENET_DEVICE_NAME, ONENET_PRODUCT_ID, ONENET_TOKEN);
    Serial_SendString(2, cfg_cmd);
    Serial_SendString(2, "\r\n");
    Serial_SendString(1, "设置MQTT用户数据\r\n");
    if (!ESP_WaitResp("OK", 6000))
    {
        Serial_SendString(1, "MQTT用户数据设置失败！\r\n");
        return;
    }
    Serial_SendString(1, "MQTT用户数据设置成功\r\n");
    Delay_ms(500);
    
    USART2_RX_LEN = 0;
    memset(USART2_RX_BUF, 0, sizeof(USART2_RX_BUF));
    Serial_SendString(2, "AT+MQTTCONN=0,\"mqtts.heclouds.com\",1883,1\r\n");
    Serial_SendString(1, "正在连接OneNET云平台...\r\n");
    
    start = GetTick();
    connected = 0;
    while (GetTick() - start < 10000)
    {
        if (strstr(USART2_RX_BUF, "+MQTTCONNECTED:") != NULL)
        {
            connected = 1;
            break;
        }
        if (strstr(USART2_RX_BUF, "+MQTTCONN: 0") != NULL || strstr(USART2_RX_BUF, "+MQTTCONN:0") != NULL)
        {
            connected = 1;
            break;
        }
        Delay_ms(10);
    }
    
    if (connected)
    {
        Serial_SendString(1, "MQTT云平台连接成功\r\n");
        MQTT_Init_Success = 1;
        Delay_ms(500);
        
        USART2_RX_LEN = 0;
        memset(USART2_RX_BUF, 0, sizeof(USART2_RX_BUF));
        sprintf(sub_cmd, "AT+MQTTSUB=0,\"%s\",0\r\n", ONENET_SUB_TOPIC);
        Serial_SendString(2, sub_cmd);
        if (ESP_WaitResp("OK", 5000))
        {
            Serial_SendString(1, "订阅正常\r\n");
        }
        else
        {
            Serial_SendString(1, "订阅失败\r\n");
        }
    }
    else
    {
        Serial_SendString(1, "消息发送失败 ");
        Serial_SendString(1, USART2_RX_BUF);
        Serial_SendString(1, "\r\n");
        MQTT_Init_Success = 0;
        return;
    }
    Delay_ms(2000);
}

void MQTT_Publish_SeatStatus(const char* payload)
{
    char pub_cmd[256];
    uint16_t payload_len;
    
    if (!MQTT_Init_Success) return;
    
    payload_len = strlen(payload);
    USART2_RX_LEN = 0;
    memset(USART2_RX_BUF, 0, sizeof(USART2_RX_BUF));
    
    sprintf(pub_cmd, "AT+MQTTPUBRAW=0,\"%s\",%d,0,0\r\n", ONENET_PUB_TOPIC, payload_len);
    
    Serial_SendString(1, "当前发送主题: ");
    Serial_SendString(1, pub_cmd);
    Serial_SendString(1, "\r\n");
    
    Serial_SendString(2, pub_cmd);
    
    if (!ESP_WaitResp(">", 3000))
    {
        Serial_SendString(1, "Wait for '>' timeout\r\n");
        publish_fail_cnt++;
        if (publish_fail_cnt >= 3) {
            MQTT_Init_Success = 0;
            publish_fail_cnt = 0;
        }
        return;
    }
    
    Serial_SendString(2, payload);
    Serial_SendString(1, "当前发送数据包: ");
    Serial_SendString(1, (char*)payload);
    Serial_SendString(1, "\r\n");
    
    if (ESP_WaitResp("OK", 6000))
    {
        Serial_SendString(1, "发送成功！\r\n");
        publish_fail_cnt = 0;
    }
    else
    {
        Serial_SendString(1, "发送失败, buffer: ");
        Serial_SendString(1, USART2_RX_BUF);
        Serial_SendString(1, "\r\n");
        publish_fail_cnt++;
        if (publish_fail_cnt >= 3) {
            MQTT_Init_Success = 0;
            publish_fail_cnt = 0;
        }
    }
}