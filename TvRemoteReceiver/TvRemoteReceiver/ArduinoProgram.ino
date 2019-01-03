#include <IRremote.h>

#define irPin 7
IRrecv irrecv(irPin);
decode_results results;

long int resultLast = 0;
long int timee;
bool hold = false;
String res = "";

void setup() {
  Serial.begin(9601);
  irrecv.enableIRIn();

}

void loop() {
  if (irrecv.decode(&results)) {

    if (results.value == resultLast && millis() - timee < 110UL) {
      hold = true;

    }
    resultLast = results.value;
    switch (resultLast)
    {
      case 0xE0E006F9:
        res = "{\"Result\":\"CrossUp\",\"Hex\":\"0x" + String(resultLast, HEX) + "\",\"Hold\": ";
        res += hold ? "true }" : "false}";
        break;
      case 0xE0E046B9:
        res = "{\"Result\":\"CrossRight\",\"Hex\":\"0x" + String(resultLast, HEX) + "\",\"Hold\": ";
        res += hold ? "true }" : "false}";
        break;
      case 0xE0E08679:
        res = "{\"Result\":\"CrossDown\",\"Hex\":\"0x" + String(resultLast, HEX) + "\",\"Hold\": ";
        res += hold ? "true }" : "false}";
        break;
      case 0xE0E0A659:
        res = "{\"Result\":\"CrossLeft\",\"Hex\":\"0x" + String(resultLast, HEX) + "\",\"Hold\": ";
        res += hold ? "true }" : "false}";
        break;
      case 0xE0E016E9:
        res = "{\"Result\":\"CrossOk\",\"Hex\":\"0x" + String(resultLast, HEX) + "\",\"Hold\": ";
        res += hold ? "true }" : "false}";
        break;
      case 0xE0E036C9:
        res = "{\"Result\":\"BtnRed\",\"Hex\":\"0x" + String(resultLast, HEX) + "\",\"Hold\": ";
        res += hold ? "true }" : "false}";
        break;
      case 0xE0E028D7:
        res = "{\"Result\":\"BtnGreen\",\"Hex\":\"0x" + String(resultLast, HEX) + "\",\"Hold\": ";
        res += hold ? "true }" : "false}";
        break;
      case 0xE0E0E01F:
        res = "{\"Result\":\"VolUp\",\"Hex\":\"0x" + String(resultLast, HEX) + "\",\"Hold\": ";
        res += hold ? "true }" : "false}";
        break;
      case 0xE0E0D02FU:
        res = "{\"Result\":\"VolDown\",\"Hex\":\"0x" + String(resultLast, HEX) + "\",\"Hold\": ";
        res += hold ? "true }" : "false}";
        break;
case 0xe0e0f00f:
        res = "{\"Result\":\"VolMute\",\"Hex\":\"0x" + String(resultLast, HEX) + "\",\"Hold\": ";
        res += hold ? "true }" : "false}";
        break;
      default:
        res = "{\"Result\":\"Error\",\"Hex\":\"0x" + String(resultLast, HEX) + "\",\"Hold\": ";
        res += hold ? "true }" : "false}";
        break;        
    }

    Serial.println(res);
    timee = millis();
    delay(30);
    hold = false;
    irrecv.resume();
  }


}
