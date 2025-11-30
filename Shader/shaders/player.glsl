#include video.glsl

vec4 getPixel(vec2 texcoord, float time) {
	ivec2 pixelPos = ivec2(floor(texcoord * vec2(fwidth, fheight)));
	pixelPos.y = (fheight - 1) - pixelPos.y;
	int frameIndex = int(floor(time * fps)) % frameCount;
	int pixelIndex = pixelPos.y * fwidth + pixelPos.x;
	pixelIndex += frameIndex * fwidth * fheight;
	vec4 cl = getTexturePixel(pixelIndex >> 5);
	pixelIndex &= 0x1F;
	int comp = int(floor(cl[pixelIndex >> 3] * 255.0));
	comp = (comp >> (pixelIndex & 0x7)) & 0x1;
	return vec4(comp, comp, comp, 1.0);
}