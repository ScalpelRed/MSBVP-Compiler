#version 130

#include "player.glsl"

uniform float viewWidth;
uniform float viewHeight;
uniform sampler2D texture;
uniform sampler2D lightmap;

uniform float frameTimeCounter;

varying vec4 color;
varying vec4 texcoord;
varying vec4 lightcoord;

void main()
{
	gl_FragData[0] = texture2D(texture, texcoord.st) * texture2D(lightmap, lightcoord.st) * color;
	gl_FragData[0].rgb *= 0.25 + 0.75 * getPixel(gl_FragCoord.xy, vec2(viewWidth, viewHeight), frameTimeCounter);
}