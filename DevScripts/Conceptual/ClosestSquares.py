import math
import sys

def closest_square(number):
    sqrt_num = math.sqrt(number)
    upper_square = math.ceil(sqrt_num) ** 2

    if upper_square >= number:
        return upper_square
    else:
        return math.floor(sqrt_num) ** 2

def get_xy_tuple(number):
	return (math.ceil(math.sqrt(number)), math.ceil(math.sqrt(number)))

def find_values(n, closest_tuple):
	a, b = closest_tuple
	main_diff = (a*b) - n
	max_acceptable_diff = get_max_diff_val(n*0.35)

	best_a = a
	best_b = b
	best_diff = main_diff

	print(f"MAX DIFF: {max_acceptable_diff}")

	if(check_valid_val(a) and check_valid_val(b) and main_diff <= max_acceptable_diff):
		return (a, b)

	while(a > 2 or b <= math.ceil(n/2)+1):
		if(not check_valid_val(a)):
			a -= 1
			continue
		if(not check_valid_val(b)):
			b += 1
			continue

		diff = (a*b) - n

		print(f"PROGRESS: {a},{b}  ->  {diff}")

		
		if(diff == 0):
			return a,b
		if(diff > 0 and diff <= max_acceptable_diff):
			return a,b
		if(diff > 0 and diff <= best_diff):
			best_diff = diff
			best_a = a
			best_b = b

		if(diff > 0):
			a -= 1
			continue
		else:
			b += 1
			continue

	if(check_valid_val(n)):
		return 1, n

	return best_a, best_b


def get_max_diff_val(n):
	if(n <= 12):
		return math.ceil(n*0.35)
	elif(n <= 100):
		return math.ceil(n*0.18)
	else:
		return math.ceil(n*0.08)


def check_valid_val(val):
	if(val == 1):
		return True
	if(val%3 == 0 or val%7 == 0 or val%9 == 0 or val%11 == 0 or val%13 or val%17 or val%19 or val%23 or (val%2 == 1 and val%5 != 0)):
		return False
	return True


# Example usage:
number = int(sys.argv[1])
closest = closest_square(number)
x, y = get_xy_tuple(closest)
x, y = find_values(number, (x,y)) 
print(f"Tweaked Tuple is: ({x}, {y}) with distance of: {(x*y)-number}")

CONVERT TO C#