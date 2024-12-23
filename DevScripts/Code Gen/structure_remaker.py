import sys

def ob():
	return '{'

def cb():
	return '}'

def q():
	return '"'

def parse_voxel_loader_names(text):
	parsed_text = text.split('HashSet<ushort>(){')[1].split('};')[0]

	aux = ""
	for element in text.split(", "):
		aux += '"' + element.split('VoxelLoader.GetBlockID("')[1].split('")')[0] + '"' 
		aux += ", "

	return aux[0:-2]

def parse_filltype(t):
	if(t == "OverwriteAll"):
		return 0
	if(t == "FreeSpace"):
		return 1
	if(t == "SpecificOverwrite"):
		return 2
	return 9999

def read_file(filename):
	file = open(filename, 'r')
	return file.readlines()

def parse_line(line, parse_list):
	global first

	aux = ""
	
	if("public class" in line):
		if(not first):
			parse_list.append("}")
		print(f"BASE_{line.split('public class ')[1].split(' :')[0]}")
		parse_list.append(f"{ob()} BASE_{line.split('public class ')[1].split(' :')[0]}\n")
		parse_list.append(f"\t{q()}name{q()}: {q()}BASE_{line.split('public class ')[1].split(' :')[0]}{q()},")
		first = False
	elif("this.sizeX" in line):
		parse_list.append(f"\t{q()}sizeX{q()}: {line.split('= ')[1].split(';')[0]},")
	elif("this.sizeY" in line):
		parse_list.append(f"\t{q()}sizeY{q()}: {line.split('= ')[1].split(';')[0]},")
	elif("this.sizeZ" in line):
		parse_list.append(f"\t{q()}sizeZ{q()}: {line.split('= ')[1].split(';')[0]},")
	elif("this.offsetX" in line):
		parse_list.append(f"\t{q()}offsetX{q()}: {line.split('= ')[1].split(';')[0]},")
	elif("this.offsetZ" in line):
		parse_list.append(f"\t{q()}offsetZ{q()}: {line.split('= ')[1].split(';')[0]},")
	elif("this.considerAir" in line):
		parse_list.append(f"\t{q()}considerAir{q()}: {line.split('= ')[1].split(';')[0]},")
	elif("this.needsBase" in line):
		parse_list.append(f"\t{q()}needsBase{q()}: {line.split('= ')[1].split(';')[0]},")
	elif("this.randomStates" in line):
		parse_list.append(f"\t{q()}randomStates{q()}: {line.split('= ')[1].split(';')[0]},")
	elif("FillType" in line):
		parse_list.append(f"\t{q()}type{q()}: {parse_filltype(line.split('FillType.')[1].split(';')[0])},")
	elif("this.overwriteBlocks" in line):
		if("HashSet<ushort>();" not in line):
			parse_list.append(f"\t{q()}overwriteBlocks{q()}: [{parse_voxel_loader_names(line)}],")
		else:
			parse_list.append(f"\t{q()}overwriteBlocks{q()}: [],")
	elif("this.acceptableBaseBlocks" in line):
		if("HashSet<ushort>();" not in line):
			parse_list.append(f"\t{q()}acceptableBaseBlocks{q()}: [{parse_voxel_loader_names(line)}],")
		else:
			parse_list.append(f"\t{q()}acceptableBaseBlocks{q()}: [],")
	elif("public ushort[] blocks" in line):
		parse_list.append(f"\t{q()}blockdata_raw{q()}: [{line.split('{')[1].split('}')[0]}]")
	elif("public ushort[] hps" in line):
		parse_list.append(f"\t{q()}hpdata_raw{q()}: [{line.split('{')[1].split('}')[0]}]")
	elif("public ushort[] states" in line):
		parse_list.append(f"\t{q()}statedata_raw{q()}: [{line.split('{')[1].split('}')[0]}]")

text = read_file(sys.argv[1])
first = True
parse_list = []

for line in text:
	parse_line(line, parse_list)

'''
current_file = ""
file = None
for line in parse_list:
	if(line[0] == "{"):
		if(file != None):
			file.close()

		current_file = line.split(" ")[1].replace("\n", "") + ".json"
		file = open(f"Generated/{current_file}", "w")
		file.write("{\n")
	else:
		file.write(line + "\n")

'''