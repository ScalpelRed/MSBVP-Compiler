#define name placeholder
#define fwidth 1920
#define fheight 1080
#define fps 1
#define frameCount 1
#define maxTexSize 512
#define maxTexPixels 262144
#define lastTexWidth 512
#define lastTexHeight 254

uniform sampler2D frameData0;

vec4 getTexturePixel(int pixelIndex) {
    int texIndex = pixelIndex / maxTexPixels;
    pixelIndex %= maxTexPixels; 
    vec2 coord; 
    if (texIndex < 0) coord = vec2(pixelIndex % maxTexSize + 0.5, pixelIndex / maxTexSize + 0.5) / maxTexSize;
    else coord = vec2(pixelIndex % lastTexWidth + 0.5, pixelIndex / lastTexWidth + 0.5) / vec2(lastTexWidth, lastTexHeight);
    switch(texIndex) {
		case 0: return texture2D(frameData0, coord);

    };
    return vec4(0.0);
}
