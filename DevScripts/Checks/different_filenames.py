'''
This script points out which json filenames are not contained in both given directories 
'''

import os
import sys

def get_json_filenames(folder_path):
    return {f for f in os.listdir(folder_path) if f.endswith(".json")}

def compare_json_folders(folder1, folder2):
    json_files1 = get_json_filenames(folder1)
    json_files2 = get_json_filenames(folder2)
    
    # Find files in folder1 that are not in folder2
    difference = json_files1 - json_files2
    return difference


folder1 = sys.argv[1]
folder2 = sys.argv[2]

missing_files = compare_json_folders(folder1, folder2)

if missing_files:
    print("JSON files in folder1 not found in folder2:")
    for f in sorted(missing_files):
        print(f)
else:
    print("All JSON files in folder1 are present in folder2.")
