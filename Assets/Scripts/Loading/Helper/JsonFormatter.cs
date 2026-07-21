using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;

public static class JsonFormatter{
	private static char[] TRIM_CHARS = {'[', ']'};

	public static List<string> StringToList(string text){
		string cleaned = Regex.Replace(text, @"[\s\[\]]", "");
		return new List<string>(Regex.Replace(cleaned, @",$", "").Split(','));
	}

	public static string StringifyJsonFields(string input){
		string pattern1 = "\"json\"\\s*:\\s*\\{([^}]*)\\}";
		string result = Regex.Replace(input, pattern1, new MatchEvaluator(WrapAndClean));
		
		return result;
	}

	public static string RemoveComments(string input){
		if(string.IsNullOrEmpty(input))
			return input;

		string noSingleLine = Regex.Replace(input, @"//.*?$", "", RegexOptions.Multiline);
		string noBlock = Regex.Replace(noSingleLine, @"/\*.*?\*/", "", RegexOptions.Singleline);

		return noBlock;
	}

	private static string WrapAndClean(Match match){
		string inner = "{" + match.Groups[1].Value + "}";
		string cleaned = Regex.Replace(inner, @"\s+", " "); 
		cleaned = cleaned.Replace("\"", "\\\"");
		return "\"json\":\"" + cleaned.Trim() + "\"";
	}
}