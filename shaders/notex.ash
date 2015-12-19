#include shaders/vertex.ash
#shader fragment
#extension GL_ARB_texture_rectangle : enable

void main(void)
{
	gl_FragColor = gl_Color;
}

