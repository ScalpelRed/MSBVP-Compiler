#include "video.glsl"

float getPixel(vec2 fragCoord, vec2 screenSize, float time) {

	vec2 pixelPos = floor(fragCoord / screenSize * vec2(width, height));
	pixelPos.y = height - pixelPos.y - 1;
	int pixelIndex = int(floor(pixelPos.y * width + pixelPos.x));

	int framePos = int(floor(time * fps)) % frameCount;
	int intIndex = frameInds[framePos];
	int vecIndex = intIndex >> 2;
	intIndex &= 3;
	int vecIndexLim = (frameInds[framePos + 1] >> 2) + 1;
	for (; vecIndex < vecIndexLim; vecIndex++) {
	
		ivec4 intVec = frameData[vecIndex];
		for (; intIndex < 4; intIndex++) {
		
			int intValue = intVec[intIndex];
			if (intValue >= 0) { // 0X
				if (intValue < 0x40000000) { // 00, 2 groups, 14 count bits
					pixelIndex -= (intValue & 0x3FFF) + 1;
					if (pixelIndex < 0) return float(intValue >> 14 & 1);
					intValue >>= 15;
					pixelIndex -= (intValue & 0x3FFF) + 1;
					if (pixelIndex < 0) return float(intValue >> 14 & 1);
				}
				else { // 01, 3 groups, 9 count bits
					for (int i = 0; i < 3; i++) {
						pixelIndex -= (intValue & 0x1FF) + 1;
						if (pixelIndex < 0) return float(intValue >> 9 & 1);
						intValue >>= 10;
					}
				}
			}
			else { // 1X
				intValue ^= 0x80000000; // zeroing sign bit
			
				if (intValue < 0x40000000) { // 10, 6 groups, 4 count bits
					for (int i = 0; i < 6; i++) {
						pixelIndex -= (intValue & 0xF) + 1;
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
		intIndex = 0;
	}
	return 0.0;
}