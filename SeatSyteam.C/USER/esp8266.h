#ifndef __ESP8266_H
#define __ESP8266_H

#include "stm32f10x.h"

extern uint8_t ESP8266_Init_Success;
extern uint8_t MQTT_Init_Success;

void ESP8266_Init(void);
void MQTT_Init(void);
void MQTT_Publish_SeatStatus(const char* payload);
uint8_t ESP_WaitResp(char *wait_str, uint16_t timeout_ms);

#endif