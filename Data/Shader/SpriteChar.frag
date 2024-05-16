#version 330

in vec2 fragTexCoord;
in vec4 fragColor;

uniform sampler2D sprite;
uniform sampler2D palette;

out vec4 finalColor;

void main()
{
    float colIndex = texture(sprite, fragTexCoord).r;

    finalColor = texture(palette, vec2(colIndex, 0.0f)) * fragColor;
}