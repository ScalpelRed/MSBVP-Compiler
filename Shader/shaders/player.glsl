#include "video.glsl"

#define I2P14 1073741824

float getPixel(vec2 fragCoord, vec2 screenSize, float time) {

	vec2 pixelPos = floor(fragCoord / screenSize * frameSize);
	pixelPos.y = frameSize.y - pixelPos.y - 1;
	int pixelIndex = int(floor(pixelPos.y * frameSize.x + pixelPos.x));
	int framePos = int(floor(time * fps)) % frameCount;
	int intIndex = frameInds[framePos];
	int intIndexLim = frameInds[framePos + 1];
	for (; intIndex < intIndexLim; intIndex++) {
		int intValue = frameData[intIndex];
		
		if (intValue >= 0) { // 0X
			if (intValue < I2P14) { // 00, 2 groups
				pixelIndex -= (intValue & 16383) + 1;
				if (pixelIndex < 0) return float(intValue >> 14 & 1);
				intValue >>= 15;
				pixelIndex -= (intValue & 16383) + 1;
				if (pixelIndex < 0) return float(intValue >> 14 & 1);
			}
			else { // 01, 3 groups
				for (int i = 0; i < 3; i++) {
					pixelIndex -= (intValue & 511) + 1;
					if (pixelIndex < 0) return float(intValue >> 9 & 1);
					intValue >>= 10;
				}
			}
		}
		else { // 1X
			intValue ^= -2147483648; // zeroing sign bit
			
			if (intValue < I2P14) { // 10, 6 groups
				for (int i = 0; i < 6; i++) {
					pixelIndex -= (intValue & 15) + 1;
					if (pixelIndex < 0) return float(intValue >> 4 & 1); 
					intValue >>= 5;
				}
			}
			else { // 11, immediate 30 pixels
				if (pixelIndex >= 30) pixelIndex -= 30;
				else return (float(intValue >> pixelIndex & 1));
			}
		}
	}
	return 0.0;
}