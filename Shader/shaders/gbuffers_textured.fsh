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
	vec4 fd = texture2D(texture, texcoord.st) * texture2D(lightmap, lightcoord.st) * color;
	vec4 cl = getPixel(gl_FragCoord.xy / vec2(viewWidth, viewHeight), frameTimeCounter);
	gl_FragData[0] = vec4(fd.rgb * mix(vec3(1.0), cl.rgb, cl.a * 0.75), fd.a);
}