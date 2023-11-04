'''
Script to be used when extracting gradients from https://coolors.co/gradient-maker/fae9cf-907030

Create your gradient, click "Copy CSS" and select the "filter" line and use it as first argument when calling this script
'''

import sys

def hex_to_rgb_normalized(hex_color):
    # Remove the '#' character if present
    if hex_color.startswith('#'):
        hex_color = hex_color[1:]

    # Convert the hex color code to RGB
    red = int(hex_color[0:2], 16) / 255.0
    green = int(hex_color[2:4], 16) / 255.0
    blue = int(hex_color[4:6], 16) / 255.0

    return red, green, blue


input_text = sys.argv[1]
splitted = input_text.split(",")
splitted = [x.split("=")[1] for x in splitted][0:2]

init_color = splitted[0]
end_color = splitted[1]

red_normalized1, green_normalized1, blue_normalized1 = hex_to_rgb_normalized(init_color)
red_normalized2, green_normalized2, blue_normalized2 = hex_to_rgb_normalized(end_color)

print(f"new Gradient(new Color({red_normalized1:.2f}f, {green_normalized1:.2f}f, {blue_normalized1:.2f}f), new Color({red_normalized2:.2f}f, {green_normalized2:.2f}f, {blue_normalized2:.2f}f));")