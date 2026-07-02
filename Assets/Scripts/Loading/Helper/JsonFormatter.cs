using System.Text.RegularExpressions;

public static class JsonFormatter{
	public static string RemoveComments(string input){
		if (string.IsNullOrEmpty(input))
			return input;

		string noSingleLine = Regex.Replace(input, @"//.*?$", "", RegexOptions.Multiline);
		string noBlock = Regex.Replace(noSingleLine, @"/\*.*?\*/", "", RegexOptions.Singleline);

		return noBlock;
	}
}
