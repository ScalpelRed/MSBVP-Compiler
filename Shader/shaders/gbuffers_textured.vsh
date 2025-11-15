#version 130

uniform mat4 gbufferModelView;
uniform mat4 gbufferModelViewInverse;

varying vec4 color;
varying vec4 texcoord;
varying vec4 lightcoord;

uniform float frameTimeCounter;

void main() 
{
	vec4 position = gl_Vertex;
	position = gl_ModelViewMatrix * position;
	position = gbufferModelViewInverse * position;
	position = gbufferModelView * position;
	gl_Position = gl_ProjectionMatrix * position;

	color = gl_Color;
	texcoord = gl_TextureMatrix[0] * gl_MultiTexCoord0;
	lightcoord = gl_TextureMatrix[1] * gl_MultiTexCoord1;
}