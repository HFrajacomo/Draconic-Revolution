using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.IO;
using UnityEngine;


/*
    TRANSCRIPT FILE (.tsc)

    Grammar:
        [(Timestamp;Text)(;Timestamp;Text)*\n
        (Timestamp;Text)(;Timestamp;Text)*]

    Explanation:
        Every line is text from a segment in the Audio
        Every line has a pair (Timestamp;Text), which is the timestamp in the audio
            that the given timestamp should appear on screen

*/
public static class TranscriptFileHandler
{
    public static string SEGMENT_SEPARATOR = "\n";
    public static string WRAPPER_SEPARATOR = ";";

    public static string ReadTranscript(string filePath){
        return File.ReadAllText(filePath);
    }
}
