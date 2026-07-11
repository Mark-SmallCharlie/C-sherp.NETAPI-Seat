#include "json.h"
#include "radar.h"
#include <stdio.h>
//定义json缓冲区
char json_buffer[256];

void Build_JSON(void)
{
	//根据传感器上报数据生成json字符串，方便上报noenet云平台
    sprintf(json_buffer,
				//上报json格式
        "{\"id\":\"1\",\"version\":\"1.0\",\"params\":{"
        "\"seat_1\":{\"value\":%s},"
        "\"seat_2\":{\"value\":%s},"
        "\"seat_3\":{\"value\":%s},"
        "\"seat_4\":{\"value\":%s}}}",
				//判断座位信息
        seat_status[0] ? "true" : "false",
        seat_status[1] ? "true" : "false",
        seat_status[2] ? "true" : "false",
        seat_status[3] ? "true" : "false"
    );
}