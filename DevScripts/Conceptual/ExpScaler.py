'''

This script is meant to verify the EXP progression needed for the next levels of skills.
Here, we can tweak the initial cost, growth rate and added bonus per level.

'''


init = 80
growth_rate = 0.08607
add_bonus = 20

final = 0

for i in range(0,100):
	if(i == 0):
		continue

	if(i == 1):
		last_level = init
	else:
		last_level = int(last_level + last_level*growth_rate + add_bonus)

	print(f"{i} -> {i+1} = {last_level}")
	final = final + last_level

print(f"TOTAL EXP: {final}")