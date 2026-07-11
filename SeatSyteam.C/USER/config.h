#ifndef __CONFIG_H
#define __CONFIG_H

// Wi-Fi名称与密码，统一在用户设置内定义，避免混淆
#define WIFI_SSID       "zdg"
#define WIFI_PASSWORD   "zdg20030228"

// OneNET云平台用户数据
#define ONENET_PRODUCT_ID   "vCRg326c00"
#define ONENET_DEVICE_NAME  "ESP8266"
//计算后的 md5 token
#define ONENET_TOKEN        "version=2018-10-31&res=products%2FvCRg326c00%2Fdevices%2FESP8266&et=1806716220&method=md5&sign=0%2FXPduv9j6%2B2sjaJB2L5DA%3D%3D"
#define ONENET_PUB_TOPIC    "$sys/vCRg326c00/ESP8266/thing/property/post" //发布主题
#define ONENET_SUB_TOPIC    "$sys/vCRg326c00/ESP8266/thing/property/post/reply"  //订阅主题

//时间参数
#define HEARTBEAT_INTERVAL_MS   30000   // 30s心跳间隔
#define SCAN_INTERVAL_MS        100     // 100毫秒雷达扫描间隔

#endif