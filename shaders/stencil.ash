#include shaders/vertex.ash
#shader fragment
#version 120
#extension GL_ARB_texture_rectangle : enable

uniform sampler2DRect texture1;

void main(void)
{
	vec4 texCol = texture2DRect(texture1, gl_TexCoord[0].xy);
	gl_FragColor = vec4(gl_Color.rgb, texCol.a*gl_Color.a);
}

