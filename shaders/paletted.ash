#include shaders/vertex.ash
#shader fragment
#version 120
#extension GL_ARB_texture_rectangle : enable

uniform sampler2DRect texture1;
uniform sampler2DRect texture2;

void main(void)
{
	// note this ONLY works with nearest filtering. on both palette and texture, especially the texture.
	// because a color between index 0 and index 100 isn't 50, its color between VALUES of 0 and 100.
	vec4 texCol = texture2DRect(texture1, gl_TexCoord[0].xy);
	vec4 palCol = texture2DRect(texture2, vec2(floor(texCol.r*255.0), 0.0));
	gl_FragColor = vec4(palCol.rgb, texCol.a) * gl_Color;
}

