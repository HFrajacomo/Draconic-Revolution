import sys

def tabbed_print(key, val):
	if(key == "shaderIndex"):
		val = convert_shader_index(val)
	elif(key == "transparent"):
		val = convert_transparency(val)
	else:
		val = convert(val)

	if(key != "tileBottom"):
		print(f"\t\"{key}\": {val},")
	else:
		print(f"\t\"{key}\": {val}")

def convert(val):
	if(val is str):
		return f"\"{val}\""
	return val

def convert_transparency(val):
	val = val.replace(" ", "")
	if(val == "1"):
		return "true"
	if(val == "0"):
		return "false"
	return val

def convert_shader_index(val):
	if(type(val) == str):
		val = val.replace("\t", "").replace(" ", "")

	if(val == "ShaderIndex.OPAQUE"):
		return 0;
	if(val == "ShaderIndex.SPECULAR"):
		return 1;
	if(val == "ShaderIndex.WATER"):
		return 2;
	if(val == "ShaderIndex.ASSETS"):
		return 3;
	if(val == "ShaderIndex.ASSETS_SOLID"):
		return 4;
	if(val == "ShaderIndex.LEAVES"):
		return 5;
	if(val == "ShaderIndex.ICE"):
		return 6;
	if(val == "ShaderIndex.LAVA"):
		return 7;
	return val


defaults = {"codename":"\"\"", "name":"\"\"", "shaderIndex":0, "solid":"true", "transparent":"false", "invisible":"false", "affectLight":"true", "liquid":"false", "seamless":"false", "luminosity":0, "hasLoadEvent":"false", "washable":"false", "needsRotation":"false", "customBreak":"false", "customPlace":"false", "drawRegardless":"false", "maxHP":1, "indestructible":"false", "tileTop":0, "tileSide":0, "tileBottom":0}
blockInfo = {}

input_file = open("converter_input.txt", "r")
text = input_file.read().split("\n")

for line in text:
	if("this." in line):
		line = line.replace("\t", "").replace("this.", "").replace(" =", ":").replace(";", "").replace("    ", "")

		key, val = line.split(":")

		if(key in defaults):
			blockInfo[key] = val

print("{")

for key in defaults.keys():
	if(key in blockInfo.keys()):
		tabbed_print(key, blockInfo[key])
	else:
		pass
		tabbed_print(key, defaults[key])

print("}")
