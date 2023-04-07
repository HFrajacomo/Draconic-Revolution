'''
This script's purpose is to generate Getter and Setter methods for private properties in C# scripts
It's important to note that C# has a Property {get;set;} capability that is overlooked in this project due to it not being traceable
in the profiler or via Debug.Log

argv[1] = Filename to be analyzed
argv[2] = 0/1/2 where
	0 = Generate only Getters
	1 = Generate only Setters
	2 = Generate both
'''

import sys


def generate_getters(input_list):
	print("// Getter\n")

	for line in input_list:
		# If is not a bool type
		if(line[0] != "bool"):
			print(f"public {line[0]} Get{line[1][0].upper() + line[1][1:]}() {ob()}return this.{line[1]};{cb()}")
		# If is bool type
		elif(line[1][0:2] == "is" or line[1][0:3] == "has"):
			print(f"public bool {line[1][0].upper() + line[1][1:]}() {ob()}return this.{line[1]};{cb()}")
		else:
			print(f"public bool Is{line[1][0].upper() + line[1][1:]}() {ob()}return this.{line[1]};{cb()}")

	print("\n")

def generate_setters(input_list):
	print("// Setter\n\n")

	for line in input_list:
		print(f"public void Set{line[1][0].upper() + line[1][1:]}({line[0]} {line[1][0].lower()}) {ob()}this.{line[1]} = {line[1][0]};{cb()}")

	print("\n")

def ob():
	return "{"

def cb():
	return "}"


def main():
	filename = sys.argv[1]
	operation = int(sys.argv[2])
	file = open(filename, "r");
	split_list = []
	input_list = []

	for line in file:
		modded_line = line.strip('\t').strip('\n').strip(';')
		split_list = modded_line.split(' ');

		if(len(split_list) != 3):
			continue
		if(split_list[0] != "private"):
			continue
		if(split_list[0][0:2] == "//"):
			continue
		if("(" in split_list[2]):
			continue

		input_list.append(split_list[1:])


	if(operation == 0 or operation == 2):
		generate_getters(input_list)
	if(operation == 1 or operation == 2):
		generate_setters(input_list)

main()