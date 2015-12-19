#shader vertex

uniform vec4 screen;
uniform vec3 translate;

void main(void)
{
	// screen x, y, w, h
	// gl_Vertex.x, gl_Vertex.y
	float hx = screen[2] / 2.0;
	float hy = screen[3] / 2.0;
	gl_Position.xyz = vec3((gl_Vertex.x - screen[0] + translate.x) / hx - 1.0, (screen[3] - (gl_Vertex.y - screen[1] + translate.y)) / hy - 1.0, gl_Vertex.z + translate.z);
	gl_TexCoord[0] = gl_MultiTexCoord0;
	gl_FrontColor = gl_Color;
}
