using System;
using System.Collections.Generic;

/*
Chunk Streaming helpers shared by Client and Server
*/
public static class ChunkStreaming
{
	// Converts a player's Render Distance to the radius of its StreamWindow
	// The extra ring guarantees that every chunk inside Render Distance has all its neighbors loaded to be drawn
	public static int GetRadius(int renderDistance){
		return Math.Max(renderDistance, 1) + 1;
	}

	// Fills output with every chunk inside the window
	public static void FillWindow(StreamWindow window, List<ChunkPos> output){
		output.Clear();

		AddRect(window.center.x - window.radius, window.center.x + window.radius, window.center.z - window.radius, window.center.z + window.radius, window.center.y, output);

		if(window.HasSecondLayer())
			AddRect(window.center.x - window.radius, window.center.x + window.radius, window.center.z - window.radius, window.center.z + window.radius, window.GetSecondLayer(), output);
	}

	/*
	Fills added with the chunks of newWindow that are not in oldWindow and removed with the chunks of oldWindow that are not in newWindow
		Only the strips that differ are visited, so moving N chunks costs O(radius*N) instead of O(radius²)
		Windows that don't overlap (teleports) or layer changes naturally fall back to whole layers
	*/
	public static void Diff(StreamWindow oldWindow, StreamWindow newWindow, List<ChunkPos> added, List<ChunkPos> removed){
		added.Clear();
		removed.Clear();

		AddWindowDifference(oldWindow, newWindow, removed);
		AddWindowDifference(newWindow, oldWindow, added);
	}

	// Adds every chunk of a that is not in b
	private static void AddWindowDifference(StreamWindow a, StreamWindow b, List<ChunkPos> output){
		AddLayerDifference(a, a.center.y, b, output);

		if(a.HasSecondLayer())
			AddLayerDifference(a, a.GetSecondLayer(), b, output);
	}

	private static void AddLayerDifference(StreamWindow a, int y, StreamWindow b, List<ChunkPos> output){
		int ax0 = a.center.x - a.radius;
		int ax1 = a.center.x + a.radius;
		int az0 = a.center.z - a.radius;
		int az1 = a.center.z + a.radius;

		if(!b.ContainsLayer(y)){
			AddRect(ax0, ax1, az0, az1, y, output);
			return;
		}

		int bx0 = b.center.x - b.radius;
		int bx1 = b.center.x + b.radius;
		int bz0 = b.center.z - b.radius;
		int bz1 = b.center.z + b.radius;

		// No overlap
		if(bx1 < ax0 || bx0 > ax1 || bz1 < az0 || bz0 > az1){
			AddRect(ax0, ax1, az0, az1, y, output);
			return;
		}

		// Columns of A entirely outside of B
		AddRect(ax0, Math.Min(ax1, bx0-1), az0, az1, y, output);
		AddRect(Math.Max(ax0, bx1+1), ax1, az0, az1, y, output);

		// Columns shared with B: only the rows outside of B
		int ox0 = Math.Max(ax0, bx0);
		int ox1 = Math.Min(ax1, bx1);

		AddRect(ox0, ox1, az0, Math.Min(az1, bz0-1), y, output);
		AddRect(ox0, ox1, Math.Max(az0, bz1+1), az1, y, output);
	}

	// Inclusive bounds. Empty ranges add nothing
	private static void AddRect(int x0, int x1, int z0, int z1, int y, List<ChunkPos> output){
		for(int x=x0; x <= x1; x++){
			for(int z=z0; z <= z1; z++){
				output.Add(new ChunkPos(x, z, y));
			}
		}
	}
}
